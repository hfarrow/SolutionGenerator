using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public abstract class PropertyCollectionDefinition : PropertyDefinition
    {
        protected PropertyCollectionDefinition(string name, Type valueType, object defaultValueObj,
            ElementReader<PropertyElement, PropertyDefinition> reader)
            : base(name, valueType, defaultValueObj, reader)
        {
        }

        public abstract void AddToCollection(object collection, object value);
        public abstract void ClearCollection(object collection);
        public abstract object CloneCollection(object collection);

        public abstract bool ExpandVariablesInCollection(object collection, string varName, string varExpansion,
            out object newCollection);
    }
    
    public class PropertyCollectionDefinition<TCollection, TValue, TReader> : PropertyCollectionDefinition
        where TReader : ElementReader<PropertyElement, PropertyDefinition>, new()
        where TCollection : ICollection<TValue>, new()
    {
        private TCollection DefaultValue { get; }
        
        public PropertyCollectionDefinition(string name, TCollection defaultValue)
            : base(name, typeof(TCollection), defaultValue, new TReader())
        {
            DefaultValue = defaultValue;
        }

        public PropertyCollectionDefinition(string name)
            : this(name, new TCollection())
        {
        }

        public override void AddToCollection(object collection, object value)
        {
            CheckCollectionType(collection);
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Provided value cannot be null.");
            }

            if (!(value is TValue))
            {
                throw new ArgumentException($"Provided value must be of type '{typeof(TValue).FullName}'.");
            }

            var castedCollection = (TCollection) collection;
            castedCollection.Add((TValue) value);
        }
        
        public override void ClearCollection(object collection)
        {
            CheckCollectionType(collection);
            ((TCollection)collection).Clear();
        }

        public override object CloneCollection(object collection)
        {
            CheckCollectionType(collection);
            var castedCollection = (TCollection) collection;
            var copy = new TCollection();
            foreach (TValue value in castedCollection)
            {
                copy.Add(value);
            }

            return copy;
        }

        public override bool ExpandVariablesInCollection(object collection, string varName, string varExpansion,
            out object newCollection)
        {
            CheckCollectionType(collection);
            var castedCollection = (TCollection) collection;
            
            bool didExpand = false;
            var collectionCopy = new TCollection();
            foreach (TValue value in castedCollection)
            {
                if (ExpandableVar.ExpandInCopy(value, varName, varExpansion, out object copy))
                {
                    didExpand = true;
                }
                collectionCopy.Add((TValue) copy);
            }

            newCollection = collectionCopy;
            return didExpand;
        }

        public override object GetOrCloneDefaultValue()
        {
            var copy = new TCollection();
            foreach (TValue value in DefaultValue)
            {
                copy.Add(value);
            }

            return copy;
        }

        public override object CloneValue(object value)
        {
            return CloneCollection(value);
        }

        public override bool ExpandVariable(object value, string varName, string varExpansion, out object newValue)
        {
            return ExpandVariablesInCollection(value, varName, varExpansion, out newValue);
        }

        public override string PrintValue(object value)
        {
            CheckCollectionType(value);
            var castedCollection = (TCollection) value;
            return "[" + string.Join(", ", castedCollection.Select(obj => $"<{obj.ToString()}>")) + "]";
        }

        private static void CheckCollectionType(object collection)
        {
            if (!(collection is TCollection))
            {
                throw new ArgumentException($"Provided collection must be of type '{typeof(TCollection).FullName}'." );
            }
        }
    }
}