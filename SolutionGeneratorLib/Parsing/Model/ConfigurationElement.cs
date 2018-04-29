﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SolutionGenerator.Parsing.Model
{
    public class ConfigurationElement : CommandElement
    {
        public string ConfigurationName { get; }
        public IReadOnlyDictionary<string, HashSet<string>> Configurations { get; }

        public ConfigurationElement(string configurationName, IEnumerable<KeyValuePair> values)
            : base("configuration", "true")
        {
            ConfigurationName = configurationName;

            Configurations = values.ToDictionary(
                kvp => kvp.PairKey,
                kvp => new HashSet<string>(kvp.PairValue.Value.ToString().Split(',')));
        }
    }
}