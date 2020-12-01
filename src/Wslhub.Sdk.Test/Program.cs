using Bullseye;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Wslhub.Sdk.Test
{
    internal static partial class Program
    {
        private static void Main()
        {
            Wsl.InitializeSecurityModel();

            var methods = typeof(WslTest)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic| BindingFlags.Static)
                .Where(x => x.Name.StartsWith("Test_", StringComparison.Ordinal))
                .OrderBy(x => Guid.NewGuid())
                .ToArray();

            var targets = new Targets();
            var methodNames = new List<string>(methods.Length);

            foreach (var eachMethod in methods)
            {
                methodNames.Add(eachMethod.Name);
                targets.Add(eachMethod.Name, () => eachMethod.Invoke(null, null));
            }

            targets.RunAndExit(methodNames);
        }
    }
}
