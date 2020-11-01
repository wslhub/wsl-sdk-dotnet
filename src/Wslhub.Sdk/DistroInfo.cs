using System.Collections.Generic;

namespace Wslhub.Sdk
{
    public sealed class DistroInfo : DistroRegistryInfo
    {
        public bool Succeed => HResult == 0;
        public List<string> DefaultEnvironmentVariables { get; set; } = new List<string>();
        public int DefaultUid { get; set; }
        public bool EnableInterop => DistroFlags.HasFlag(DistroFlags.EnableInterop);
        public bool EnableDriveMounting => DistroFlags.HasFlag(DistroFlags.EnableDriveMouting);
        public bool AppendNtPath => DistroFlags.HasFlag(DistroFlags.AppendNtPath);
        public DistroFlags DistroFlags { get; set; }
        public bool IsRegistered { get; set; }
        public bool IsDefaultDistro { get; set; }
        public int HResult { get; set; }
        public int WslVersion { get; set; }
    }
}
