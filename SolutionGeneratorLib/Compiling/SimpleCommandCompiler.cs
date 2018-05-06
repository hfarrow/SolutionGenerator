using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling
{
    public class SimpleCommandCompiler : ElementCompiler<CommandElement, CommandDefinition>
    {
        protected override bool UseEvaluatedConditionalToSkip => false;
        
        protected override Result GenerateCompiledAction(Settings settings,
            CommandElement element, CommandDefinition definition)
        {
            return definition.CommandAction(settings);
        }
    }
}