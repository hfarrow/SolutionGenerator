using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class DictionaryPropertyReader
        : ElementReader<PropertyElement, PropertyDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(PropertyElement element, PropertyDefinition definition)
        {
            if (!(element.ValueElement.Value is ObjectElement objElement))
            {
                throw new InvalidOperationException(
                    "Property value element must be of type ObjectElement in order to read it as a dictionary. " +
                    "Definition = " + definition.Name);
            }
            
            var dictionary = new Dictionary<string, object>();

            foreach (PropertyElement innerPropertyElement in objElement.Elements.OfType<PropertyElement>())
            {
                string key = innerPropertyElement.FullName;
                object value;
                if (innerPropertyElement.ValueElement.Value is ObjectElement)
                {
                    IResult<IEnumerable<object>> result = Read(innerPropertyElement, definition);
                    value = result.Value.First();
                }
                else
                {
                    value = innerPropertyElement.ValueElement.Value;
                }

                dictionary[key] = value;
            }
            
            return new Result<IEnumerable<Dictionary<string, object>>>(false, new []{dictionary});
        }
    }
}