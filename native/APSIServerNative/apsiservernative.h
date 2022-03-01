// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

#pragma once

// STD
#include <cstdint>

///////////////////////////////////////////////////////////////////////////
//
// This API is provided as a simple interface for the APSI library
// that can be PInvoked by .Net code.
// 
///////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER

#ifdef APSISERVERNATIVEDLL
#define APSIEXPORT extern "C" __declspec(dllexport)
#else
#define APSIEXPORT extern "C" __declspec(dllimport)
#endif

#define APSICALL __stdcall

#else

#define APSIEXPORT
#define APSICALL

#define TRUE 1
#define FALSE 0

#endif

APSIEXPORT HRESULT APSICALL APSIServer_GetParameters(void* thisptr, std::uint64_t* parameters_size, std::uint8_t** parameters);

APSIEXPORT HRESULT APSICALL APSIServer_Query(void* thisptr, const std::uint64_t encrypted_query_size, const std::uint8_t* encrypted_query, std::uint64_t* result_buffer_size, std::uint8_t** result_buffer);

APSIEXPORT HRESULT APSICALL APSIServer_ReleasePointer(std::uint8_t* ptr);

APSIEXPORT HRESULT APSICALL APSIServer_SetData(void* thisptr, const std::uint64_t count, std::uint64_t* data);

APSIEXPORT HRESULT APSICALL APSIServer_Create(void** thisptr, void *oprf_key, void* params);

APSIEXPORT HRESULT APSICALL APSIServer_Destroy(void* thisptr);

APSIEXPORT HRESULT APSICALL APSIServer_SaveDB1(void* thisptr, std::uint64_t* db_buffer_size, std::uint8_t** db_buffer);

APSIEXPORT HRESULT APSICALL APSIServer_SaveDB2(void* thisptr, char* file_path);

APSIEXPORT HRESULT APSICALL APSIServer_LoadDB1(void** thisptr, std::uint64_t db_buffer_size, std::uint8_t* db_buffer);

APSIEXPORT HRESULT APSICALL APSIServer_LoadDB2(void** thisptr, char* file_path);

APSIEXPORT HRESULT APSICALL OPRFKey_Create(void** thisptr);

APSIEXPORT HRESULT APSICALL OPRFKey_Destroy(void* thisptr);

APSIEXPORT HRESULT APSICALL OPRFKey_Save(void* thisptr, std::uint64_t* key_size, std::uint8_t* oprf_key_buffer);

APSIEXPORT HRESULT APSICALL OPRFKey_Load(void* thisptr, const std::uint64_t key_size, const std::uint8_t* oprf_key_buffer);

APSIEXPORT HRESULT APSICALL OPRFSender_RunOPRF(const std::uint64_t encoded_items_size, const std::uint8_t* encoded_items, void* oprf_key, std::uint64_t* result_buffer_size, std::uint8_t** result_buffer);

APSIEXPORT HRESULT APSICALL APSIParams_Create(void** thisptr, const char* params);

APSIEXPORT HRESULT APSICALL APSIParams_Destroy(void* thisptr);

APSIEXPORT HRESULT APSICALL APSI_SetThreads(std::uint64_t threads);
