using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
#if NET10_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Wslhub.Sdk
{
    /// <summary>
    /// Provides functionality to help you call WSL from .NET applications.
    /// </summary>
    public static class Wsl
    {
        /// <summary>
        /// Call CoInitializeSecurity so that you can call the WSL API.
        /// </summary>
        public static void InitializeSecurityModel()
        {
            AssertWindows();
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

        /// <summary>Checks the current platform and the presence of WSL components without throwing for unsupported or missing components.</summary>
        /// <returns>A status describing the first unmet requirement, or <see cref="WslSupportStatus.Available"/>.</returns>
        /// <remarks>
        /// Safe to call on non-Windows systems and before COM initialization. Available means that wslapi.dll
        /// and wsl.exe exist; it does not guarantee that WSL can start or that a distribution is installed.
        /// </remarks>
        public static WslSupportStatus GetWslSupportStatus() => GetWslSupportStatus(WslEnvironment.Instance);

        internal static WslSupportStatus GetWslSupportStatus(IWslEnvironment environment)
        {
            if (!environment.IsWindows)
                return WslSupportStatus.UnsupportedOperatingSystem;
            if (!environment.Is64BitOperatingSystem || !environment.Is64BitProcess)
                return WslSupportStatus.UnsupportedArchitecture;

            var version = environment.GetOSVersion();
            if (version == null)
                return WslSupportStatus.OperatingSystemVersionUnavailable;
            if (version < new Version(10, 0, 16299))
                return WslSupportStatus.UnsupportedOperatingSystem;

            if (!environment.FileExists(Path.Combine(environment.SystemDirectory, "wslapi.dll")))
                return WslSupportStatus.WslApiNotFound;
            if (!environment.FileExists(Path.Combine(environment.SystemDirectory, "wsl.exe")))
                return WslSupportStatus.WslExecutableNotFound;

            return WslSupportStatus.Available;
        }

        /// <summary>Checks whether this process meets the platform requirements and can find the WSL components.</summary>
        /// <exception cref="PlatformNotSupportedException">The operating system or process does not meet the platform requirements.</exception>
        /// <exception cref="NotSupportedException">A WSL component is missing.</exception>
        public static void AssertWslSupported() => AssertWslSupported(GetWslSupportStatus());

        internal static void AssertWslSupported(WslSupportStatus status)
        {
            switch (status)
            {
                case WslSupportStatus.Available:
                    return;
                case WslSupportStatus.WslApiNotFound:
                    throw new NotSupportedException("This system does not have the WSL API (wslapi.dll).");
                case WslSupportStatus.WslExecutableNotFound:
                    throw new NotSupportedException("This system does not have wsl.exe CLI.");
                default:
                    throw new PlatformNotSupportedException(
                        "WSL requires a 64-bit process on 64-bit Windows 10 build 16299 or later. Status: " + status);
            }
        }

        private static void AssertWindows()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("WSL requires Windows.");
        }

        /// <summary>
        /// Reads WSL-related information from a registry key and returns it as a model object.
        /// </summary>
        /// <param name="lxssKey">Registry key from which to read information.</param>
        /// <param name="keyName">The GUID name under the LXSS registry key.</param>
        /// <param name="parsedDefaultGuid">Default distribution's GUID key as recorded in the LXSS registry key.</param>
        /// <returns>Returns the WSL distribution information obtained through registry information.</returns>
#if NET10_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        internal static DistroRegistryInfo? ReadFromRegistryKey(RegistryKey lxssKey, string keyName, Guid? parsedDefaultGuid)
        {
            if (!Guid.TryParse(keyName, out Guid parsedGuid))
                return null;

            using (var distroKey = lxssKey.OpenSubKey(keyName))
            {
                if (distroKey == null)
                    return null;

                var distroName = distroKey.GetValue("DistributionName", default(string)) as string;

                if (distroName == null || string.IsNullOrWhiteSpace(distroName))
                    return null;

                var basePath = distroKey.GetValue("BasePath", default(string)) as string;
                if (basePath == null || string.IsNullOrWhiteSpace(basePath))
                    return null;

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
                }

                return result;
            }
        }

        /// <summary>
        /// Returns information about the default WSL distribution from the registry.
        /// </summary>
        /// <returns>
        /// Returns default WSL distribution information obtained through registry information.
        /// Returns null if no WSL distro is installed or no distro is set as the default.
        /// </returns>
#if NET10_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        public static DistroRegistryInfo? GetDefaultDistro()
        {
            foreach (var distro in GetDistroListFromRegistry())
            {
                if (distro.IsDefault)
                    return distro;
            }

            return null;
        }

        /// <summary>
        /// Returns information about WSL distributions obtained from the registry without calling the WSL API.
        /// </summary>
        /// <returns>Returns a list of information about the searched WSL distributions.</returns>
#if NET10_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        public static IEnumerable<DistroRegistryInfo> GetDistroListFromRegistry()
        {
            AssertWindows();
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using (var lxssKey = currentUser.OpenSubKey(lxssPath, false))
            {
                foreach (var info in ReadDistroList(lxssKey))
                    yield return info;
            }
        }

#if NET10_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        internal static IEnumerable<DistroRegistryInfo> ReadDistroList(RegistryKey? lxssKey)
        {
            if (lxssKey == null)
                yield break;

            var defaultGuid = Guid.TryParse(
                lxssKey.GetValue("DefaultDistribution", default(string)) as string,
                out Guid parsedDefaultGuid) ? parsedDefaultGuid : default(Guid?);

            foreach (var keyName in lxssKey.GetSubKeyNames())
            {
                var info = ReadFromRegistryKey(lxssKey, keyName, defaultGuid);
                if (info != null)
                    yield return info;
            }
        }

        /// <summary>
        /// Get details of WSL distributions reported as installed on the system by calling the WSL API.
        /// </summary>
        /// <returns>Returns the list of WSL distributions inquired for detailed information with the WSL API.</returns>
#if NET10_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        public static IEnumerable<DistroInfo> GetDistroQueryResult()
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
                    IsDefault = eachItem.IsDefault,
                    IsDefaultDistro = eachItem.IsDefault,
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

                try
                {
                    for (int i = 0; i < environmentVariableCount; i++)
                    {
                        var pointer = Marshal.ReadIntPtr(environmentVariables, i * IntPtr.Size);
                        distro.DefaultEnvironmentVariables.Add(Marshal.PtrToStringAnsi(pointer) ?? string.Empty);
                    }
                }
                finally
                {
                    for (int i = 0; i < environmentVariableCount; i++)
                        Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(environmentVariables, i * IntPtr.Size));
                    Marshal.FreeCoTaskMem(environmentVariables);
                }
            }

            return results;
        }

        /// <summary>
        /// Execute the specified command through the default shell of a specific WSL distribution, and get the result as a System.IO.Stream object.
        /// </summary>
        /// <param name="distroName">The name of the WSL distribution on which to run the command.</param>
        /// <param name="commandLine">The command you want to run.</param>
        /// <param name="outputStream">The System.IO.Stream object to receive the results. It must be writable.</param>
        /// <param name="bufferLength">Specifies the size of the buffer array to use when copying from anonymous pipes to the underlying stream. You do not need to specify a value.</param>
        /// <returns>Returns the sum of the number of bytes received.</returns>
        public static long RunWslCommand(string distroName, string commandLine, Stream outputStream, int bufferLength = 65536)
        {
            if (string.IsNullOrWhiteSpace(distroName))
                throw new ArgumentException("A distribution name is required.", nameof(distroName));
            if (commandLine == null)
                throw new ArgumentNullException(nameof(commandLine));
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));
            if (!outputStream.CanWrite)
                throw new ArgumentException("The output stream must be writable.", nameof(outputStream));
            if (bufferLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferLength));

            AssertWslSupported();
            if (!NativeMethods.WslIsDistributionRegistered(distroName))
                throw new InvalidOperationException($"{distroName} is not a registered distribution.");

            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>(),
                bInheritHandle = true,
            };

            if (!NativeMethods.CreatePipe(out var readPipe, out var writePipe, ref attributes, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot create pipe for I/O.");

            using (readPipe)
            using (writePipe)
            {
                if (!NativeMethods.SetHandleInformation(readPipe, NativeMethods.HANDLE_FLAG_INHERIT, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot configure the output pipe.");

                var stdin = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
                var stderr = NativeMethods.GetStdHandle(NativeMethods.STD_ERROR_HANDLE);
                var hr = NativeMethods.WslLaunch(distroName, commandLine, false, stdin, writePipe, stderr, out var child);

                using (child)
                {
                    if (hr < 0)
                        throw new COMException("Cannot launch WSL process.", hr);

                    // Only the child may retain a writer, so EOF arrives when it closes stdout.
                    writePipe.Dispose();
                    // Drain stdout before waiting: otherwise a full pipe prevents the child from exiting.
                    var length = WslPipe.CopyTo(readPipe, outputStream, bufferLength);

                    if (NativeMethods.WaitForSingleObject(child, NativeMethods.INFINITE) == NativeMethods.WAIT_FAILED)
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot wait for the WSL process.");
                    if (!NativeMethods.GetExitCodeProcess(child, out int exitCode))
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot query the process exit code.");
                    if (exitCode != 0)
                        throw new InvalidOperationException($"Process exit code is non-zero: {exitCode}");

                    return length;
                }
            }
        }

        /// <summary>
        /// Execute the specified command through the default shell of a specific WSL distribution, and get the result as a string.
        /// </summary>
        /// <remarks>
        /// When receiving data from WSL, it is encoded as UTF-8 data without the byte order mark.
        /// </remarks>
        /// <param name="distroName">The name of the WSL distribution on which to run the command.</param>
        /// <param name="commandLine">The command you want to run.</param>
        /// <param name="bufferLength">Specifies the size of the buffer array to use when copying from anonymous pipes to the underlying stream. You do not need to specify a value.</param>
        /// <returns>Returns the collected output string.</returns>
        public static string RunWslCommand(string distroName, string commandLine, int bufferLength = 65536)
        {
            using (var output = new MemoryStream())
            {
                RunWslCommand(distroName, commandLine, output, bufferLength);
                return new UTF8Encoding(false).GetString(output.ToArray());
            }
        }
    }
}
