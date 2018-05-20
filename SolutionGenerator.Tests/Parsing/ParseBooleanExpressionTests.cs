using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SolutionGen.Parser;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseBooleanExpressionTests
    {
        public ParseBooleanExpressionTests()
        {
            BooleanExpressionParser.SetConditionalConstants(new HashSet<string> {"true"});
        }

        [Fact]
        public void SingleDefinedConditionWithParenIsTrue()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("(true)");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void SingleDefinedConditionWithoutParenIsTrue()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void SingleUndefinedConditionIsFalse()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("false");
            Assert.False(exp.Compile().Invoke());
        }

        [Fact]
        public void SingleDefinedNotConditionIsFalse()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("!true");
            Assert.False(exp.Compile().Invoke());
        }
        
        [Fact]
        public void SingleUndefinedNotConditionIsTrue()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("!false");
            Assert.True(exp.Compile().Invoke());
        }

        [Fact]
        public void NotEntireTrueConditionIsFalse()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("!(true)");
            Assert.False(exp.Compile().Invoke());
        }

        [Fact]
        public void TrueOrTrueCondtionIsTrue()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("true||true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void TrueOrFalseCondtionIsTrue()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("true||false");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void FalseOrTrueCondtionIsTrue()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("false||true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void TrueAndTrueCondtionIsTrue()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("true&&true");
            Assert.True(exp.Compile().Invoke());
        }
        
        [Fact]
        public void TrueAndFalseCondtionIsFalse()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("true&&false");
            Assert.False(exp.Compile().Invoke());
        }
        
        [Fact]
        public void FalseAndTrueCondtionIsFalse()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression("false&&true");
            Assert.False(exp.Compile().Invoke());
        }

        [Fact]
        public void AndOperatorTakesHigherPrecedence()
        {
            Assert.True(BooleanExpressionParser.ParseExpression("true||true&&false").Compile().Invoke());
            Assert.False(BooleanExpressionParser.ParseExpression("(true||true)&&false").Compile().Invoke());
            Assert.True(BooleanExpressionParser.ParseExpression("true||(true&&false)").Compile().Invoke());
        }

        [Fact]
        public void ExpressionCanHaveWhiteSpace()
        {
            Expression<Func<bool>> exp = BooleanExpressionParser.ParseExpression(" ! ( true && true || false ) ");
            Assert.False(exp.Compile().Invoke());
        }
    }
}