using Microsoft.Research.APSI.Common;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Research.APSI.Server
{
    /// <summary>
    /// Methods for performing OPRF in the Server
    /// </summary>
    public static class OPRFSender
    {
        /// <summary>
        /// Run OPRF on the given encoded items
        /// </summary>
        /// <param name="oprfRequest">Items encoded in a byte array by an APSIClient</param>
        /// <param name="oprfKey">OPRF key used to preprocess the encoded items</param>
        /// <returns>Byte array that must be sent to the client as a result, null if PreQuery failed</returns>
        public static byte[] RunOPRF(byte[] oprfRequest, OPRFKey oprfKey)
        {
            if (null == oprfRequest)
                throw new ArgumentNullException(nameof(oprfRequest));
            if (null == oprfKey)
                throw new ArgumentNullException(nameof(oprfKey));

            ulong resultSize = 0;
            IntPtr nativeBuffer = IntPtr.Zero;

            uint hr = NativeMethods.OPRFSender_RunOPRF((ulong)oprfRequest.LongLength, oprfRequest, oprfKey.NativePtr, ref resultSize, ref nativeBuffer);
            HRESULT.ThrowIfFailed(hr, "Run OPRF");

            byte[] resultBuffer = new byte[resultSize];
            Marshal.Copy(nativeBuffer, resultBuffer, startIndex: 0, length: (int)resultSize);

            hr = NativeMethods.APSIServer_ReleasePointer(nativeBuffer);
            HRESULT.ThrowIfFailed(hr, "Release OPRF response");

            return resultBuffer;
        }
    }
}
