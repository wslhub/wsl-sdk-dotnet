# WSL SDK for .NET

[![NuGet version (Wslhub.Sdk)](https://img.shields.io/nuget/v/Wslhub.Sdk.svg?style=flat-square)](https://www.nuget.org/packages/Wslhub.Sdk/)

Wslhub.Sdk wraps the [Windows Subsystem for Linux API](https://learn.microsoft.com/en-us/windows/win32/api/wslapi/) for .NET applications. It enumerates registered distributions, reads their configuration, and captures command output as text or a stream.

The source targets .NET 10 and .NET Standard 2.0 and builds with the .NET 10 SDK. WSL operations require a 64-bit Windows process. Status detection also works on non-Windows hosts.

## Runtime compatibility

- `net10.0`: uses the registry APIs included in .NET 10, without additional runtime package dependencies.
- `netstandard2.0`: supports compatible existing applications, including .NET Framework 4.7.2 and later, with `Microsoft.Win32.Registry` 5.0.0.
- The previous `netstandard1.3` target is no longer included. Applications limited to .NET Standard 1.3 require a runtime upgrade before adopting this source version.

The SDK's platform check retains the Windows 10 build 16299 minimum for WSL APIs. The application's runtime can impose a newer minimum; see [.NET supported OS requirements](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md). Building on macOS or Linux does not make WSL operations available on those operating systems.

## WSL support status

Call `GetWslSupportStatus()` to distinguish platform limitations and missing components without catching exceptions. This call does not initialize COM or launch WSL.

```csharp
using Wslhub.Sdk;

var status = Wsl.GetWslSupportStatus();
if (status != WslSupportStatus.Available)
{
    Console.Error.WriteLine($"WSL components are unavailable: {status}");
    return;
}
```

The first unmet requirement determines the result:

| Status | Meaning | Typical next step |
| --- | --- | --- |
| `UnsupportedOperatingSystem` | Non-Windows OS or Windows older than the API baseline | Use a supported Windows host |
| `UnsupportedArchitecture` | Operating system or current process is not 64-bit | Run an x64 or Arm64 process on 64-bit Windows |
| `OperatingSystemVersionUnavailable` | Windows version query failed | Investigate the host environment |
| `WslApiNotFound` | `wslapi.dll` is missing from the system directory | Check the WSL installation |
| `WslExecutableNotFound` | `wsl.exe` is missing from the system directory | Check the WSL installation |
| `Available` | Platform checks pass and both files exist | Initialize COM and use the WSL APIs |

`Available` describes component presence. It does not verify optional Windows features, virtualization settings, services, the Linux kernel, or an installed distribution. A Windows installation can contain WSL stubs even when WSL cannot start. For runtime diagnostics, use [`wsl --status` and `wsl --list --verbose`](https://learn.microsoft.com/en-us/windows/wsl/basic-commands).

`AssertWslSupported()` remains available and delegates to the same check. It preserves `PlatformNotSupportedException` for unsupported platforms and `NotSupportedException` for missing components.

## Application initialization

Call `Wsl.InitializeSecurityModel()` once at the beginning of your application's entry point, before other components initialize COM security. The initialization is process-wide and can fail if another component has already configured it. WSL execution tests therefore use a standalone console program rather than a test host that controls COM initialization. See the [WSL COM initialization discussion](https://github.com/microsoft/WSL/issues/5824#issuecomment-685231813).

For Windows executable applications, use an application manifest with `requestedExecutionLevel` set to `asInvoker` and the Windows 10 compatibility GUID `8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a`. The [integration test manifest](src/Wslhub.Sdk.Test/app.manifest) provides an example. Status detection itself does not require that manifest.

## Distribution queries and text output

This example targets .NET 10 and handles an absent default distribution explicitly.

```csharp
using Wslhub.Sdk;

if (!OperatingSystem.IsWindows())
    return;

Wsl.InitializeSecurityModel();
Wsl.AssertWslSupported();

foreach (var distro in Wsl.GetDistroListFromRegistry())
    Console.WriteLine($"{distro.DistroName}: default={distro.IsDefault}");

foreach (var distro in Wsl.GetDistroQueryResult())
    Console.WriteLine($"{distro.DistroName}: WSL {distro.WslVersion}");

var defaultDistro = Wsl.GetDefaultDistro();
if (defaultDistro is null)
{
    Console.Error.WriteLine("No default WSL distribution is installed.");
    return;
}

Console.WriteLine(defaultDistro.RunWslCommand("cat /etc/passwd"));
```

Registry enumeration returns all valid distributions, including non-default entries. If the WSL registry key is absent, enumeration is empty and `GetDefaultDistro()` returns `null`. Distribution configuration comes from [`WslGetDistributionConfiguration`](https://learn.microsoft.com/en-us/windows/win32/api/wslapi/nf-wslapi-wslgetdistributionconfiguration).

## Binary output and command completion

The stream overload copies stdout without decoding it and leaves the caller's output stream open.

```csharp
using System.IO.Compression;
using System.Text;
using Wslhub.Sdk;

if (!OperatingSystem.IsWindows())
    return;

Wsl.InitializeSecurityModel();
Wsl.AssertWslSupported();
var distro = Wsl.GetDefaultDistro()
    ?? throw new InvalidOperationException("No default WSL distribution is installed.");

using var output = new MemoryStream();
long bytesWritten = distro.RunWslCommand("ls /dev | gzip -", output);
output.Position = 0;
using var gzip = new GZipStream(output, CompressionMode.Decompress, leaveOpen: true);
using var reader = new StreamReader(gzip, new UTF8Encoding(false));
Console.WriteLine(reader.ReadToEnd());
```

Both overloads drain stdout while the command runs and wait for completion after the pipe reaches EOF. Short reads do not truncate output. The string overload decodes the collected UTF-8 bytes after completion, preserving characters split across reads. A non-zero exit code throws `InvalidOperationException`; the stream may already contain output when that exception occurs. Stdin and stderr use the calling process's standard handles. Commands run synchronously without a timeout or cancellation API. See [`WslLaunch`](https://learn.microsoft.com/en-us/windows/win32/api/wslapi/nf-wslapi-wsllaunch) and [anonymous-pipe EOF behavior](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile#pipes).

## Build and automated tests

Install a stable .NET 10 SDK, then run these commands from the repository root. [`global.json`](global.json) selects the latest installed .NET 10 feature band and excludes preview SDKs.

```shell
dotnet restore src/wsl-sdk-dotnet.sln
dotnet build src/wsl-sdk-dotnet.sln --configuration Release --no-restore
dotnet test tests/Wslhub.Sdk.UnitTests --configuration Release --no-build
dotnet pack src/Wslhub.Sdk --configuration Release --no-build --output artifacts/packages
```

The unit suite covers status classification, legacy exception types, input validation, isolated registry fixtures, and native pipe behavior. Windows-only tests skip on other hosts. Registry tests create and remove their own temporary keys without altering the user's WSL configuration. XML documentation, the README, and the license are included in the NuGet package. Packing creates local artifacts; it does not publish to NuGet.

## WSL integration tests

On a 64-bit Windows host with WSL and a default Linux distribution installed, run the standalone integration program. The distribution needs standard tools including `sh`, `cat`, `head`, `tr`, `sleep`, and `gzip`.

```shell
dotnet run --project src/Wslhub.Sdk.Test --configuration Release
```

The program tests registry queries, distribution configuration, text and gzip output, large output, delayed writes, UTF-8 boundaries, empty output, and non-zero exits. A missing Windows host, WSL installation, or default distribution causes failure rather than a successful test run. Integration commands do not install, unregister, or modify distributions.

The [CI workflow](.github/workflows/ci.yml) builds, tests, and packs on Windows, Linux, and macOS. Its Windows job also imports temporary Ubuntu distributions on the disposable runner for WSL 1 integration coverage. WSL 2, Arm64, and .NET Framework execution require separate validation before making compatibility claims about those environments.
