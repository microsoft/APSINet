// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

#include "pch.h"

#include <stdexcept>
#include <algorithm>
#include "apsiclientnative.h"
#include "apsiclient.h"

using namespace std;
using namespace APSIClient;

#define IfNullRet(exp, ret) if (nullptr == (exp)) { return ret; }

namespace {
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

/**
Create client instance
*/
APSIEXPORT HRESULT APSICALL APSIClient_Create(void** thisptr)
{
    IfNullRet(thisptr, E_POINTER);

    Client* client = new Client();
    *thisptr = client;

    return S_OK;
}

/** 
Destroy client instance
*/
APSIEXPORT HRESULT APSICALL APSIClient_Destroy(void* thisptr)
{
    IfNullRet(thisptr, E_POINTER);

    Client* client = reinterpret_cast<Client*>(thisptr);
    delete client;

    return S_OK;
}

/**
Set parameters for the client
*/
APSIEXPORT HRESULT APSICALL APSIClient_SetParameters(void* thisptr, const uint64_t params_size, const uint8_t* parameters)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(parameters, E_POINTER);

    Client* client = reinterpret_cast<Client*>(thisptr);
    vector<uint8_t> params(params_size);
    copy_bytes(params.data(), parameters, params_size);

    return client->SetParameters(params);
}

/**
Perform OPRF for the given items
*/
APSIEXPORT HRESULT APSICALL APSIClient_CreateOPRFRequest(void* thisptr, const uint64_t item_count, const apsi_item* items, uint64_t* oprf_request_size, uint8_t** oprf_request)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(items, E_POINTER);
    IfNullRet(oprf_request_size, E_POINTER);
    IfNullRet(oprf_request, E_POINTER);

    Client* client = reinterpret_cast<Client*>(thisptr);
    vector<apsi_item> items_a(item_count);
    copy_bytes(items_a.data(), items, sizeof(apsi_item) * item_count);

    vector<uint8_t> oprf_bf;
    HRESULT hr = client->CreateOPRFRequest(items_a, oprf_bf);

    *oprf_request_size = oprf_bf.size();
    *oprf_request = new uint8_t[oprf_bf.size()];

    copy_bytes(*oprf_request, oprf_bf.data(), oprf_bf.size());
    return hr;
}

/**
Decode OPRF result from Server
*/
APSIEXPORT HRESULT APSICALL APSIClient_ExtractHashes(void* thisptr, const uint64_t oprf_response_size, const uint8_t* oprf_response, uint64_t* hashed_item_count, uint8_t** hashed_items)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(oprf_response, E_POINTER);
    IfNullRet(hashed_item_count, E_POINTER);
    IfNullRet(hashed_items, E_POINTER);

    Client* client = reinterpret_cast<Client*>(thisptr);

    vector<uint8_t> prequery_bf(oprf_response_size);
    copy_bytes(prequery_bf.data(), oprf_response, oprf_response_size);

    vector<apsi_item> items_a;
    HRESULT hr = client->ExtractHashes(prequery_bf, items_a);

    *hashed_item_count = items_a.size();
    *hashed_items = new uint8_t[sizeof(apsi_item) * items_a.size()];
    copy_bytes(*hashed_items, items_a.data(), sizeof(apsi_item) * items_a.size());

    return hr;
}

/**
Perform a Query for the given items.
*/
APSIEXPORT HRESULT APSICALL APSIClient_CreateQuery(void* thisptr, const uint64_t item_count, const apsi_item* items, uint64_t* encrypted_query_size, uint8_t** encrypted_query)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(items, E_POINTER);
    IfNullRet(encrypted_query_size, E_POINTER);
    IfNullRet(encrypted_query, E_POINTER);

    Client* client = reinterpret_cast<Client*>(thisptr);

    vector<apsi_item> items_a(item_count);
    copy_bytes(items_a.data(), items, sizeof(apsi_item) * item_count);

    vector<uint8_t> encrypted_query_bf;
    HRESULT hr = client->CreateQuery(items_a, encrypted_query_bf);

    *encrypted_query_size = encrypted_query_bf.size();
    *encrypted_query = new uint8_t[encrypted_query_bf.size()];

    copy_bytes(*encrypted_query, encrypted_query_bf.data(), encrypted_query_bf.size());

    return hr;
}

/**
Decrypt the result of a query
*/
APSIEXPORT HRESULT APSICALL APSIClient_ProcessResult(void* thisptr, const uint64_t result_buffer_size, const uint8_t* encrypted_result, uint64_t *intersection_size, uint8_t **intersection)
{
    IfNullRet(thisptr, E_POINTER);
    IfNullRet(intersection_size, E_POINTER);
    IfNullRet(intersection, E_POINTER);
    Client* client = reinterpret_cast<Client*>(thisptr);

    vector<uint8_t> result_bf(result_buffer_size);
    copy_bytes(result_bf.data(), encrypted_result, result_buffer_size);

    vector<bool> intersection_a;

    HRESULT hr = client->ProcessResult(result_bf, intersection_a);

    *intersection_size = intersection_a.size();
    *intersection = new uint8_t[intersection_a.size()];

    for (size_t i = 0; i < intersection_a.size(); i++)
    {
        (*intersection)[i] = (intersection_a[i] ? 1 : 0);
    }

    return hr;
}

APSIEXPORT HRESULT APSICALL APSIClient_ReleaseNativePointer(uint8_t* native_ptr)
{
    if (nullptr != native_ptr)
    {
        delete[] native_ptr;
    }

    return S_OK;
}
