using System;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public abstract class PropertyDefinition
    {
        public string Name { get; }
        public Type ValueType { get; }
        public readonly ElementReader<PropertyElement, PropertyDefinition> Reader;
        
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
        public abstract object CloneValue(object value);
        public abstract bool ExpandVariable(object value, string varName, string varExpansion, out object newValue);
        public abstract bool StripEscapedVariables(object value, out object newValue);

        public abstract string PrintValue(object value);
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

        public override object CloneValue(object value)
        {
            // Properties are assumed to be strings, ints, bools, etc. and do not need a deep copy.
            return value;
        }

        public override bool ExpandVariable(object value, string varName, string varExpansion, out object newValue)
        {
            if (!ExpandableVar.ExpandInCopy(value, varName, varExpansion, out object copy))
            {
                newValue = value;
                return false;
            }

            newValue = copy;
            return true;
        }

        public override bool StripEscapedVariables(object value, out object newValue)
        {
            if (!ExpandableVar.StripEscapedVariablesInCopy(value, out object copy))
            {
                newValue = value;
                return false;
            }

            newValue = copy;
            return true;
        }

        public override string PrintValue(object value)
        {
            return value.ToString();
        }
    }
}