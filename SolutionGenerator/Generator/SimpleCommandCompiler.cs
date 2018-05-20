using SolutionGen.Generator.ModelOld;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator
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