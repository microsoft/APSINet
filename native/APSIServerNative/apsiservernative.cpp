// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

// APSINative
#include "pch.h"
#include "apsiservernative.h"

// STD
#include <thread>
#include <vector>
#include <sstream>
#include <mutex>
#include <fstream>
#include <unordered_set>

// APSI
#include "apsi/item.h"
#include "apsi/sender.h"
#include "apsi/network/stream_channel.h"
#include "apsi/log.h"
#include "apsi/oprf/oprf_sender.h"

// SEAL
#include "seal/relinkeys.h"
#include "seal/randomgen.h"
#include "seal/util/streambuf.h"


using namespace std;
using namespace apsi;
using namespace apsi::sender;
using namespace apsi::network;
using namespace apsi::oprf;
using namespace seal;
using namespace seal::util;


static int server_instance_count_s = 0;


namespace {
    class APSIServer
    {
    public:
        APSIServer(OPRFKey* oprf_key, PSIParams* params)
            : oprf_key_(make_shared<OPRFKey>(*oprf_key)), params_(make_shared<PSIParams>(*params)), sender_db_(nullptr)
        {}

    private:
        APSIServer()
        {}

    public:
        void create_sender_db()
        {
            sender_db_ = make_shared<SenderDB>(*params_, *oprf_key_, /* label_byte_count */ 0, /* nonce_byte_count */ 16, /* compressed */ true);
        }

        shared_ptr<SenderDB> get_sender_db()
        {
            return sender_db_;
        }

        void save(ostream& stream)
        {
            // Save basic data
            sender_db_->save(stream);
        }

        static APSIServer* Load(istream& stream)
        {
            APSIServer* server = new APSIServer();
            server->sender_db_ = make_shared<SenderDB>(SenderDB::Load(stream).first);
            return server;
        }

    private:
        shared_ptr<SenderDB> sender_db_;
        shared_ptr<OPRFKey> oprf_key_;
        shared_ptr<PSIParams> params_;
    };

    void copy_bytes(void* dst, const void* src, size_t count)
    {
        if (!count) {
            return;
        }
        if (!src) {
            throw invalid_argument("cannot copy data: source is null");
        }
        if (!dst) {
            throw invalid_argument("cannot copy data: destination is null");
        }
        copy_n(
            reinterpret_cast<const unsigned char*>(src),
            count,
            reinterpret_cast<unsigned char*>(dst));
    }
}

APSIEXPORT HRESULT APSICALL APSIServer_GetParameters(void* thisptr, uint64_t* parameters_size, uint8_t** parameters)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(parameters_size, E_POINTER);
    IfNullRet(parameters, E_POINTER);

    APSIServer* server = reinterpret_cast<APSIServer*>(thisptr);
    auto sender_db = server->get_sender_db();

    IfNullRet(sender_db, E_INVALIDARG);

    PSIParams params = sender_db->get_params();

    stringstream ss;
    params.save(ss);

    string params_str = ss.str();
    *parameters_size = params_str.size();
    *parameters = new uint8_t[params_str.size()];
    copy_bytes(*parameters, params_str.c_str(), params_str.size());

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_Query(void* thisptr, const uint64_t encrypted_query_size, const uint8_t* encrypted_query, uint64_t* result_buffer_size, uint8_t** result_buffer)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(encrypted_query, E_POINTER);
    IfNullRet(result_buffer_size, E_POINTER);
    IfNullRet(result_buffer, E_POINTER);

    APSIServer* server = reinterpret_cast<APSIServer*>(thisptr);
    auto sender_db = server->get_sender_db();

    IfNullRet(sender_db, E_INVALIDARG);

    try
    {
        stringstream ss;
        ss.write(reinterpret_cast<const char*>(encrypted_query), encrypted_query_size);

        QueryRequest query_request = make_unique<SenderOperationQuery>();

        query_request->load(ss, sender_db->get_seal_context());

        Query query(move(query_request), sender_db);

        stringstream ss_response;
        StreamChannel channel_response(ss_response);
        Sender::RunQuery(query, channel_response);

        string str = ss_response.str();

        *result_buffer_size = str.size();
        *result_buffer = new uint8_t[str.size()];
        copy_bytes(*result_buffer, str.data(), str.size());
    }
    catch (const std::exception& ex)
    {
        APSI_LOG_ERROR("APSIServer::Query: " << ex.what());
        return E_FAIL;
    }
    catch (...)
    {
        APSI_LOG_ERROR("APSIServer::Query: unknown exception");
        return E_FAIL;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_ReleasePointer(std::uint8_t* ptr)
{
    if (nullptr != ptr)
    {
        delete[] ptr;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_SetData(void* thisptr, std::uint64_t count, std::uint64_t* data)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(data, E_POINTER);

    APSIServer* server = reinterpret_cast<APSIServer*>(thisptr);

    try
    {
        vector<Item> apsidata(count);
        for (size_t i = 0; i < count; i++)
        {
            size_t idx = i * 2;
            auto apsidata64 = apsidata[i].get_as<uint64_t>();
            apsidata64[0] = data[idx];
            apsidata64[1] = data[idx + 1];
        }

        server->create_sender_db();
        auto sender_db = server->get_sender_db();

        sender_db->set_data(apsidata);
        sender_db->strip();
    }
    catch (const std::exception& ex)
    {
        APSI_LOG_ERROR("APSIServer::SetData: " << ex.what());
        return E_FAIL;
    }
    catch (...)
    {
        APSI_LOG_ERROR("APSIServer::SetData: unknown exception");
        return E_FAIL;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_Create(void** thisptr, void* poprf_key, void* params)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(poprf_key, E_POINTER);
    IfNullRet(params, E_POINTER);

    Log::SetLogLevel(Log::Level::info);
    server_instance_count_s++;

    OPRFKey* oprf_key = reinterpret_cast<OPRFKey*>(poprf_key);
    PSIParams* parameters = reinterpret_cast<PSIParams*>(params);

    APSIServer* server = new APSIServer(oprf_key, parameters);
    *thisptr = server;

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_Destroy(void* thisptr)
{
    IfNullRet(thisptr, E_POINTER);

    APSIServer* server = reinterpret_cast<APSIServer*>(thisptr);
    delete server;

    server_instance_count_s--;
    if (server_instance_count_s <= 0)
    {
        Log::Terminate();
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_SaveDB1(void* thisptr, uint64_t* db_buffer_size, uint8_t** db_buffer)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(db_buffer_size, E_POINTER);
    IfNullRet(db_buffer, E_POINTER);

    APSIServer* server = reinterpret_cast<APSIServer*>(thisptr);
    auto sender_db = server->get_sender_db();

    if (nullptr == sender_db)
        return E_INVALIDARG;

    try
    {
        string str;
        {
            // Keep ss in this block so its memory will be released at the end of the block
            stringstream ss;
            server->save(ss);
            str = ss.str();
        }

        *db_buffer_size = str.size();
        *db_buffer = new uint8_t[str.size()];
        copy_bytes(*db_buffer, str.data(), str.size());
    }
    catch (const std::exception& ex)
    {
        APSI_LOG_ERROR("APSIServer_SaveDB: Error saving DB: " << ex.what());
        return E_FAIL;
    }
    catch (...)
    {
        APSI_LOG_ERROR("APSIServer_SaveDB: Unknown error saving DB");
        return E_FAIL;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_SaveDB2(void* thisptr, char* file_path)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(file_path, E_POINTER);

    APSIServer* server = reinterpret_cast<APSIServer*>(thisptr);
    auto sender_db = server->get_sender_db();

    if (nullptr == sender_db)
        return E_INVALIDARG;

    try {
        ofstream output(file_path, ios::binary | ios::out | ios::trunc);
        server->save(output);
        output.close();
    }
    catch (const std::exception& ex) {
        APSI_LOG_ERROR("APSIServer_SaveDB2: Error saving DB: " << ex.what());
        return E_FAIL;
    }
    catch (...) {
        APSI_LOG_ERROR("APSIServer_SaveDB2: Unknown error saving DB");
        return E_FAIL;
    }

    return S_OK;
}


APSIEXPORT HRESULT APSICALL APSIServer_LoadDB1(void** thisptr, uint64_t db_buffer_size, uint8_t* db_buffer)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(db_buffer, E_POINTER);

    try
    {
        ArrayGetBuffer agbuf(reinterpret_cast<const char*>(db_buffer), db_buffer_size);
        istream db_stream(&agbuf);

        APSIServer* server = APSIServer::Load(db_stream);
        *thisptr = server;
    }
    catch (const std::exception& ex)
    {
        APSI_LOG_ERROR("APSIServer_LoadDB1: Error loading DB: " << ex.what());
        return E_FAIL;
    }
    catch (...)
    {
        APSI_LOG_ERROR("APSIServer_LoadDB1: Uknown error loading DB");
        return E_FAIL;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIServer_LoadDB2(void** thisptr, char* file_path)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(file_path, E_POINTER);

    try
    {
        ifstream input(file_path, ios::binary | ios::in);
        APSIServer* server = APSIServer::Load(input);
        input.close();
        *thisptr = server;
    }
    catch (const std::exception& ex)
    {
        APSI_LOG_ERROR("APSIServer_LoadDB2: Error loading DB: " << ex.what());
    }
    catch (...)
    {
        APSI_LOG_ERROR("APSIServer_LoadDB2: Unknown error loading DB");
        return E_FAIL;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL OPRFKey_Create(void** thisptr)
{
    IfNullRet(thisptr, E_POINTER);

    OPRFKey* oprf_key = new OPRFKey();
    *thisptr = oprf_key;

    return S_OK;
}

APSIEXPORT HRESULT APSICALL OPRFKey_Destroy(void* thisptr)
{
    IfNullRet(thisptr, E_POINTER);

    OPRFKey* oprf_key = reinterpret_cast<OPRFKey*>(thisptr);
    delete oprf_key;

    return S_OK;
}

APSIEXPORT HRESULT APSICALL OPRFKey_Save(void* thisptr, std::uint64_t* key_size, std::uint8_t* oprf_key_buffer)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(key_size, E_POINTER);

    OPRFKey* oprf_key = reinterpret_cast<OPRFKey*>(thisptr);
    stringstream ss;

    oprf_key->save(ss);

    string str = ss.str();
    *key_size = str.length();

    if (nullptr == oprf_key_buffer)
    {
        // We only wanted the size
        return S_OK;
    }

    copy_bytes(oprf_key_buffer, str.data(), str.length());
    return S_OK;
}

APSIEXPORT HRESULT APSICALL OPRFKey_Load(void* thisptr, const std::uint64_t key_size, const std::uint8_t* oprf_key_buffer)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(oprf_key_buffer, E_POINTER);

    try
    {
        OPRFKey* oprf_key = reinterpret_cast<OPRFKey*>(thisptr);
        stringstream ss;
        ss.write(reinterpret_cast<const char*>(oprf_key_buffer), key_size);
        ss.seekp(0);

        oprf_key->load(ss);
    }
    catch (const std::exception& ex)
    {
        APSI_LOG_ERROR("OPRFKey::Load: " << ex.what());
        return E_FAIL;
    }
    catch (...)
    {
        APSI_LOG_ERROR("OPRFKey::Load: unknown exception");
        return E_FAIL;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL OPRFSender_RunOPRF(const std::uint64_t encoded_items_size, const std::uint8_t* encoded_items, void* oprf_key, std::uint64_t* result_buffer_size, std::uint8_t** result_buffer)
{
    IfNullRet(encoded_items, E_POINTER);
    IfNullRet(oprf_key, E_POINTER);
    IfNullRet(result_buffer_size, E_POINTER);
    IfNullRet(result_buffer, E_POINTER);

    try
    {
        stringstream ss;
        ss.write(reinterpret_cast<const char*>(encoded_items), encoded_items_size);

        OPRFRequest oprf_request = make_unique<SenderOperationOPRF>();
        oprf_request->load(ss);

        size_t oprf_count = oprf_request->data.size() / oprf_query_size;
        OPRFKey* poprfKey = reinterpret_cast<OPRFKey*>(oprf_key);

        stringstream ss_response;
        StreamChannel channel(ss_response);
        Sender::RunOPRF(oprf_request, *poprfKey, channel);

        string str = ss_response.str();

        *result_buffer_size = str.size();
        *result_buffer = new uint8_t[str.size()];
        copy_bytes(*result_buffer, str.data(), str.length());
    }
    catch (const std::exception& ex)
    {
        APSI_LOG_ERROR("OPRFSender::PreQuery: " << ex.what());
        return E_FAIL;
    }
    catch (...)
    {
        APSI_LOG_ERROR("OPRFSender::PreQuery: unknown exception");
        return E_FAIL;
    }

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIParams_Create(void** thisptr, const char* params)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(params, E_POINTER);

    // Parameters should be a JSON string
    string parameters(params);
    PSIParams *params_ptr = new PSIParams(PSIParams::Load(parameters));

    *thisptr = params_ptr;

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSIParams_Destroy(void* thisptr)
{
    IfNullRet(thisptr, E_POINTER);

    PSIParams* params = reinterpret_cast<PSIParams*>(thisptr);
    delete params;

    return S_OK;
}

APSIEXPORT HRESULT APSICALL APSI_SetThreads(uint64_t threads)
{
    if (threads == 0) {
        threads = thread::hardware_concurrency();
    }

    ThreadPoolMgr::SetThreadCount(threads);

    return S_OK;
}
