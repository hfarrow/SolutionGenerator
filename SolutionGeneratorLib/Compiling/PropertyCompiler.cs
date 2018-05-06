using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling
{
    public abstract class PropertyCompiler : ElementCompiler<PropertyElement, PropertyDefinition>
    {
        protected override bool UseEvaluatedConditionalToSkip => true;

        protected override Result CompileElement(Settings settings, PropertyElement element,
            PropertyDefinition definition)
        {
            if (!settings.HasProperty(definition.Name))
            {
                settings.SetProperty(definition.Name, definition.DefaultValueObj);
            }

            return CompileProperty(settings, element, definition);
        }

        protected abstract Result CompileProperty(Settings settings, PropertyElement element,
            PropertyDefinition definition);
    }
}