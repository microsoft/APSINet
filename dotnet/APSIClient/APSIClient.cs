using Microsoft.Research.APSI.Common;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace Microsoft.Research.APSI.Client
{
    /// <summary>
    /// Client for an APSI Server.
    /// </summary>
    public class APSIClient : NativeObject
    {
        /// <summary>
        /// Create an APSIClient object
        /// </summary>
        public APSIClient()
        {
            uint hr = NativeMethods.APSIClient_Create(out IntPtr thisptr);
            HRESULT.ThrowIfFailed(hr, "Create APSIClient");

            NativePtr = thisptr;
        }

        /// <summary>
        /// Set parameters on the current APSIClient
        /// </summary>
        /// <param name="parameters"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetParameters(byte[] parameters)
        {
            if (parameters == null) 
                throw new ArgumentNullException(nameof(parameters));

            uint hr = NativeMethods.APSIClient_SetParameters(NativePtr, (ulong)parameters.LongLength, parameters);
            HRESULT.ThrowIfFailed(hr, "Set parameters on APSIClient");
        }

        /// <summary>
        /// Create an OPRF request
        /// </summary>
        /// <param name="items">Items to query</param>
        /// <returns>Byte array to send to APSI server</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public byte[] CreateOPRFRequest(ulong[,] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (items.GetLength(dimension: 1) != 2)
                throw new ArgumentException($"{nameof(items)} should be an array of pairs of ulongs");

            // Get the count of ulongs
            ulong itemCount = (ulong)items.GetLongLength(dimension: 0);
            ulong oprfRequestSize = 0;
            IntPtr oprfRequestPtr = IntPtr.Zero;
            uint hr = NativeMethods.APSIClient_CreateOPRFRequest(NativePtr, itemCount, items, ref oprfRequestSize, ref oprfRequestPtr);
            HRESULT.ThrowIfFailed(hr, "Create OPRF request");

            byte[] oprfRequest = new byte[oprfRequestSize];
            Marshal.Copy(oprfRequestPtr, oprfRequest, startIndex: 0, length: (int)oprfRequestSize);
            hr = NativeMethods.APSIClient_ReleaseNativePointer(oprfRequestPtr);
            HRESULT.ThrowIfFailed(hr, "Release OPRF request");

            return oprfRequest;
        }

        /// <summary>
        /// Extract hashed items from an OPRF response received from an APSI server
        /// </summary>
        /// <param name="oprfResponse">OPRF Response received from an APSI server</param>
        /// <returns>Hashed items that should be used to create an APSI query</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ulong[,] ExtractHashes(byte[] oprfResponse)
        {
            if (null == oprfResponse)
                throw new ArgumentNullException(nameof(oprfResponse));

            ulong hashedItemCount = 0;
            IntPtr hashedItemsPtr = IntPtr.Zero;
            uint hr = NativeMethods.APSIClient_ExtractHashes(NativePtr, (ulong)oprfResponse.LongLength, oprfResponse, ref hashedItemCount, ref hashedItemsPtr);
            HRESULT.ThrowIfFailed(hr, "Extract hashes");

            int hashedItemsBytesLength = (int)hashedItemCount * sizeof(ulong) * 2;
            byte[] hashedItemsBytes = new byte[hashedItemsBytesLength];
            Marshal.Copy(hashedItemsPtr, hashedItemsBytes, startIndex: 0, length: hashedItemsBytesLength);
            hr = NativeMethods.APSIClient_ReleaseNativePointer(hashedItemsPtr);
            HRESULT.ThrowIfFailed(hr, "Release hashed items");

            ulong[,] hashedItems = new ulong[hashedItemCount, 2];
            for (int idx = 0; idx < (int)hashedItemCount; idx++)
            {
                hashedItems[idx, 0] = BitConverter.ToUInt64(hashedItemsBytes, sizeof(ulong) * (idx * 2));
                hashedItems[idx, 1] = BitConverter.ToUInt64(hashedItemsBytes, sizeof(ulong) * (idx * 2 + 1));
            }

            return hashedItems;
        }

        /// <summary>
        /// Create an APSI query to send to an APSI server
        /// </summary>
        /// <param name="items">Hashed items</param>
        /// <returns>Byte array containing the encrypted query to send to an APSI server</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public byte[] CreateQuery(ulong[,] items)
        {
            if (null == items)
                throw new ArgumentNullException(nameof(items));
            if (items.GetLength(dimension: 1) != 2)
                throw new ArgumentException($"{nameof(items)} should be an array of pairs of ulongs");

            ulong itemCount = (ulong)items.GetLongLength(dimension: 0);
            ulong queryBufferSize = 0;
            IntPtr encryptedQueryPtr = IntPtr.Zero;
            uint hr = NativeMethods.APSIClient_CreateQuery(NativePtr, itemCount, items, ref queryBufferSize, ref encryptedQueryPtr);
            HRESULT.ThrowIfFailed(hr, "Create query");

            byte[] encryptedQuery = new byte[queryBufferSize];
            Marshal.Copy(encryptedQueryPtr, encryptedQuery, startIndex: 0, length: (int)queryBufferSize);
            hr = NativeMethods.APSIClient_ReleaseNativePointer(encryptedQueryPtr);
            HRESULT.ThrowIfFailed(hr, "Release encrypted query");

            return encryptedQuery;
        }

        /// <summary>
        /// Process the encrypted result of a Query response received from an APSI server
        /// </summary>
        /// <param name="encryptedResult">Encrypted result to process</param>
        /// <returns>Array with the intersection result</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool[] ProcessResult(byte[] encryptedResult)
        {
            if (null == encryptedResult)
                throw new ArgumentNullException(nameof(encryptedResult));

            ulong intersectionSize = 0;
            IntPtr intersectionPtr = IntPtr.Zero;
            uint hr = NativeMethods.APSIClient_ProcessResult(NativePtr, (ulong)encryptedResult.LongLength, encryptedResult, ref intersectionSize, ref intersectionPtr);
            HRESULT.ThrowIfFailed(hr, "Process result");

            bool[] intersection = new bool[intersectionSize];
            byte[] intersectionBt = new byte[intersectionSize];
            Marshal.Copy(intersectionPtr, intersectionBt, startIndex: 0, length: intersectionBt.Length);
            hr = NativeMethods.APSIClient_ReleaseNativePointer(intersectionPtr);
            HRESULT.ThrowIfFailed(hr, "Release intersection");

            int intersectionIndex = 0;
            foreach (byte bt in intersectionBt)
            {
                intersection[intersectionIndex++] = (bt == 1);
            }

            return intersection;
        }

        /// <summary>
        /// Release native instance
        /// </summary>
        protected override void DestroyNativeObject()
        {
            NativeMethods.APSIClient_Destroy(NativePtr);
        }
    }
}
