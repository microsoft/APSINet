using Microsoft.Research.APSI.Common;
using System;
using System.IO;
using System.Text;

namespace Microsoft.Research.APSI.Server
{
    /// <summary>
    /// Represents a key for OPRF
    /// </summary>
    public class OPRFKey : NativeObject
    {
        private const int StringRepresentationLength = 72;

        /// <summary>
        /// Create a new instance of an OPRFKey
        /// </summary>
        public OPRFKey()
        {
            uint hr = NativeMethods.OPRFKey_Create(out IntPtr thisptr);
            HRESULT.ThrowIfFailed(hr, "Create OPRFKey");
            NativePtr = thisptr;
        }

        /// <summary>
        /// Create a new instance of an OPRFKey from a string representation
        /// </summary>
        /// <param name="str">String with hexadecimal representation of an OPRF key</param>
        public OPRFKey(string str)
        {
            if (null == str)
                throw new ArgumentNullException(nameof(str));
            if (str.Length != StringRepresentationLength)
                throw new ArgumentException($"Length of input string is not {StringRepresentationLength}");

            uint hr = NativeMethods.OPRFKey_Create(out IntPtr thisptr);
            HRESULT.ThrowIfFailed(hr, "Create OPRFKey");
            NativePtr = thisptr;

            int numChars = str.Length;
            byte[] bytes = new byte[numChars / 2];
            for (int i = 0; i < numChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);
            }

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                Load(stream);
            }
        }

        /// <summary>
        /// Save the OPRF Key to a stream
        /// </summary>
        /// <param name="stream">Stream where the OPRF key will be saved</param>
        public void Save(Stream stream)
        {
            if (null == stream)
                throw new ArgumentNullException(nameof(stream));

            ulong keySize = 0;
            uint hr = NativeMethods.OPRFKey_Save(NativePtr, ref keySize, oprfKeyBuffer: null);
            HRESULT.ThrowIfFailed(hr, "Save OPRFKey: get size");

            byte[] buffer = new byte[keySize];
            hr = NativeMethods.OPRFKey_Save(NativePtr, ref keySize, buffer);
            HRESULT.ThrowIfFailed(hr, "Save OPRFKey");

            using (BinaryWriter writer = new BinaryWriter(stream, encoding: Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(buffer.Length);
                writer.Write(buffer);
            }
        }

        /// <summary>
        /// Load the OPRF key from a stream
        /// </summary>
        /// <param name="stream">Stream where the OPRF will be loaded from</param>
        public void Load(Stream stream)
        {
            if (null == stream)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer;
            using (BinaryReader reader = new BinaryReader(stream, encoding: Encoding.UTF8, leaveOpen: true))
            {
                int length = reader.ReadInt32();
                buffer = reader.ReadBytes(length);
            }

            uint hr = NativeMethods.OPRFKey_Load(NativePtr, (ulong)buffer.LongLength, buffer);
            HRESULT.ThrowIfFailed(hr, "Load OPRFKey");
        }

        /// <summary>
        /// Destroy native object
        /// </summary>
        protected override void DestroyNativeObject()
        {
            NativeMethods.OPRFKey_Destroy(NativePtr);
        }

        /// <summary>
        /// Get an hexadecimal string representation of this OPRF key
        /// </summary>
        /// <returns>String with an hexadecimal representation of the OPRF key</returns>
        public override string ToString()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Save(stream);
                byte[] bytes = stream.ToArray();

                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }
}
