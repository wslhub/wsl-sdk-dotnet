﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        public static IEnumerable<DistroRegistryInfo> GetDistroListFromRegistry()
        {
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using (var lxssKey = currentUser.OpenSubKey(lxssPath, false))
            {
                var defaultGuid = lxssKey.GetValue("DefaultDistribution", default(string)) as string;
                var defaultGuidFound = Guid.TryParse(defaultGuid, out Guid parsedDefaultGuid);
                var results = new List<DistroRegistryInfo>();

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    if (!Guid.TryParse(keyName, out Guid parsedGuid))
                        continue;

                    using (var distroKey = lxssKey.OpenSubKey(keyName))
                    {
                        var distroName = distroKey.GetValue("DistributionName", default(string)) as string;

                        if (string.IsNullOrWhiteSpace(distroName))
                            continue;

                        var basePath = distroKey.GetValue("BasePath", default(string)) as string;
                        var normalizedPath = Path.GetFullPath(basePath);

                        var kernelCommandLine = (distroKey.GetValue("KernelCommandLine", default(string)) as string ?? string.Empty);
                        var result = new DistroRegistryInfo()
                        {
                            DistroId = parsedGuid,
                            DistroName = distroName,
                            BasePath = basePath,
                        };
                        result.KernelCommandLine.AddRange(kernelCommandLine.Split(
                            new char[] { ' ', '\t', },
                            StringSplitOptions.RemoveEmptyEntries));

                        if (defaultGuidFound && parsedDefaultGuid == parsedGuid)
                            result.IsDefault = true;
                        results.Add(result);
                    }
                }

                return results;
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

        public static string RunWslCommand(string distroName, string commandLine)
        {
            var isRegistered = NativeMethods.WslIsDistributionRegistered(distroName);

            if (!isRegistered)
                throw new Exception($"{distroName} is not registered distro.");

            var stdin = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
            var stdout = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
            var stderr = NativeMethods.GetStdHandle(NativeMethods.STD_ERROR_HANDLE);

            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = true,
            };
            attributes.nLength = Marshal.SizeOf(attributes);

            if (NativeMethods.CreatePipe(out IntPtr readPipe, out IntPtr writePipe, ref attributes, 0))
            {
                try
                {
                    var hr = NativeMethods.WslLaunch(distroName, commandLine, false, stdin, writePipe, stderr, out IntPtr child);

                    if (hr >= 0)
                    {
                        NativeMethods.WaitForSingleObject(child, NativeMethods.INFINITE);
                        if ((NativeMethods.GetExitCodeProcess(child, out int exitCode) == false) || (exitCode != 0))
                        {
                            hr = NativeMethods.E_INVALIDARG;
                        }

                        NativeMethods.CloseHandle(child);

                        if (hr >= 0)
                        {
                            // TODO: Support larger output (more than 64K)
                            var length = 65536;
                            var buffer = Marshal.AllocHGlobal(length);
                            NativeMethods.FillMemory(buffer, length, 0x00);

                            var readFileResult = NativeMethods.ReadFile(readPipe, buffer, length - 1, out int read, IntPtr.Zero);
                            var lastError = Marshal.GetLastWin32Error();

                            var outputContents = new StringBuilder();
                            outputContents.Append(Marshal.PtrToStringAnsi(buffer, read));

                            Marshal.FreeHGlobal(buffer);
                            return outputContents.ToString();
                        }
                        else
                            throw new COMException("Cannot obtain output stream", hr);
                    }
                    else
                        throw new COMException("Cannot launch WSL process", hr);
                }
                catch { throw; }
                finally
                {
                    NativeMethods.CloseHandle(readPipe);
                    NativeMethods.CloseHandle(writePipe);
                }
            }
            else
                throw new Exception("Cannot create pipe for I/O.");
        }
    }
}
