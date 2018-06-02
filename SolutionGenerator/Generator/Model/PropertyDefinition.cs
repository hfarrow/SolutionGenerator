using System;
using System.Collections.Generic;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Model
{
    public abstract class PropertyDefinition
    {
        public string Name { get; }
        public Type ValueType { get; }
        public ElementReader<PropertyElement, PropertyDefinition> Reader;
        
        protected object DefaultValueObj { get; }

        protected PropertyDefinition(string name, Type valueType, object defaultValueObj,
            ElementReader<PropertyElement, PropertyDefinition> reader)
        {
            Name = name;
            ValueType = valueType;
            DefaultValueObj = defaultValueObj;
            Reader = reader;
        }

        public abstract object GetOrCloneDefaultValue();
    }

    
    public class PropertyDefinition<TValue, TReader> : PropertyDefinition
        where TReader : ElementReader<PropertyElement, PropertyDefinition>, new()
    {
        public TValue DefaultValue { get; }

        public PropertyDefinition(string name, TValue defaultValue)
            : base(name, typeof(TValue), defaultValue, new TReader())
        {
            DefaultValue = defaultValue;
        }

        public PropertyDefinition(string name)
            : this(name, Activator.CreateInstance<TValue>())
        {
            
        }

        public override object GetOrCloneDefaultValue()
        {
            // It should be safe to assume all single values are basic types and will only be replaced and not
            // modified in place.
            return DefaultValueObj;
        }
    }
}