using System;
using System.Collections.Generic;

namespace Wslhub.Sdk
{
    /// <summary>
    /// A model class that represents information about the WSL distribution registered in the registry.
    /// </summary>
    public class DistroRegistryInfo
    {
        /// <summary>
        /// Unique ID identifying the WSL distribution
        /// </summary>
        public Guid DistroId { get; internal set; }

        /// <summary>
        /// Name of the WSL distribution
        /// </summary>
        public string DistroName { get; internal set; }

        /// <summary>
        /// List of kernel parameters to be passed on cold boot
        /// </summary>
        public List<string> KernelCommandLine { get; internal set; } = new List<string>();

        /// <summary>
        /// The path to the local directory where the WSL distribution is installed.
        /// </summary>
        public string BasePath { get; internal set; }

        /// <summary>
        /// Whether or not registered as the default WSL distribution
        /// </summary>
        public bool IsDefault { get; internal set; }

        /// <summary>
        /// Returns a description of this model object.
        /// </summary>
        /// <returns>Returns a description of this model object.</returns>
        public override string ToString() => $"{DistroName} [{DistroId}]";
    }
}
