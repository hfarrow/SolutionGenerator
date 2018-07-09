﻿using System;
using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class Solution
    {
        public string Name { get; }
        public readonly Guid Guid;
        public string SolutionConfigPath { get; }
        public readonly Settings Settings;

        public IReadOnlyCollection<string> TargetPlatforms =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_TARGET_PLATFORMS);

        public string RootNamespace => Settings.GetProperty<string>(Settings.PROP_ROOT_NAMESPACE);

        public Solution(string name, Settings settings, string solutionConfigPath)
        {
            Name = name;
            Guid = Guid.NewGuid();
            Settings = settings;
            SolutionConfigPath = solutionConfigPath;
        }
    }
}