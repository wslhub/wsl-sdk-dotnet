﻿using System;
using System.Collections.Generic;

namespace Wslhub.Sdk
{
    public class DistroRegistryInfo
    {
        public Guid DistroId { get; set; }
        public string DistroName { get; set; }
        public List<string> KernelCommandLine { get; set; } = new List<string>();
        public string BasePath { get; set; }
        public bool IsDefault { get; set; }
    }
}
