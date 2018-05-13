using System.Collections.Generic;
using SolutionGen.Compiling.Model;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling
{
    public class HashSetPropertyCompiler : PropertyCompiler
    {
        protected override bool UseEvaluatedConditionalToSkip => true;

        protected override Result CompileProperty(Settings settings,
            PropertyElement element, PropertyDefinition definition)
        {
            var values = new HashSet<object>(settings.GetProperty<HashSet<object>>(element.FullName));
            if (element.Action == PropertyAction.Set)
            {
                values.Clear();
            }

            switch (element.ValueElement)
            {
                case GlobValue _:
                    values.Add(element.ValueElement);
                    break;
                case ArrayValue arrayValue:
                    foreach (ValueElement arrayElement in arrayValue.Values)
                    {
                        values.Add(arrayElement.Value.ToString());
                    }
                    break;
                default:
                    values.Add(element.ValueElement.Value.ToString());
                    break;
            }
            
            settings.SetProperty(element.FullName, values);

            return Result.Continue;
        }
    }
}