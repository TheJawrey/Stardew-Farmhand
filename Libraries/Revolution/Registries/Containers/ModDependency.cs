﻿using Version = System.Version;

namespace Revolution.Registries.Containers
{
    public class ModDependency
    {
        public string UniqueId { get; set; }
        public Version MinimumVersion { get; set; }
        public Version MaximumVersion { get; set; }
        public DependencyState DependencyState { get; set; }
    }
}
