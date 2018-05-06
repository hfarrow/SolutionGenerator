using System.Collections.Generic;
using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling
{
    public class HashSetPropertyCompiler : ElementCompiler<PropertyElement, PropertyDefinition>
    {
        protected override bool UseEvaluatedConditionalToSkip => true;

        protected override Result GenerateCompiledAction(Settings settings,
            PropertyElement element, PropertyDefinition definition)
        {
            var values = settings.GetProperty<HashSet<string>>(element.FullName);
            if (element.Action == PropertyAction.Set)
            {
                values.Clear();
            }

            if (element.ValueElement is GlobValue globValue)
            {
                foreach (string path in ExpandGlob(globValue.GlobStr))
                {
                    values.Add(path);
                }
            }
            else
            {
                values.Add(element.ValueElement.Value.ToString());
            }

            return Result.Continue;
        }
    }
}