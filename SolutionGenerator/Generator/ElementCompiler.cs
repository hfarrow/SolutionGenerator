using System;
using System.Linq.Expressions;
using SolutionGen.Generator.ModelOld;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;

namespace SolutionGen.Generator
{
    public abstract class ElementCompiler<TElement, TDefinition> : ElementCompiler
        where TElement : ConfigElement
    {
        public Result Compile(Settings settings, TElement element, TDefinition definition)
        {
            ConditionalEvaluation = EvaluateConditional(element.ConditionalExpression);
            if (!ConditionalEvaluation && UseEvaluatedConditionalToSkip)
            {
                return Result.Continue;
            }

            return CompileElement(settings, element, definition);
        }

        protected bool ConditionalEvaluation { get; private set; }
        protected abstract bool UseEvaluatedConditionalToSkip { get; }

        protected abstract Result CompileElement(Settings settings,
            TElement element, TDefinition definition);
    }

    public class ElementCompiler
    {
        public enum Result
        {
            Terminate,
            Continue
        }
        
        public static bool EvaluateConditional(string conditionalExpr)
        {
            if (!BooleanExpressionParser.TryParseExpression(conditionalExpr,
                out IResult<Expression<Func<bool>>> result))
            {
                throw new BoolExpressionEvaluationException(conditionalExpr, result.ToString());
            }

            return result.Value.Compile().Invoke();
        }
    }
    
    public class BoolExpressionEvaluationException : Exception
    {
        public BoolExpressionEvaluationException(string expr, string message)
            : base(string.Format("Failed to evaluate conditional expression: {0} => {1}", expr, message))
        {
            
        }
    }
}