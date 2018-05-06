using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;

namespace SolutionGenerator.Compiling
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
        
        public static IEnumerable<string> ExpandGlob(string globStr)
        {
            // TODO: use Microsoft.Extensions.FileSystemGlobbing
            yield break;
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