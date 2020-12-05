using System.Collections.Generic;

namespace Wslhub.Sdk
{
    public sealed class DistroInfo : DistroRegistryInfo
    {
        public List<string> DefaultEnvironmentVariables { get; internal set; } = new List<string>();
        public int DefaultUid { get; internal set; }
        public DistroFlags DistroFlags { get; internal set; }
        public bool IsRegistered { get; internal set; }
        public bool IsDefaultDistro { get; internal set; }
        public int WslVersion { get; internal set; }

        public bool EnableInterop => DistroFlags.HasFlag(DistroFlags.EnableInterop);
        public bool EnableDriveMounting => DistroFlags.HasFlag(DistroFlags.EnableDriveMouting);
        public bool AppendNtPath => DistroFlags.HasFlag(DistroFlags.AppendNtPath);
    }
}
