using SolutionGen.Compiling.Model;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling
{
    public class SimpleCommandCompiler : ElementCompiler<CommandElement, CommandDefinition>
    {
        protected override bool UseEvaluatedConditionalToSkip => false;
        
        protected override Result CompileElement(Settings settings,
            CommandElement element, CommandDefinition definition)
        {
            return ConditionalEvaluation 
                ? definition.CommandAction(settings) 
                : Result.Continue;
        }
    }
}