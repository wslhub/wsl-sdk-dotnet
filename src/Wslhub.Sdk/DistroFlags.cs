using System;

namespace Wslhub.Sdk
{
    [Flags, Serializable]
    public enum DistroFlags
    {
        None = 0x0,
        EnableInterop = 0x1,
        AppendNtPath = 0x2,
        EnableDriveMouting = 0x4,
    }
}
