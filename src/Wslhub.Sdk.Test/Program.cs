using System;
using System.Linq;
using System.Reflection;

namespace Wslhub.Sdk.Test
{
    internal static class Program
    {
        private static int Main()
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("WSL integration tests require Windows and an installed default distribution.");
                return 1;
            }

            // Initialization belongs to this standalone process, before any WSL calls.
            Wsl.InitializeSecurityModel();
            Wsl.AssertWslSupported();

            var methods = typeof(WslTest)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(method => method.Name.StartsWith("Test_", StringComparison.Ordinal))
                .OrderBy(method => method.Name, StringComparer.Ordinal);
            var failures = 0;

            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(null, null);
                    Console.WriteLine($"PASS {method.Name}");
                }
                catch (TargetInvocationException exception)
                {
                    Console.Error.WriteLine($"FAIL {method.Name}: {exception.InnerException}");
                    failures++;
                }
            }

            return failures == 0 ? 0 : 1;
        }
    }
}
