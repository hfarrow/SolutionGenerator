using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SolutionGen.Parser;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseBooleanExpressionTests
    {
        private BooleanExpressionParser parser;
        
        public ParseBooleanExpressionTests()
        {
            parser = new BooleanExpressionParser();
            parser.SetConditionalConstants(new HashSet<string> {"true"});
        }

        [Fact]
        public void SingleDefinedConditionWithParenIsTrue()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("(true)");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void SingleDefinedConditionWithoutParenIsTrue()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void SingleUndefinedConditionIsFalse()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("false");
            Assert.False(exp.Compile().Invoke());
        }

        [Fact]
        public void SingleDefinedNotConditionIsFalse()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("!true");
            Assert.False(exp.Compile().Invoke());
        }
        
        [Fact]
        public void SingleUndefinedNotConditionIsTrue()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("!false");
            Assert.True(exp.Compile().Invoke());
        }

        [Fact]
        public void NotEntireTrueConditionIsFalse()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("!(true)");
            Assert.False(exp.Compile().Invoke());
        }

        [Fact]
        public void TrueOrTrueCondtionIsTrue()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("true||true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void TrueOrFalseCondtionIsTrue()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("true||false");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void FalseOrTrueCondtionIsTrue()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("false||true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void TrueAndTrueCondtionIsTrue()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("true&&true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void TrueAndFalseCondtionIsFalse()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("true&&false");
            Assert.False(exp.Compile().Invoke());
        }
        
        [Fact]
        public void FalseAndTrueCondtionIsFalse()
        {
            Expression<Func<bool>> exp = parser.ParseExpression("false&&true");
            Assert.False(exp.Compile().Invoke());
        }

        [Fact]
        public void AndOperatorTakesHigherPrecedence()
        {
            Assert.True(parser.ParseExpression("true||true&&false").Compile().Invoke());
            Assert.False(parser.ParseExpression("(true||true)&&false").Compile().Invoke());
            Assert.True(parser.ParseExpression("true||(true&&false)").Compile().Invoke());
        }

        [Fact]
        public void ExpressionCanHaveWhiteSpace()
        {
            Expression<Func<bool>> exp = parser.ParseExpression(" ! ( true && true || false ) ");
            Assert.False(exp.Compile().Invoke());
        }
    }
}