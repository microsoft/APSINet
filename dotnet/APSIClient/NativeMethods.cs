using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Research.APSI.Client
{
    /// <summary>
    /// Provides the API for the native APSI Client
    /// </summary>
    public static class NativeMethods
    {
        private const string APSIClientNative = "APSIClientNative";

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_Create(out IntPtr thisptr);

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_Destroy(IntPtr thisptr);

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_SetParameters(IntPtr thisptr, ulong paramsSize, byte[] parameters);

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_CreateOPRFRequest(IntPtr thisptr, ulong itemCount, ulong[,] items, ref ulong oprfRequestSize, ref IntPtr oprfRequest);

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_ExtractHashes(IntPtr thisptr, ulong oprfResponseSize, byte[] oprfResponse, ref ulong hashedItemCount, ref IntPtr hashedItems);

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_CreateQuery(IntPtr thisptr, ulong itemCount, ulong[,] items, ref ulong encryptedQuerySize, ref IntPtr encryptedQuery);

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_ProcessResult(IntPtr thisptr, ulong encryptedResultSize, byte[] encrptedResult, ref ulong intersectionSize, ref IntPtr intersection);

        [DllImport(APSIClientNative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIClient_ReleaseNativePointer(IntPtr nativePointer);
    }
}
