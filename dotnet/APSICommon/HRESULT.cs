
using System;
using System.IO;

namespace Microsoft.Research.APSI.Common
{
    /// <summary>
    /// Possible HRESULTs
    /// </summary>
    internal static class HRESULT
    {
        /// <summary>
        /// HRESULT indicating success
        /// </summary>
        public const uint S_OK = 0;

        /// <summary>
        /// HRESULT indicating success with a False result
        /// </summary>
        public const uint S_FALSE = 1;

        /// <summary>
        /// HRESULT indicating an invalid parameter
        /// </summary>
        public const uint E_INVALIDARG = 0x80070057;

        /// <summary>
        /// HRESULT indicating a null pointer
        /// </summary>
        public const uint E_POINTER = 0x80004003;

        /// <summary>
        /// HRESULT indicating a general failure
        /// </summary>
        public const uint E_FAIL = 0x80004005;

        /// <summary>
        /// HRESULT indicating a resource is not in the correct state
        /// </summary>
        public const uint E_NOT_VALID_STATE = 0x8007139F;

        /// <summary>
        /// Indicates whether the given HRESULT is a successful result
        /// </summary>
        /// <param name="hr">HRESULT to check</param>
        /// <returns>True if successful, False otherwise</returns>
        public static bool Succeeded(uint hr)
        {
            return hr == S_OK || hr == S_FALSE;
        }

        /// <summary>
        /// Indicates whether the given HRESULT is a failed result
        /// </summary>
        /// <param name="hr">HRESULT to check</param>
        /// <param name="error">Error string, if failed</param>
        /// <returns>True if failed, False otherwise</returns>
        public static bool Failed(uint hr, out string error)
        {
            error = GetErrorString(hr);
            return !Succeeded(hr);
        }

        /// <summary>
        /// Indicates whether the given HRESULT is a filed result
        /// </summary>
        /// <param name="hr">HRESULT to check</param>
        /// <returns>True if failed, False otherwise</returns>
        public static bool Failed(uint hr)
        {
            return !Succeeded(hr);
        }

        /// <summary>
        /// Throw an exception if the given HRESULT is not successful
        /// </summary>
        /// <param name="hr">HRESULT to check</param>
        /// <param name="description">Description of the operation</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void ThrowIfFailed(uint hr, string description)
        {
            if (!Succeeded(hr))
            {
                string error = GetErrorString(hr);
                throw new InvalidOperationException($"FAILED: {description}: {hr}: {error}");
            }
        }

        private static string GetErrorString(uint hr)
        {
            switch (hr)
            {
                case S_OK:
                case S_FALSE:
                    return string.Empty;

                case E_INVALIDARG:
                    return "Invalid parameter";

                case E_POINTER:
                    return "Received a null pointer";

                case E_FAIL:
                    return "General failure";

                case E_NOT_VALID_STATE:
                    return "State is not valid";

                default:
                    return "Unknown error";
            }
        }
    }
}
