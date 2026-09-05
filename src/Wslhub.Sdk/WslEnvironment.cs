using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Wslhub.Sdk
{
    internal interface IWslEnvironment
    {
        bool IsWindows { get; }
        bool Is64BitOperatingSystem { get; }
        bool Is64BitProcess { get; }
        Version? GetOSVersion();
        string SystemDirectory { get; }
        bool FileExists(string path);
    }

    internal sealed class WslEnvironment : IWslEnvironment
    {
        internal static readonly WslEnvironment Instance = new WslEnvironment();

        public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public bool Is64BitOperatingSystem => Environment.Is64BitOperatingSystem;
        public bool Is64BitProcess => Environment.Is64BitProcess;
        public string SystemDirectory => Environment.SystemDirectory;
        public bool FileExists(string path) => File.Exists(path);

        public Version? GetOSVersion()
        {
            var version = new NativeMethods.OSVERSIONINFOEXW();
            version.dwOSVersionInfoSize = (uint)Marshal.SizeOf<NativeMethods.OSVERSIONINFOEXW>();
            return NativeMethods.RtlGetVersion(ref version) == 0
                ? new Version((int)version.dwMajorVersion, (int)version.dwMinorVersion, (int)version.dwBuildNumber)
                : null;
        }
    }
}
