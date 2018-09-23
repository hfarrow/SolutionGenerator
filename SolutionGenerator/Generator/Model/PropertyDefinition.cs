using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public static string LogValue(object value)
        {
            // Best effort to pretty print most types of collections
            switch (value)
            {
                case string str:
                    return str;
                case IDictionary dictionary when dictionary.Count == 0:
                    return "<none>";
                case IDictionary dictionary:
                    return Log.GetIndentedCollection(dictionary,
                        obj =>
                        {
                            switch (obj)
                            {
                                case DictionaryEntry entry:
                                    return $"{entry.Key} => {LogValue(entry.Value)}";
                                case KeyValuePair<string, object> kvp:
                                    return $"{kvp.Key} => {LogValue(kvp.Value)}";
                            }

                            return obj.ToString();
                        });
                case ICollection collection when collection.Count == 0:
                    return "<none>";
                case ICollection collection:
                    return Log.GetIndentedCollection(collection, LogValue);
                case IEnumerable enumerable:
                    List<object> list = enumerable.OfType<object>().ToList();
                    if (list.Count == 0)
                    {
                        return "<none>";
                    }
                    return Log.GetIndentedCollection(list, LogValue);
                default:
                    return value.ToString();
            }
        }
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
            if (!ExpandableVars.ExpandInCopy(value, varName, varExpansion, out object copy))
            {
                newValue = value;
                return false;
            }

            newValue = copy;
            return true;
        }

        public override bool StripEscapedVariables(object value, out object newValue)
        {
            if (!ExpandableVars.StripEscapedVariablesInCopy(value, out object copy))
            {
                newValue = value;
                return false;
            }

            newValue = copy;
            return true;
        }
    }
}