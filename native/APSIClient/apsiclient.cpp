// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

// STD
#include <memory>
#include <thread>
#include <vector>
#include <sstream>
#include <random>

// APSINative
#include "pch.h"
#include "apsiclient.h"

// APSI
#include "apsi/item.h"
#include "apsi/receiver.h"
#include "apsi/network/stream_channel.h"
#include "apsi/log.h"
#include "apsi/oprf/oprf_receiver.h"

// SEAL
#include "seal/publickey.h"
#include "seal/relinkeys.h"
#include "seal/ciphertext.h"


using namespace std;
using namespace apsi;
using namespace apsi::receiver;
using namespace apsi::oprf;
using namespace apsi::network;
using namespace seal::util;

static int client_instance_count_s = 0;


namespace
{
    void CreateReceiver(const PSIParams& params, unique_ptr<Receiver> &receiver)
    {
        Log::SetLogLevel(Log::Level::info);

        // Client is single threaded to avoid CPU spikes
        ThreadPoolMgr::SetThreadCount(1);
        receiver.reset(new Receiver(params));
    }
}

APSIClient::Client::Client()
{
    client_instance_count_s++;
}

APSIClient::Client::~Client()
{
    Terminate();

    client_instance_count_s--;
    if (client_instance_count_s <= 0)
    {
        Log::Terminate();
    }
}

HRESULT APSIClient::Client::SetParameters(const vector<uint8_t>& parameters)
{
    stringstream ss;
    ss.write(reinterpret_cast<const char*>(parameters.data()), parameters.size());

    auto params = PSIParams::Load(ss);

    // New parameters means new receiver
    receiver_ = nullptr;
    CreateReceiver(params.first, receiver_);

    return S_OK;
}

HRESULT APSIClient::Client::CreateOPRFRequest(const vector<apsi_item>& items, vector<uint8_t>& oprf_request)
{
    // Max max_query_elements items supported
    if (items.size() == 0)
        return E_INVALIDARG;

    // Terminate any previously existing Receiver
    Terminate();

    {
        default_random_engine generator;
        uniform_int_distribution<uint64_t> distribution(0);

        vector<apsi::Item> apsi_items;
        apsi_items.resize(items.size());

        // Copy user items
        for (size_t i = 0; i < items.size(); i++)
            std::memcpy(apsi_items[i].get_as<uint64_t>().data(), items[i].data(), sizeof(apsi_item));

        // Create OPRF receiver
        oprf_receiver_ = make_unique<OPRFReceiver>(Receiver::CreateOPRFReceiver(apsi_items));

        Request oprf_req = Receiver::CreateOPRFRequest(*oprf_receiver_);

        stringstream ss;
        oprf_req->save(ss);

        string str = ss.str();
        oprf_request.resize(str.size());
        memcpy(oprf_request.data(), str.data(), str.size());
    }

    return S_OK;
}

HRESULT APSIClient::Client::ExtractHashes(const vector<uint8_t>& oprf_response, vector<apsi_item>& hashed_items)
{
    IfNullRet(oprf_receiver_, E_NOT_VALID_STATE);

    {
        stringstream ss;
        ss.write(reinterpret_cast<const char*>(oprf_response.data()), oprf_response.size());

        StreamChannel input(ss);
        OPRFResponse oprf_response = to_oprf_response(input.receive_response());

        vector<HashedItem> hashed_recv_items;
        vector<LabelKey> label_keys;
        tie(hashed_recv_items, label_keys) = Receiver::ExtractHashes(oprf_response, *oprf_receiver_);

        // Copy result to output
        hashed_items.resize(hashed_recv_items.size());
        memcpy(hashed_items.data(), hashed_recv_items.data(), sizeof(apsi_item) * hashed_items.size());
        label_keys_ = make_unique<vector<LabelKey>>(move(label_keys));
    }

    return S_OK;
}

HRESULT APSIClient::Client::CreateQuery(const vector<apsi_item>& items, vector<uint8_t>& encrypted_query)
{
    IfNullRet(receiver_, E_NOT_VALID_STATE);

    if (items.size() == 0)
        return E_INVALIDARG;

    {
        // Copy input items
        vector<apsi::HashedItem> hashed_items(items.size());
        for (size_t i = 0; i < items.size(); i++)
        {
            auto hashed_item = hashed_items[i].get_as<uint64_t>();
            hashed_item[0] = items[i][0];
            hashed_item[1] = items[i][1];
        }

        auto query = receiver_->create_query(hashed_items);
        itt_ = make_unique<IndexTranslationTable>(move(query.second));

        stringstream ss;
        query.first->save(ss);

        string str = ss.str();
        encrypted_query.resize(str.size());
        memcpy(encrypted_query.data(), str.data(), str.size());
    }

    return S_OK;
}

HRESULT APSIClient::Client::ProcessResult(const vector<uint8_t>& encrypted_result, vector<bool>& intersection)
{
    IfNullRet(receiver_, E_NOT_VALID_STATE);

    {
        stringstream ss;
        ss.write(reinterpret_cast<const char*>(encrypted_result.data()), encrypted_result.size());

        StreamChannel channel(ss);

        QueryResponse query_response = to_query_response(channel.receive_response());
        vector<ResultPart> result_parts(query_response->package_count);

        for (uint32_t i = 0; i < query_response->package_count; i++)
        {
            result_parts[i] = channel.receive_result(receiver_->get_seal_context());
        }
        auto query_result = receiver_->process_result(*label_keys_, *itt_, result_parts);

        intersection.resize(query_result.size());

        // Copy result
        for (size_t i = 0; i < intersection.size(); i++)
        {
            intersection[i] = query_result[i].found;
        }
    }

    return S_OK;
}

void APSIClient::Client::Terminate()
{
    oprf_receiver_ = nullptr;
    itt_ = nullptr;
    label_keys_ = nullptr;
}
