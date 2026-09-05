namespace Wslhub.Sdk
{
    /// <summary>Describes whether the current process can find the WSL API and command-line components.</summary>
    /// <remarks>This check does not start WSL or verify optional features, virtualization, services, kernels, or distributions.</remarks>
    public enum WslSupportStatus
    {
        /// <summary>The operating system does not meet this SDK's Windows version requirement.</summary>
        UnsupportedOperatingSystem = 0,

        /// <summary>The operating system or the current process is not 64-bit.</summary>
        UnsupportedArchitecture = 1,

        /// <summary>The Windows version could not be determined.</summary>
        OperatingSystemVersionUnavailable = 2,

        /// <summary>The system directory does not contain wslapi.dll.</summary>
        WslApiNotFound = 3,

        /// <summary>The system directory does not contain wsl.exe.</summary>
        WslExecutableNotFound = 4,

        /// <summary>The platform requirements are met and both WSL components are present.</summary>
        Available = 5,
    }
}
