using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sprache;

namespace SolutionGen.Parser
{
    public class BooleanExpressionParser
    {
        private HashSet<string> conditionalConstants = new HashSet<string>
        {
            "true"
        };       

        public void SetConditionalConstants(IEnumerable<string> constants)
        {
            conditionalConstants = new HashSet<string>(constants);

            // Never allow "false" to be evaluated to true.
            conditionalConstants.Remove("false");
            conditionalConstants.Add("true");
        }
        
        public Expression<Func<bool>> ParseExpression(string text)
        {
            return lambda.Parse(text);
        }

        public bool TryParseExpression(string text, out IResult<Expression<Func<bool>>> result)
        {
            result = lambda.TryParse(text);
            return result.WasSuccessful;
        }

        public bool InvokeExpression(string text)
        {
            return ParseExpression(text).Compile().Invoke();
        }

        private static Parser<ExpressionType> Operator(string op, ExpressionType opType) =>
            Parse.String(op).Token().Return(opType);

        private static readonly Parser<ExpressionType> and = Operator("&&", ExpressionType.AndAlso);
        private static readonly Parser<ExpressionType> or = Operator("||", ExpressionType.OrElse);

        // The higher the expr number, the higher the precendence. In this case, AND takes precedence over OR
        private readonly Parser<Expression> expression;
        private readonly Parser<Expression<Func<bool>>> lambda;

        public BooleanExpressionParser()
        {
            Parser<Expression> boolean = BasicParser.IdentifierToken
                .Select(id => Expression.Constant(conditionalConstants.Contains(id)))
                .Named("boolean");

            Parser<Expression> factor = (from lparen in Parse.Char('(')
                    from expr in Parse.Ref(() => expression)
                    from rparen in Parse.Char(')')
                    select expr).Named("expression")
                .XOr(boolean);

            Parser<Expression> operand = ((from sign in Parse.Char('!').Token()
                    from f in factor
                    select Expression.Not(f))
                .XOr(factor)).Token();

            // The higher the expr number, the higher the precendence. In this case, AND takes precedence over OR
            Parser<Expression> expression2 = Parse.ChainOperator(and, operand, Expression.MakeBinary);

            expression =
                Parse.ChainOperator(or, expression2, Expression.MakeBinary);

            lambda =
                expression.End().Select(body => Expression.Lambda<Func<bool>>(body));
        }
    }
}