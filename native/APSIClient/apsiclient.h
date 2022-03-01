// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

#pragma once

#include "pch.h"

// STD
#include <array>
#include <memory>
#include <vector>


namespace apsi
{
    namespace receiver
    {
        class Receiver;
        class IndexTranslationTable;
    }

    namespace oprf {
        class OPRFReceiver;
    }

    using LabelKey = std::array<unsigned char, 16>;
}

namespace APSIClient
{
    using apsi_item = std::array<std::uint64_t, 2>;

    /**
    APSI Client
    */
    class Client
    {
    public:
        Client();
        ~Client();

        /**
        Set parameters for the Client.
        Parameters need to be set before creating a Query.
        */
        HRESULT SetParameters(const std::vector<std::uint8_t>& parameters);

        /**
        Create an OPRF request for the given items.

        NOTE: After a successful call, the resulting oprf_request vector will have been resized to the actual size of
        the OPRF request.
        */
        HRESULT CreateOPRFRequest(const std::vector<apsi_item>& items, std::vector<std::uint8_t>& oprf_request);

        /**
        Extract hashed items from an OPRF response
        */
        HRESULT ExtractHashes(const std::vector<std::uint8_t>& oprf_response, std::vector<apsi_item>& hashed_items);

        /**
        Create a Query for the given items.

        NOTE: After a successful call, the resulting encrypted_query vector will have been resized to the actual size of
        the encrypted query.
        */
        HRESULT CreateQuery(const std::vector<apsi_item>& items, std::vector<std::uint8_t>& encrypted_query);

        /**
        Process the result of a query and get the intersection

        NOTE: After a successful call, the resulting intersection vector will have been resized to the actual size
        intersection, which will be the same number of items that were passed to the CreateQuery method.
        */
        HRESULT ProcessResult(const std::vector<std::uint8_t>& encrypted_result, std::vector<bool>& intersection);

    private:
        std::unique_ptr<apsi::receiver::Receiver> receiver_;
        std::unique_ptr<apsi::oprf::OPRFReceiver> oprf_receiver_;
        std::unique_ptr<apsi::receiver::IndexTranslationTable> itt_;
        std::unique_ptr<std::vector<apsi::LabelKey>> label_keys_;

        /**
        Release Receiver.
        */
        void Terminate();
    };
}
