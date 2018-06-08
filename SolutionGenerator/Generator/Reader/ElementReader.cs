using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;

namespace SolutionGen.Generator.Reader
{
    public abstract class ElementReader
    {
        public interface IResult<out T>
        {
            bool Terminate { get; }
            T Value { get; }
            bool HasValue { get; }
        }
        
        public struct Result<T> : IResult<T>
        {
            public bool Terminate { get; }
            public T Value { get; }
            public bool HasValue { get; }
            
            public Result(bool terminate, T value)
            {
                Terminate = terminate;
                Value = value;
                HasValue = true;
            }
            
            public Result(bool terminate)
            {
                Terminate = terminate;
                Value = default(T);
                HasValue = false;
            }
        }

        public struct Result : IResult<IEnumerable<object>>
        {
            public Result(bool terminate)
            {
                Terminate = terminate;
            }

            public bool Terminate { get; }
            public IEnumerable<object> Value => null;
            public bool HasValue => false;
        }
        
        protected static bool EvaluateConditional(string conditionalExpr, BooleanExpressionParser parser)
        {
            if (!parser.TryParseExpression(conditionalExpr,
                out Sprache.IResult<Expression<Func<bool>>> result))
            {
                throw new BoolExpressionEvaluationException(conditionalExpr, result.ToString());
            }

            return result.Value.Compile().Invoke();
        }
    }
    
    public abstract class ElementReader<TElement, TDefinition> : ElementReader
        where TElement : ConfigElement
    {
        protected bool ConditionalResult { get; private set; }
        
        protected abstract IResult<IEnumerable<object>> Read(TElement element, TDefinition definition);

        public IResult<IEnumerable<object>> EvaluateAndRead(TElement element, TDefinition definition,
            BooleanExpressionParser parser)
        {
            ConditionalResult = EvaluateConditional(element.ConditionalExpression, parser);
            if (!ConditionalResult)
            {
                return new Result<IEnumerable<object>>(false);
            }
            
            return Read(element, definition);
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