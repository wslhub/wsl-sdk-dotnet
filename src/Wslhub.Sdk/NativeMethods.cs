using System;
using System.Runtime.InteropServices;

namespace Wslhub.Sdk
{
    internal static class NativeMethods
    {
        [DllImport("wslapi.dll",
            CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WslIsDistributionRegistered(
            string distributionName);

        [DllImport("wslapi.dll",
            CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            PreserveSig = true)]
        public static extern int WslGetDistributionConfiguration(
            string distributionName,
            [MarshalAs(UnmanagedType.I4)] out int distributionVersion,
            [MarshalAs(UnmanagedType.I4)] out int defaultUID,
            [MarshalAs(UnmanagedType.I4)] out DistroFlags wslDistributionFlags,
            out IntPtr defaultEnvironmentVariables,
            [MarshalAs(UnmanagedType.I4)] out int defaultEnvironmentVariableCount);

        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            ExactSpelling = true,
            SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            ExactSpelling = true,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process);

        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVersionExW(ref OSVERSIONINFOEXW osvi);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OSVERSIONINFOEXW
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
    }
}
