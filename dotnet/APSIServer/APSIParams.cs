// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;

using Microsoft.Research.APSI.Common;
using System;
using System.IO;

namespace Microsoft.Research.APSI.Server
{
    /// <summary>
    /// Represents a set of APSI parameters
    /// </summary>
    public class APSIParams : NativeObject
    {
        private const int MaxFileSize = 100000;

        /// <summary>
        /// Create a PSIParams object from a JSON string or a file containing JSON on disk.
        /// </summary>
        /// <param name="jsonOrPath">Path to a file on disk or JSON text describing parameters</param>
        /// <exception cref="ArgumentNullException">If <paramref name="jsonOrPath"/> is null</exception>
        /// <exception cref="ArgumentException">If <paramref name="jsonOrPath"/> is not a valid JSON string or file on disk</exception>
        public APSIParams(string jsonOrPath)
        {
            if (null == jsonOrPath)
                throw new ArgumentNullException(nameof(jsonOrPath));

            string jsonString;

            if (File.Exists(jsonOrPath))
            {
                FileInfo info = new FileInfo(jsonOrPath);
                if (info.Length > MaxFileSize)
                    throw new ArgumentException($"File '{jsonOrPath}' is more than {MaxFileSize} bytes.");

                jsonString = File.ReadAllText(jsonOrPath);
            }
            else
            {
                jsonString = jsonOrPath;
            }

            uint hr = NativeMethods.APSIParams_Create(out IntPtr thisptr, jsonString);
            HRESULT.ThrowIfFailed(hr, "Create APSIParams from JSON string");

            NativePtr = thisptr;
        }

        /// <summary>
        /// Destroy the native instance
        /// </summary>
        protected override void DestroyNativeObject()
        {
            NativeMethods.APSIParams_Destroy(NativePtr);
        }
    }
}
