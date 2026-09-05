using System;
using Xunit;

namespace Wslhub.Sdk.UnitTests
{
    public sealed class WindowsFactAttribute : FactAttribute
    {
        public WindowsFactAttribute()
        {
            if (!OperatingSystem.IsWindows())
                Skip = "Requires Windows registry or native pipe APIs; no WSL installation is required.";
        }
    }
}
