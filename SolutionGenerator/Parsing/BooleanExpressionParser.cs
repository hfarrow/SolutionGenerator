using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sprache;

namespace SolutionGen.Parsing
{
    public static class BooleanExpressionParser
    {
        private static HashSet<string> conditionalConstants = new HashSet<string>
        {
            "true"
        };

        public static void SetConditionalConstants(HashSet<string> constants)
        {
            conditionalConstants = new HashSet<string>(constants);

            // Never allow "false" to be evaluated to true.
            conditionalConstants.Remove("false");
            conditionalConstants.Add("true");
        }
        
        public static Expression<Func<bool>> ParseExpression(string text)
        {
            return lambda.Parse(text);
        }

        public static bool TryParseExpression(string text, out IResult<Expression<Func<bool>>> result)
        {
            result = lambda.TryParse(text);
            return result.WasSuccessful;
        }

        public static bool InvokeExpression(string text)
        {
            return ParseExpression(text).Compile().Invoke();
        }

        private static Parser<ExpressionType> Operator(string op, ExpressionType opType) =>
            Parse.String(op).Token().Return(opType);

        private static readonly Parser<ExpressionType> and = Operator("&&", ExpressionType.AndAlso);
        private static readonly Parser<ExpressionType> or = Operator("||", ExpressionType.OrElse);

        private static readonly Parser<Expression> boolean =
            BasicParser.IdentifierToken
                .Select(id => System.Linq.Expressions.Expression.Constant(conditionalConstants.Contains(id)))
                .Named("boolean");

        private static readonly Parser<Expression> factor =
            (from lparen in Parse.Char('(')
                from expr in Parse.Ref(() => Expression)
                from rparen in Parse.Char(')')
                select expr).Named("expression")
            .XOr(boolean);

        private static readonly Parser<Expression> operand =
            ((from sign in Parse.Char('!').Token()
                    from factor in factor
                    select System.Linq.Expressions.Expression.Not(factor))
                .XOr(factor)).Token();

        // The higher the expr number, the higher the precendence. In this case, AND takes precedence over OR
        private static readonly Parser<Expression> expression2 =
            Parse.ChainOperator(and, operand, System.Linq.Expressions.Expression.MakeBinary);
        
        public static readonly Parser<Expression> Expression =
            Parse.ChainOperator(or, expression2, System.Linq.Expressions.Expression.MakeBinary);

        private static readonly Parser<Expression<Func<bool>>> lambda =
            Expression.End().Select(body => System.Linq.Expressions.Expression.Lambda<Func<bool>>(body));
    }
}