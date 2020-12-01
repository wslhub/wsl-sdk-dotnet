#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Linq;

namespace Wslhub.Sdk.Test
{
    static class WslTest
    {
        static void Test_SystemAssertion()
        {
            Wsl.AssertWslSupported();
        }

        static void Test_GetDistroListFromRegistryTest()
        {
            var distros = Wsl.GetDistroListFromRegistry();

            if (distros == null)
                throw new ArgumentNullException(nameof(distros));
        }

        static void Test_QueryDistroInfo()
        {
            var distros = Wsl.GetDistroQueryResult();

            if (distros == null)
                throw new ArgumentNullException(nameof(distros));

            if (distros.Count(x => x.IsRegistered == false) > 0)
                throw new Exception("Cannot query distro properties.");
        }

        static void Test_ExecTest()
        {
            var distroName = Wsl.GetDistroListFromRegistry().Select(x => x.DistroName).First();

            var result = Wsl.RunWslCommand(distroName, "cat /etc/passwd");

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result.Length == 0)
                throw new Exception("No output.");
        }
    }
}

#pragma warning restore IDE0051 // Remove unused private members
