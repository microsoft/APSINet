using System;
using System.Runtime.InteropServices;

namespace Microsoft.Research.APSI.Server
{
    /// <summary>
    /// Provides the API for the native APSI Server
    /// </summary>
    public static class NativeMethods
    {
        private const string APSINative = "APSIServerNative";

        #region APSIServer methods

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIServer_GetParameters(IntPtr thisptr, ref ulong parametersSize, ref IntPtr parametersPtr);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIServer_Query(IntPtr thisptr, ulong encryptedQuerySize, byte[] encryptedQuery, ref ulong resultBufferSize, ref IntPtr resultBuffer);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIServer_ReleasePointer(IntPtr ptr);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIServer_SetData(IntPtr thisptr, ulong count, ulong[,] data);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, PreserveSig = true)]
        internal static extern uint APSIServer_Create(out IntPtr thisptr, IntPtr oprf_key, IntPtr parameters);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIServer_Destroy(IntPtr thisptr);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, EntryPoint = "APSIServer_SaveDB1", PreserveSig = true)]
        internal static extern uint APSIServer_SaveDB(IntPtr thisptr, ref ulong dbBufferSize, ref IntPtr dbBuffer);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, EntryPoint = "APSIServer_SaveDB2", CharSet = CharSet.Ansi, PreserveSig = true)]
        internal static extern uint APSIServer_SaveDB(IntPtr thisptr, string filePath);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, EntryPoint = "APSIServer_LoadDB1", PreserveSig = true)]
        internal static extern uint APSIServer_LoadDB(out IntPtr thisptr, ulong dbBufferSize, byte[] dbBuffer);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, EntryPoint = "APSIServer_LoadDB2", CharSet = CharSet.Ansi, PreserveSig = true)]
        internal static extern uint APSIServer_LoadDB(out IntPtr thisptr, string filePath);

        #endregion

        #region OPRFKey methods

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint OPRFKey_Create(out IntPtr thisptr);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint OPRFKey_Destroy(IntPtr thisptr);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint OPRFKey_Save(IntPtr thisptr, ref ulong keySize, byte[] oprfKeyBuffer);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint OPRFKey_Load(IntPtr thisptr, ulong keySize, byte[] oprfKeyBuffer);

        #endregion

        #region OPRFSender methods

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint OPRFSender_RunOPRF(ulong encodedItemsSize, byte[] encodedItems, IntPtr oprfKey, ref ulong resultBufferSize, ref IntPtr resultBuffer);

        #endregion

        #region APSIParams methods

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, PreserveSig = true)]
        internal static extern uint APSIParams_Create(out IntPtr thisptr, string parameters);

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSIParams_Destroy(IntPtr thisptr);

        #endregion

        #region APSI methods

        [DllImport(APSINative, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern uint APSI_SetThreads(ulong threads);

        #endregion
    }
}
