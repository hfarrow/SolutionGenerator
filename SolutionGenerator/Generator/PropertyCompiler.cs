using SolutionGen.Generator.ModelOld;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator
{
    public abstract class PropertyCompiler : ElementCompiler<PropertyElement, PropertyDefinition>
    {
        protected override bool UseEvaluatedConditionalToSkip => true;

        protected override Result CompileElement(Settings settings, PropertyElement element,
            PropertyDefinition definition)
        {
            if (!settings.HasProperty(definition.Name))
            {
                // TODO: apply all defaults without requiring compile. The defaults might not be applied
                // if 'skip' command is used.
                settings.SetProperty(definition.Name, definition.DefaultValueObj);
            }

            return CompileProperty(settings, element, definition);
        }

        protected abstract Result CompileProperty(Settings settings, PropertyElement element,
            PropertyDefinition definition);
    }
}