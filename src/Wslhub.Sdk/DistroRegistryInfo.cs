using System;
using System.Collections.Generic;

namespace Wslhub.Sdk
{
    public class DistroRegistryInfo
    {
        public Guid DistroId { get; internal set; }
        public string DistroName { get; internal set; }
        public List<string> KernelCommandLine { get; internal set; } = new List<string>();
        public string BasePath { get; internal set; }
        public bool IsDefault { get; internal set; }

        public override string ToString() => $"{DistroName} [{DistroId}]";
    }
}
