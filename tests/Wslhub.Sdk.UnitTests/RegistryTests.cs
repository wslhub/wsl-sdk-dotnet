using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Xunit;

namespace Wslhub.Sdk.UnitTests
{
    [SupportedOSPlatform("windows")]
    public sealed class RegistryTests
    {
        [WindowsFact]
        public void MissingRegistryKeyReturnsAnEmptyList()
        {
            Assert.Empty(Wsl.ReadDistroList(null));
        }

        [WindowsFact]
        public void EnumerationIncludesDefaultAndNonDefaultDistributions()
        {
            WithTestKey(key =>
            {
                var first = AddDistro(key, "first");
                var second = AddDistro(key, "second");
                key.SetValue("DefaultDistribution", first.ToString("B"));

                var distros = Wsl.ReadDistroList(key).ToArray();
                Assert.Equal(2, distros.Length);
                Assert.True(Assert.Single(distros, distro => distro.DistroId == first).IsDefault);
                Assert.False(Assert.Single(distros, distro => distro.DistroId == second).IsDefault);
            });
        }

        [WindowsFact]
        public void MissingOrInvalidDefaultDoesNotHideDistributions()
        {
            WithTestKey(key =>
            {
                AddDistro(key, "first");
                AddDistro(key, "second");
                Assert.Equal(2, Wsl.ReadDistroList(key).Count());
                key.SetValue("DefaultDistribution", "invalid-guid");
                Assert.Equal(2, Wsl.ReadDistroList(key).Count());
                Assert.All(Wsl.ReadDistroList(key), distro => Assert.False(distro.IsDefault));
            });
        }

        [WindowsFact]
        public void IncompleteOrDisappearingEntriesAreIgnored()
        {
            WithTestKey(key =>
            {
                using (var ignored = key.CreateSubKey("not-a-guid")) { }
                using (var incomplete = key.CreateSubKey(Guid.NewGuid().ToString("B")))
                    incomplete.SetValue("DistributionName", "missing-path");
                using (var missingName = key.CreateSubKey(Guid.NewGuid().ToString("B")))
                    missingName.SetValue("BasePath", Path.GetTempPath());
                Assert.Null(Wsl.ReadFromRegistryKey(key, Guid.NewGuid().ToString("B"), null));
                Assert.Empty(Wsl.ReadDistroList(key));
            });
        }

        [WindowsFact]
        public void RegistryMetadataRetainsNamePathAndKernelArguments()
        {
            WithTestKey(key =>
            {
                var id = AddDistro(key, "테스트");
                using (var distro = key.OpenSubKey(id.ToString("B"), writable: true))
                    distro!.SetValue("KernelCommandLine", "quiet\t debug  ");
                var info = Assert.Single(Wsl.ReadDistroList(key));
                Assert.Equal("테스트", info.DistroName);
                Assert.Equal(Path.GetFullPath(Path.GetTempPath()), info.BasePath);
                Assert.Equal(new[] { "quiet", "debug" }, info.KernelCommandLine);
            });
        }

        private static Guid AddDistro(RegistryKey parent, string name)
        {
            var id = Guid.NewGuid();
            using var key = parent.CreateSubKey(id.ToString("B"));
            key.SetValue("DistributionName", name);
            key.SetValue("BasePath", Path.GetTempPath());
            return id;
        }

        private static void WithTestKey(Action<RegistryKey> test)
        {
            // Never write to the user's real Lxss registry key.
            var path = @"SOFTWARE\Wslhub.Sdk.Tests-" + Guid.NewGuid().ToString("N");
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(path);
                test(key);
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
            }
        }
    }
}
