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
                throw new Exception("Cannot query registry keys.");
        }

        static void Test_QueryDistroInfo()
        {
            var distros = Wsl.GetDistroQueryResult();

            if (distros == null)
                throw new ArgumentNullException(nameof(distros));

            if (distros.Count(x => x.IsRegistered == false) > 0)
                throw new Exception("Cannot query distro properties.");
        }

        static void Test_GetDefaultDistro()
        {
            var defaultDistro = Wsl.GetDefaultDistro();

            if (defaultDistro == null)
                throw new Exception("Cannot find the default distro.");
        }

        static void Test_ExecTest()
        {
            var outputContent = Wsl.GetDefaultDistro().RunWslCommand("cat /etc/passwd");

            if (outputContent == null)
                throw new ArgumentNullException(nameof(outputContent));

            if (outputContent.Length == 0)
                throw new Exception("No output.");
        }
    }
}

#pragma warning restore IDE0051 // Remove unused private members
