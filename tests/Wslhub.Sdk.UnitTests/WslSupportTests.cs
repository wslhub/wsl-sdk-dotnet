using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Wslhub.Sdk.UnitTests
{
    public sealed class WslSupportTests
    {
        [Fact]
        public void NonWindowsDoesNotProbeWindowsVersionOrFiles()
        {
            var environment = new TestEnvironment { IsWindows = false, RejectProbes = true };
            Assert.Equal(WslSupportStatus.UnsupportedOperatingSystem, Wsl.GetWslSupportStatus(environment));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void UnsupportedArchitectureDoesNotProbeVersionOrFiles(bool os64, bool process64)
        {
            var environment = new TestEnvironment
            {
                Is64BitOperatingSystem = os64,
                Is64BitProcess = process64,
                RejectProbes = true,
            };
            Assert.Equal(WslSupportStatus.UnsupportedArchitecture, Wsl.GetWslSupportStatus(environment));
        }

        [Theory]
        [InlineData(6, 3, 9600, WslSupportStatus.UnsupportedOperatingSystem)]
        [InlineData(10, 0, 16298, WslSupportStatus.UnsupportedOperatingSystem)]
        [InlineData(10, 0, 16299, WslSupportStatus.Available)]
        [InlineData(10, 0, 26100, WslSupportStatus.Available)]
        [InlineData(11, 0, 1, WslSupportStatus.Available)]
        public void WindowsVersionUsesAnInclusiveMinimum(int major, int minor, int build, WslSupportStatus expected)
        {
            var environment = new TestEnvironment { Version = new Version(major, minor, build) };
            Assert.Equal(expected, Wsl.GetWslSupportStatus(environment));
            if (expected == WslSupportStatus.UnsupportedOperatingSystem)
                Assert.Empty(environment.ProbedFiles);
        }

        [Fact]
        public void FailedVersionQueryHasItsOwnStatus()
        {
            var environment = new TestEnvironment { Version = null };
            Assert.Equal(WslSupportStatus.OperatingSystemVersionUnavailable, Wsl.GetWslSupportStatus(environment));
            Assert.Empty(environment.ProbedFiles);
        }

        [Theory]
        [InlineData(false, false, WslSupportStatus.WslApiNotFound)]
        [InlineData(false, true, WslSupportStatus.WslApiNotFound)]
        [InlineData(true, false, WslSupportStatus.WslExecutableNotFound)]
        [InlineData(true, true, WslSupportStatus.Available)]
        public void MissingComponentsAreDistinguished(bool api, bool executable, WslSupportStatus expected)
        {
            var environment = new TestEnvironment { HasApi = api, HasExecutable = executable };
            Assert.Equal(expected, Wsl.GetWslSupportStatus(environment));
            Assert.Equal(Path.Combine(environment.SystemDirectory, "wslapi.dll"), environment.ProbedFiles[0]);
            if (api)
                Assert.Equal(Path.Combine(environment.SystemDirectory, "wsl.exe"), environment.ProbedFiles[1]);
        }

        [Theory]
        [InlineData(WslSupportStatus.UnsupportedOperatingSystem)]
        [InlineData(WslSupportStatus.UnsupportedArchitecture)]
        [InlineData(WslSupportStatus.OperatingSystemVersionUnavailable)]
        public void AssertionPreservesPlatformException(WslSupportStatus status)
        {
            Assert.Throws<PlatformNotSupportedException>(() => Wsl.AssertWslSupported(status));
        }

        [Theory]
        [InlineData(WslSupportStatus.WslApiNotFound)]
        [InlineData(WslSupportStatus.WslExecutableNotFound)]
        public void AssertionPreservesMissingComponentException(WslSupportStatus status)
        {
            Assert.Throws<NotSupportedException>(() => Wsl.AssertWslSupported(status));
        }

        [Fact]
        public void AvailableDoesNotThrow() => Wsl.AssertWslSupported(WslSupportStatus.Available);

        [Fact]
        public void ActualHostStatusMatchesAssertion()
        {
            var status = Wsl.GetWslSupportStatus();
            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(WslSupportStatus.UnsupportedOperatingSystem, status);
                Assert.Throws<PlatformNotSupportedException>(Wsl.InitializeSecurityModel);
            }

            var expected = Record.Exception(() => Wsl.AssertWslSupported(status));
            var actual = Record.Exception(Wsl.AssertWslSupported);
            Assert.Equal(expected?.GetType(), actual?.GetType());
        }

        private sealed class TestEnvironment : IWslEnvironment
        {
            public bool IsWindows { get; set; } = true;
            public bool Is64BitOperatingSystem { get; set; } = true;
            public bool Is64BitProcess { get; set; } = true;
            public Version? Version { get; set; } = new Version(10, 0, 26100);
            public string SystemDirectory => Path.Combine("system-root", "System32");
            public bool HasApi { get; set; } = true;
            public bool HasExecutable { get; set; } = true;
            public bool RejectProbes { get; set; }
            public List<string> ProbedFiles { get; } = new List<string>();

            public Version? GetOSVersion()
            {
                Assert.False(RejectProbes);
                return Version;
            }

            public bool FileExists(string path)
            {
                Assert.False(RejectProbes);
                ProbedFiles.Add(path);
                return Path.GetFileName(path) == "wslapi.dll" ? HasApi : HasExecutable;
            }
        }
    }
}
