using System;
using System.Collections.Generic;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;

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
    }

    public class PropertyDictionaryDefinition<TDictionary, TValue, TReader> : PropertyDictionaryDefinition
        where TReader : ElementReader<PropertyElement, PropertyDefinition>, new()
        where TDictionary : IDictionary<string, TValue>, new()
    {
        private TDictionary DefaultValue { get; }
        
        public PropertyDictionaryDefinition(string name, TDictionary defaultValue)
            : base(name, typeof(TValue), defaultValue, new TReader())
        {
            DefaultValue = defaultValue;
        }

        public PropertyDictionaryDefinition(string name)
            : this(name, new TDictionary())
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

            var castedDictionary = (TDictionary) dictionary;
            castedDictionary.Add(key, (TValue) value);
        }

        public override void ClearDictionary(object dictionary)
        {
            CheckDictionaryType(dictionary);
            ((TDictionary)dictionary).Clear();
        }
        
        public override object GetOrCloneDefaultValue()
        {
            var copy = new TDictionary();
            foreach (KeyValuePair<string, TValue> kvp in DefaultValue)
            {
                copy[kvp.Key] = kvp.Value;
            }

            return copy;
        }
        
        private static void CheckDictionaryType(object dictionary)
        {
            if (!(dictionary is TDictionary))
            {
                throw new ArgumentException($"Provided dictionary must be of type '{typeof(TDictionary).FullName}'." );
            }
        }

    }
}