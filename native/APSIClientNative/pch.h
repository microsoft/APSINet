// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

#if defined(_MSC_VER)

// add headers that you want to pre-compile here
#include "framework.h"

#else // _MSC_VER

#define FACILITY_WIN32                   7
#define ERROR_INSUFFICIENT_BUFFER        122L    // dderror
#define ERROR_INVALID_STATE              5023L

typedef long HRESULT;

HRESULT ACDL_HRESULT_FROM_WIN32(unsigned long x);

#define E_FAIL                           0x80004005L
#define E_POINTER                        0x80004003L
#define E_INVALIDARG                     0x80070057L
#define E_NOT_SUFFICIENT_BUFFER          ACDL_HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)
#define E_NOT_VALID_STATE                ACDL_HRESULT_FROM_WIN32(ERROR_INVALID_STATE)

#define S_OK                             ((HRESULT)0L)
#define S_FALSE                          ((HRESULT)1L)

#endif // _MSC_VER

#endif //PCH_H
