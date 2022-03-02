// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

using Microsoft.Research.APSI.Common;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Research.APSI.Server
{
    /// <summary>
    /// Represents an APSI Server object
    /// </summary>
    public class APSIServer : NativeObject
    {
        /// <summary>
        /// Create an instance of an APSIServer object
        /// </summary>
        /// <param name="parameters">APSI parameters</param>
        /// <param name="oprfKey">OPRF key used to preprocess the data</param>
        public APSIServer(APSIParams parameters, OPRFKey oprfKey = null)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (null == oprfKey)
            {
                oprfKey = new OPRFKey();
            }

            uint hr = NativeMethods.APSIServer_Create(out IntPtr thisptr, oprfKey.NativePtr, parameters.NativePtr);
            HRESULT.ThrowIfFailed(hr, "Create APSIServer");
            NativePtr = thisptr;
        }

        /// <summary>
        /// Create an instance of an APSIServer object
        /// </summary>
        /// <param name="thisPtr">Initialize with an existing native pointer</param>
        private APSIServer(IntPtr thisPtr)
        {
            NativePtr = thisPtr;
        }

        /// <summary>
        /// Get the parameters for the current instance
        /// </summary>
        /// <returns>Parameters in a byte array</returns>
        public byte[] GetParameters()
        {
            ulong paramsSize = 0;
            IntPtr paramsPtr = IntPtr.Zero;
            uint hr = NativeMethods.APSIServer_GetParameters(NativePtr, ref paramsSize, ref paramsPtr);
            HRESULT.ThrowIfFailed(hr, "Get parameters");

            byte[] parameters = new byte[paramsSize];
            Marshal.Copy(paramsPtr, parameters, startIndex: 0, length: (int)paramsSize);

            hr = NativeMethods.APSIServer_ReleasePointer(paramsPtr);
            HRESULT.ThrowIfFailed(hr, "Release parameters");

            return parameters;
        }

        /// <summary>
        /// Process an encrypted query.
        /// </summary>
        /// <param name="encryptedQuery">Encrypted query to process</param>
        /// <returns>Byte array that must be sent to the client as a result, null if query failed</returns>
        public byte[] Query(byte[] encryptedQuery)
        {
            if (null == encryptedQuery)
                throw new ArgumentNullException(nameof(encryptedQuery));

            ulong resultSize = 0;
            IntPtr nativeBuffer = IntPtr.Zero;

            uint hr = NativeMethods.APSIServer_Query(NativePtr, (ulong)encryptedQuery.LongLength, encryptedQuery, ref resultSize, ref nativeBuffer);
            HRESULT.ThrowIfFailed(hr, "Query");

            byte[] resultBuffer = new byte[resultSize];

            Marshal.Copy(nativeBuffer, resultBuffer, startIndex: 0, length: (int)resultSize);

            hr = NativeMethods.APSIServer_ReleasePointer(nativeBuffer);
            HRESULT.ThrowIfFailed(hr, "Release query response");

            return resultBuffer;
        }

        /// <summary>
        /// Set the data for the server.
        /// 
        /// This method assumes data has not been processed for OPRF, and will perform
        /// the preprocessing with the OPRF key given in the constructor.
        /// </summary>
        /// <param name="data">Data to set</param>
        public void SetData(ulong[,] data)
        {
            if (null == data)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (2 != data.GetLength(dimension: 1))
            {
                throw new ArgumentException("Second dimension of data array needs to be size 2");
            }

            long count = data.GetLongLength(dimension: 0);

            uint hr = NativeMethods.APSIServer_SetData(NativePtr, (ulong)count, data);
            HRESULT.ThrowIfFailed(hr, "Set data");
        }

        /// <summary>
        /// Load database from the given byte array
        /// </summary>
        /// <param name="bytes">Byte array to load DB from</param>
        /// <returns>A new instance of APSIServer initialized from the stream</returns>
        public static APSIServer LoadDB(byte[] bytes)
        {
            if (null == bytes)
                throw new ArgumentNullException(nameof(bytes));

            uint hr = NativeMethods.APSIServer_LoadDB(out IntPtr thisPtr, (ulong)bytes.LongLength, bytes);
            HRESULT.ThrowIfFailed(hr, "Load DB");

            return new APSIServer(thisPtr);
        }

        /// <summary>
        /// Load database from the given file path
        /// </summary>
        /// <param name="filePath">Full path to the file where to load the DB from</param>
        /// <returns>A new instance of APSIServer initialized from the given file</returns>
        public static APSIServer LoadDB(string filePath)
        {
            if (null == filePath)
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new ArgumentException($"File '{filePath}' does not exist.");

            uint hr = NativeMethods.APSIServer_LoadDB(out IntPtr thisPtr, filePath);
            HRESULT.ThrowIfFailed(hr, "Load DB");

            return new APSIServer(thisPtr);
        }

        /// <summary>
        /// Save database to the given stream
        /// </summary>
        /// <param name="stream">Stream to save to</param>
        public void SaveDB(Stream stream)
        {
            if (null == stream)
                throw new ArgumentNullException(nameof(stream));

            ulong dbSize = 0;
            IntPtr dbBufferPtr = new IntPtr();
            uint hr = NativeMethods.APSIServer_SaveDB(NativePtr, ref dbSize, ref dbBufferPtr);
            HRESULT.ThrowIfFailed(hr, "Save DB");

            byte[] dbBuffer = new byte[dbSize];
            Marshal.Copy(dbBufferPtr, dbBuffer, startIndex: 0, length: (int)dbSize);
            hr = NativeMethods.APSIServer_ReleasePointer(dbBufferPtr);
            HRESULT.ThrowIfFailed(hr, "Release DB buffer");

            stream.Write(dbBuffer, offset: 0, count: (int)dbSize);
        }

        /// <summary>
        /// Save database to the given file path
        /// </summary>
        /// <param name="filePath">Full path to the file that will hold the saved database</param>
        public void SaveDB(string filePath)
        {
            if (null == filePath)
                throw new ArgumentNullException(nameof(filePath));

            uint hr = NativeMethods.APSIServer_SaveDB(NativePtr, filePath);
            HRESULT.ThrowIfFailed(hr, "Save DB");
        }

        /// <summary>
        /// Set the number of threads that will be used to preprocess data and respond to queries.
        /// </summary>
        /// <param name="threadCount">Number of threads to use.</param>
        public static void SetThreads(uint threadCount)
        {
            uint hr = NativeMethods.APSI_SetThreads(threadCount);
            HRESULT.ThrowIfFailed(hr, "Set threads");
        }

        /// <summary>
        /// Destroy native object
        /// </summary>
        protected override void DestroyNativeObject()
        {
            NativeMethods.APSIServer_Destroy(NativePtr);
        }
    }
}
