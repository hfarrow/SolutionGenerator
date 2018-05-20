using System.Collections.Generic;
using SolutionGen.Generator.ModelOld;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator
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
                case ValueElement noneValue when noneValue.Value == null:
                    // Do nothing. Values already cleared by "set" command and adding "none" is like a no-op
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