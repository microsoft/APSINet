// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

#pragma once

#include "pch.h"

// STD
#include <array>
#include <vector>
#include <map>


///////////////////////////////////////////////////////////////////////////
//
// This API is provided as a simple interface for the APSI library
// that can be PInvoked by .Net code.
// 
///////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER

#if defined APSICLIENTNATIVE

#define APSIEXPORT extern "C" __declspec(dllexport)

#else

#define APSIEXPORT extern "C" __declspec(dllimport)

#endif

#define APSICALL __stdcall

#else

#define APSIEXPORT extern "C" __attribute__((visibility("default")))
#define APSICALL

#define TRUE 1
#define FALSE 0

#endif

using apsi_item = std::array<uint64_t, 2>;

/**
Create client instance
*/
APSIEXPORT HRESULT APSICALL APSIClient_Create(void** thisptr);

/**
Destroy client instance
*/
APSIEXPORT HRESULT APSICALL APSIClient_Destroy(void* thisptr);

/**
Set parameters for the client
*/
APSIEXPORT HRESULT APSICALL APSIClient_SetParameters(void* thisptr, const std::uint64_t params_size, const std::uint8_t* parameters);

/**
Perform OPRF for the given items
*/
APSIEXPORT HRESULT APSICALL APSIClient_CreateOPRFRequest(void* thisptr, const std::uint64_t item_count, const apsi_item* items, std::uint64_t* oprf_request_size, std::uint8_t** oprf_request);

/**
Decode OPRF result from Server
*/
APSIEXPORT HRESULT APSICALL APSIClient_ExtractHashes(void* thisptr, const std::uint64_t oprf_response_size, const std::uint8_t* oprf_response, std::uint64_t* hashed_item_count, std::uint8_t** hashed_items);

/**
Perform a Query for the given items.
*/
APSIEXPORT HRESULT APSICALL APSIClient_CreateQuery(void* thisptr, const std::uint64_t item_count, const apsi_item* items, std::uint64_t* encrypted_query_size, std::uint8_t** encrypted_query);

/**
Decrypt the result of a query
*/
APSIEXPORT HRESULT APSICALL APSIClient_ProcessResult(void* thisptr, const std::uint64_t encrypted_result_size, const std::uint8_t* encrypted_result, std::uint64_t* intersection_size, std::uint8_t** intersection);

/**
Free encrypted query memory
*/
APSIEXPORT HRESULT APSICALL APSIClient_ReleaseNativePointer(std::uint8_t* native_ptr);
