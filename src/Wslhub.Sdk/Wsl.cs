using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Wslhub.Sdk
{
    public static class Wsl
    {
        public static void AssertWslSupported()
        {
            var commonErrorMessage = "Windows Subsystems for Linux requires 64-bit system and latest version of Windows 10 or higher than Windows Server 1709.";

            if (!Environment.Is64BitOperatingSystem || !Environment.Is64BitProcess)
                throw new PlatformNotSupportedException(commonErrorMessage);

            var osVersion = Environment.OSVersion;

            if (osVersion.Platform != PlatformID.Win32NT)
                throw new PlatformNotSupportedException(commonErrorMessage);

            var versionNumber = osVersion.Version;

            if (versionNumber.Major < 10 ||
                versionNumber.Minor < 0 ||
                versionNumber.Build < 16299)
                throw new PlatformNotSupportedException(commonErrorMessage);

            if (!File.Exists(Path.Combine(Environment.SystemDirectory, "wslapi.dll")))
                throw new NotSupportedException("This system does not have WSL enabled.");

            if (!File.Exists(Path.Combine(Environment.SystemDirectory, "wsl.exe")))
                throw new NotSupportedException("This system does not have wsl.exe CLI.");
        }

        public static IEnumerable<DistroRegistryInfo> GetDistroListFromRegistry()
        {
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using var lxssKey = currentUser.OpenSubKey(lxssPath, false);
            var defaultGuid = lxssKey.GetValue("DefaultDistribution", default(string)) as string;
            var defaultGuidFound = Guid.TryParse(defaultGuid, out Guid parsedDefaultGuid);
            var results = new List<DistroRegistryInfo>();

            foreach (var keyName in lxssKey.GetSubKeyNames())
            {
                if (!Guid.TryParse(keyName, out Guid parsedGuid))
                    continue;

                using var distroKey = lxssKey.OpenSubKey(keyName);
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

            return results;
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

                distro.HResult = NativeMethods.WslGetDistributionConfiguration(
                    eachItem.DistroName,
                    out int distroVersion,
                    out int defaultUserId,
                    out DistroFlags flags,
                    out IntPtr environmentVariables,
                    out int environmentVariableCount);

                if (!distro.Succeed)
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
    }
}
