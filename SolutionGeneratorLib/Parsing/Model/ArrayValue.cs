﻿using System.Collections.Generic;

namespace SolutionGenerator.Parsing.Model
{
    public class ArrayValue : ValueElement
    {
        public IEnumerable<ValueElement> Values { get; }
        public ArrayValue(IEnumerable<ValueElement> values) : base(values)
        {
            Values = values;
        }
    }
}