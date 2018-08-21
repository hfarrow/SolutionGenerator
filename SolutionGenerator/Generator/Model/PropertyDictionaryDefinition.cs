using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public abstract class PropertyDictionaryDefinition : PropertyDefinition
    {
        protected PropertyDictionaryDefinition(string name, Type valueType, object defaultValueObj,
            ElementReader<PropertyElement, PropertyDefinition> reader)
            : base(name, valueType, defaultValueObj, reader)
        {
        }

        public abstract void AddToDictionary(object dictionary, string key, object value);
        public abstract void ClearDictionary(object dictionary);
        public abstract object CloneDictionary(object dictionary);
        
        public abstract bool ExpandVariablesInDictionary(object dictionary, string varName, string varExpansion,
            out object newDictionary);

        public abstract bool StripEscapedVariablesInDictionary(object dictionary, out object newDictionary);
    }

    public class PropertyDictionaryDefinition<TValue, TReader> : PropertyDictionaryDefinition
        where TReader : ElementReader<PropertyElement, PropertyDefinition>, new()
    {
        private Dictionary<string, TValue> DefaultValue { get; }
        
        public PropertyDictionaryDefinition(string name, Dictionary<string, TValue> defaultValue)
            : base(name, typeof(TValue), defaultValue, new TReader())
        {
            DefaultValue = defaultValue;
        }

        public PropertyDictionaryDefinition(string name)
            : this(name, new Dictionary<string, TValue>())
        {
        }

        public override void AddToDictionary(object dictionary, string key, object value)
        {
            CheckDictionaryType(dictionary);
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Provided value cannot be null.");
            }
            
            if (!(value is TValue))
            {
                throw new ArgumentException($"Provided value must be of type '{typeof(TValue).FullName}'.");
            }

            var castedDictionary = (Dictionary<string, TValue>) dictionary;
            castedDictionary.Add(key, (TValue) value);
        }

        public override void ClearDictionary(object dictionary)
        {
            CheckDictionaryType(dictionary);
            ((Dictionary<string, TValue>)dictionary).Clear();
        }

        public override object CloneDictionary(object dictionary)
        {
            CheckDictionaryType(dictionary);
            var castedDictionary = (Dictionary<string, TValue>) dictionary;
            var copy = new Dictionary<string, TValue>();
            foreach (KeyValuePair<string, TValue> kvp in castedDictionary)
            {
                copy[kvp.Key] = kvp.Value;
            }

            return copy;
        }
        
        public override bool ExpandVariablesInDictionary(object dictionary, string varName, string varExpansion,
            out object newDictionary)
        {
            CheckDictionaryType(dictionary);
            var castedDictionary = (Dictionary<string, TValue>) dictionary;

            bool didExpand = false;
            var modifiedValues = new Dictionary<string, TValue>();
            foreach (KeyValuePair<string, TValue> kvp in castedDictionary)
            {
                if (ExpandableVar.ExpandInCopy(kvp.Key, varName, varExpansion, out object copy))
                {
                    didExpand = true;
                    modifiedValues[kvp.Key] = (TValue) copy;
                }
            }

            foreach (KeyValuePair<string,TValue> kvp in modifiedValues)
            {
                castedDictionary[kvp.Key] = kvp.Value;
            }

            // dictionary expands in place (no new instance)
            newDictionary = dictionary;
            return didExpand;
        }

        public override bool StripEscapedVariablesInDictionary(object dictionary, out object newDictionary)
        {
            CheckDictionaryType(dictionary);
            var castedDictionary = (Dictionary<string, TValue>) dictionary;

            bool didStrip = false;
            var modifiedValues = new Dictionary<string, TValue>();
            foreach (KeyValuePair<string, TValue> kvp in castedDictionary)
            {
                if (ExpandableVar.StripEscapedVariablesInCopy(kvp.Key, out object copy))
                {
                    didStrip = true;
                    modifiedValues[kvp.Key] = (TValue) copy;
                }
            }

            foreach (KeyValuePair<string,TValue> kvp in modifiedValues)
            {
                castedDictionary[kvp.Key] = kvp.Value;
            }

            // dictionary strips in place (no new instance)
            newDictionary = dictionary;
            return didStrip;
        }

        public override object GetOrCloneDefaultValue()
        {
            var copy = new Dictionary<string, TValue>();
            foreach (KeyValuePair<string, TValue> kvp in DefaultValue)
            {
                copy[kvp.Key] = kvp.Value;
            }

            return copy;
        }

        public override object CloneValue(object value)
        {
            return CloneDictionary(value);
        }

        public override string PrintValue(object value)
        {
            CheckDictionaryType(value);
            var castedCollection = (Dictionary<string, TValue>) value;
            return string.Join(", ", castedCollection.Select(kvp => $"{kvp.Key}=>{kvp.Value}"));
        }

        public override bool ExpandVariable(object value, string varName, string varExpansion, out object newValue)
        {
            return ExpandVariablesInDictionary(value, varName, varExpansion, out newValue);
        }
        
        public override bool StripEscapedVariables(object value, out object newValue)
        {
            return StripEscapedVariablesInDictionary(value, out newValue);
        }

        private static void CheckDictionaryType(object dictionary)
        {
            if (!(dictionary is Dictionary<string, TValue>))
            {
                throw new ArgumentException($"Provided dictionary must be of type '{typeof(Dictionary<string, TValue>).FullName}'." );
            }
        }

    }
}