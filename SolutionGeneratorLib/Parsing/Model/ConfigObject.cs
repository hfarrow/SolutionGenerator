﻿using System.Collections.Generic;

namespace SolutionGenerator.Parsing.Model
{
    public class ConfigObject : ObjectElement
    {
        public ConfigObjectHeading Heading { get; }
        
        // TODO: base ObjectElement type
        public IEnumerable<ObjectElement> Elements { get; }
        
        public ConfigObject(ConfigObjectHeading heading, IEnumerable<ObjectElement> elements) : base("true")
        {
            Heading = heading;
            Elements = elements;
        }
    }
}