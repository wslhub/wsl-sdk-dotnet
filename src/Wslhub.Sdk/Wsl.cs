using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Wslhub.Sdk
{
    public static class Wsl
    {
        public static void InitializeSecurityModel()
        {
            var result = NativeMethods.CoInitializeSecurity(
                IntPtr.Zero,
                (-1),
                IntPtr.Zero,
                IntPtr.Zero,
                NativeMethods.RpcAuthnLevel.Default,
                NativeMethods.RpcImpLevel.Impersonate,
                IntPtr.Zero,
                NativeMethods.EoAuthnCap.StaticCloaking,
                IntPtr.Zero);

            if (result != 0)
                throw new COMException("Cannot complete CoInitializeSecurity.", result);
        }

        private static bool InternalCheckIsWow64()
        {
            var osvi = new NativeMethods.OSVERSIONINFOEXW();
            osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);

            if (NativeMethods.GetVersionExW(ref osvi))
            {
                if ((osvi.dwMajorVersion == 5 && osvi.dwMinorVersion >= 1) ||
                    osvi.dwMajorVersion >= 6)
                {
                    if (NativeMethods.IsWow64Process(NativeMethods.GetCurrentProcess(), out bool retVal))
                        return retVal;
                }
            }

            return false;
        }

        public static void AssertWslSupported()
        {
            var commonErrorMessage = "Windows Subsystems for Linux requires 64-bit system and latest version of Windows 10 or higher than Windows Server 1709.";

            var is64BitProcess = (IntPtr.Size == 8);
            var is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

            if (!is64BitOperatingSystem || !is64BitProcess)
                throw new PlatformNotSupportedException(commonErrorMessage);

            var osvi = new NativeMethods.OSVERSIONINFOEXW();
            osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);

            if (!NativeMethods.GetVersionExW(ref osvi))
                throw new PlatformNotSupportedException(commonErrorMessage);

            if (osvi.dwPlatformId != 2)
                throw new PlatformNotSupportedException(commonErrorMessage);

            if (osvi.dwMajorVersion < 10 ||
                osvi.dwMinorVersion < 0 ||
                osvi.dwBuildNumber < 16299)
                throw new PlatformNotSupportedException(commonErrorMessage);

            var systemDirectory = Path.Combine(
                Environment.GetEnvironmentVariable("WINDIR"),
                "system32");

            if (!File.Exists(Path.Combine(systemDirectory, "wslapi.dll")))
                throw new NotSupportedException("This system does not have WSL enabled.");

            if (!File.Exists(Path.Combine(systemDirectory, "wsl.exe")))
                throw new NotSupportedException("This system does not have wsl.exe CLI.");
        }

        private static DistroRegistryInfo ReadFromRegistryKey(RegistryKey lxssKey, string keyName, Guid? parsedDefaultGuid)
        {
            if (!Guid.TryParse(keyName, out Guid parsedGuid))
                return null;

            using (var distroKey = lxssKey.OpenSubKey(keyName))
            {
                var distroName = distroKey.GetValue("DistributionName", default(string)) as string;

                if (string.IsNullOrWhiteSpace(distroName))
                    return null;

                var basePath = distroKey.GetValue("BasePath", default(string)) as string;
                var normalizedPath = Path.GetFullPath(basePath);

                var kernelCommandLine = (distroKey.GetValue("KernelCommandLine", default(string)) as string ?? string.Empty);
                var result = new DistroRegistryInfo()
                {
                    DistroId = parsedGuid,
                    DistroName = distroName,
                    BasePath = normalizedPath,
                };
                result.KernelCommandLine.AddRange(kernelCommandLine.Split(
                    new char[] { ' ', '\t', },
                    StringSplitOptions.RemoveEmptyEntries));

                if (parsedDefaultGuid.HasValue && parsedDefaultGuid == parsedGuid)
                {
                    result.IsDefault = true;
                    return result;
                }
            }

            return null;
        }

        public static DistroRegistryInfo GetDefaultDistro()
        {
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using (var lxssKey = currentUser.OpenSubKey(lxssPath, false))
            {
                var defaultGuid = Guid.TryParse(
                    lxssKey.GetValue("DefaultDistribution", default(string)) as string,
                    out Guid parsedDefaultGuid) ? parsedDefaultGuid : default(Guid?);

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    var info = ReadFromRegistryKey(lxssKey, keyName, defaultGuid);

                    if (info == null)
                        continue;

                    if (info.IsDefault)
                        return info;
                }
            }

            return null;
        }

        public static IEnumerable<DistroRegistryInfo> GetDistroListFromRegistry()
        {
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using (var lxssKey = currentUser.OpenSubKey(lxssPath, false))
            {
                var defaultGuid = Guid.TryParse(
                    lxssKey.GetValue("DefaultDistribution", default(string)) as string,
                    out Guid parsedDefaultGuid) ? parsedDefaultGuid : default(Guid?);

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    var info = ReadFromRegistryKey(lxssKey, keyName, defaultGuid);

                    if (info == null)
                        continue;

                    if (info.IsDefault)
                        yield return info;
                }
            }
        }

        public unsafe static IEnumerable<DistroInfo> GetDistroQueryResult()
        {
            AssertWslSupported();

            var results = new List<DistroInfo>();

            foreach (var eachItem in GetDistroListFromRegistry())
            {
                var distro = new DistroInfo()
                {
                    DistroId = eachItem.DistroId,
                    DistroName = eachItem.DistroName,
                    BasePath = eachItem.BasePath,
                };
                distro.KernelCommandLine.AddRange(eachItem.KernelCommandLine);
                results.Add(distro);

                distro.IsRegistered = NativeMethods.WslIsDistributionRegistered(eachItem.DistroName);

                if (!distro.IsRegistered)
                    continue;

                var hr = NativeMethods.WslGetDistributionConfiguration(
                    eachItem.DistroName,
                    out int distroVersion,
                    out int defaultUserId,
                    out DistroFlags flags,
                    out IntPtr environmentVariables,
                    out int environmentVariableCount);

                if (hr != 0)
                    continue;

                distro.WslVersion = distroVersion;
                distro.DefaultUid = defaultUserId;
                distro.DistroFlags = flags;

                var lpEnvironmentVariables = (byte***)environmentVariables.ToPointer();

                for (int i = 0; i < environmentVariableCount; i++)
                {
                    byte** lpArray = lpEnvironmentVariables[i];
                    var content = Marshal.PtrToStringAnsi(new IntPtr(lpArray));
                    distro.DefaultEnvironmentVariables.Add(content);
                    Marshal.FreeCoTaskMem(new IntPtr(lpArray));
                }

                Marshal.FreeCoTaskMem(new IntPtr(lpEnvironmentVariables));
            }

            return results;
        }

        public static unsafe string RunWslCommand(string distroName, string commandLine, int bufferLength = 65536)
        {
            var isRegistered = NativeMethods.WslIsDistributionRegistered(distroName);

            if (!isRegistered)
                throw new Exception($"{distroName} is not registered distro.");

            var stdin = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
            var stderr = NativeMethods.GetStdHandle(NativeMethods.STD_ERROR_HANDLE);

            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = true,
            };
            attributes.nLength = Marshal.SizeOf(attributes);

            if (!NativeMethods.CreatePipe(out IntPtr readPipe, out IntPtr writePipe, ref attributes, 0))
                throw new Exception("Cannot create pipe for I/O.");

            try
            {
                var hr = NativeMethods.WslLaunch(distroName, commandLine, false, stdin, writePipe, stderr, out IntPtr child);

                if (hr < 0)
                    throw new COMException("Cannot launch WSL process", hr);

                NativeMethods.WaitForSingleObject(child, NativeMethods.INFINITE);

                if ((NativeMethods.GetExitCodeProcess(child, out int exitCode) == false) || (exitCode != 0))
                    hr = NativeMethods.E_INVALIDARG;

                NativeMethods.CloseHandle(child);

                if (hr < 0)
                    throw new COMException("Cannot obtain output stream", hr);

                bufferLength = Math.Min(bufferLength, 1024);
                var bufferPointer = Marshal.AllocHGlobal(bufferLength);
                var outputContents = new StringBuilder();
                var encoding = new UTF8Encoding(false);
                var read = 0;

                while (true)
                {
                    if (!NativeMethods.ReadFile(readPipe, bufferPointer, bufferLength, out read, IntPtr.Zero))
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        Marshal.FreeHGlobal(bufferPointer);

                        if (lastError != 0)
                            throw new Win32Exception(lastError, "Cannot read data from pipe.");

                        break;
                    }

                    outputContents.Append(encoding.GetString((byte *)bufferPointer.ToPointer(), read));

                    if (read < bufferLength)
                    {
                        Marshal.FreeHGlobal(bufferPointer);
                        break;
                    }
                }

                return outputContents.ToString();
            }
            finally
            {
                NativeMethods.CloseHandle(readPipe);
                NativeMethods.CloseHandle(writePipe);
            }
        }
    }
}
