using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CAS.Core.EquationParsing;

namespace CAS.UT
{
  public class StringTokenizerTest
  {
    // Useful to generate tests
    // Console.WriteLine("Assert.Equal(" + "\"" + s1 + "\"" + ", s1);");
    
    /// <summary>
    /// Assert that two sets are equal
    /// </summary>
    public static void AssertSetEqual<T>(HashSet<T> expected, HashSet<T> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var item in expected)
        {
            Assert.Contains(item, actual);
        }

        foreach (var item in actual)
        {
            Assert.Contains(item, expected);
        }
    }

    private string TokenListToTestString(List<Token> tokenizedString)
    {
      string result = "";
      foreach (Token t in tokenizedString)
        result += t.ToString();

      return result;
    }

    [Fact]
    public void Arithmetic()
    {
      var result1 = StringTokenizer.Tokenize("5 - 2 + 3");
      var result2 = StringTokenizer.Tokenize("6 * 3 / 2");
      var result3 = StringTokenizer.Tokenize("(1 + 2) * 3");

      string s1 = TokenListToTestString(result1.TokenizedExpression);
      string s2 = TokenListToTestString(result2.TokenizedExpression);
      string s3 = TokenListToTestString(result3.TokenizedExpression);
      var expectedSymbols = new HashSet<string>();

      Assert.Equal("Number(5)Operator('+')Number(-1)Operator('*')Number(2)Operator('+')Number(3)", s1);
      AssertSetEqual(expectedSymbols, result1.Symbols);
      Assert.Equal("Number(6)Operator('*')Number(3)Operator('/')Number(2)", s2);
      AssertSetEqual(expectedSymbols, result2.Symbols);
      Assert.Equal("LeftParenthesis('(')Number(1)Operator('+')Number(2)RightParenthesis(')')Operator('*')Number(3)", s3);
      AssertSetEqual(expectedSymbols, result3.Symbols);
    }

    [Fact]
    public void Polynomials()
    {
      var result1 = StringTokenizer.Tokenize("x^2 + 2x + 1");
      var result2 = StringTokenizer.Tokenize("a^3 + b^2 + c");
      var result3 = StringTokenizer.Tokenize("3x^2 - 4x + 5");

      string s1 = TokenListToTestString(result1.TokenizedExpression);
      string s2 = TokenListToTestString(result2.TokenizedExpression);
      string s3 = TokenListToTestString(result3.TokenizedExpression);

      var expectedSymbols1 = new HashSet<string> { "x" };
      var expectedSymbols2 = new HashSet<string> { "a", "b", "c" };
      var expectedSymbols3 = new HashSet<string> { "x" };

      Assert.Equal("Variable('x')Operator('^')Number(2)Operator('+')Number(2)Operator('*')Variable('x')Operator('+')Number(1)", s1);
      AssertSetEqual(expectedSymbols1, result1.Symbols);
      Assert.Equal("Variable('a')Operator('^')Number(3)Operator('+')Variable('b')Operator('^')Number(2)Operator('+')Variable('c')", s2);
      AssertSetEqual(expectedSymbols2, result2.Symbols);
      Assert.Equal("Number(3)Operator('*')Variable('x')Operator('^')Number(2)Operator('+')Number(-1)Operator('*')Number(4)Operator('*')Variable('x')Operator('+')Number(5)", s3);
      AssertSetEqual(expectedSymbols3, result3.Symbols);
    }

    [Fact]
    public void Functions()
    {
      var result1 = StringTokenizer.Tokenize("sin(x)");
      var result2 = StringTokenizer.Tokenize("log(10, 100)");
      var result3 = StringTokenizer.Tokenize("sqrt(4 + (2 * x))");

      string s1 = TokenListToTestString(result1.TokenizedExpression);
      string s2 = TokenListToTestString(result2.TokenizedExpression);
      string s3 = TokenListToTestString(result3.TokenizedExpression);

      var expectedSymbols1 = new HashSet<string> { "x" };
      var expectedSymbols2 = new HashSet<string>();
      var expectedSymbols3 = new HashSet<string> { "x" };

      Assert.Equal("Function('sin')LeftParenthesis('(')Variable('x')RightParenthesis(')')", s1);
      AssertSetEqual(expectedSymbols1, result1.Symbols);
      Assert.Equal("Function('log')LeftParenthesis('(')Number(10)FunctionArgumentSeparator(',')Number(100)RightParenthesis(')')", s2);
      AssertSetEqual(expectedSymbols2, result2.Symbols);
      Assert.Equal("Function('sqrt')LeftParenthesis('(')Number(4)Operator('+')LeftParenthesis('(')Number(2)Operator('*')Variable('x')RightParenthesis(')')RightParenthesis(')')", s3);
      AssertSetEqual(expectedSymbols3, result3.Symbols);
    }

    [Fact]
    public void ImplicitMultiplication()
    {
      var result1 = StringTokenizer.Tokenize("3x");
      var result2 = StringTokenizer.Tokenize("2sin(x)");
      var result3 = StringTokenizer.Tokenize("4(b + c)");

      string s1 = TokenListToTestString(result1.TokenizedExpression);
      string s2 = TokenListToTestString(result2.TokenizedExpression);
      string s3 = TokenListToTestString(result3.TokenizedExpression);

      var expectedSymbols1 = new HashSet<string> { "x" };
      var expectedSymbols2 = new HashSet<string> { "x" };
      var expectedSymbols3 = new HashSet<string> { "b", "c" };

      Assert.Equal("Number(3)Operator('*')Variable('x')", s1);
      AssertSetEqual(expectedSymbols1, result1.Symbols);
      Assert.Equal("Number(2)Operator('*')Function('sin')LeftParenthesis('(')Variable('x')RightParenthesis(')')", s2);
      AssertSetEqual(expectedSymbols2, result2.Symbols);
      Assert.Equal("Number(4)Operator('*')LeftParenthesis('(')Variable('b')Operator('+')Variable('c')RightParenthesis(')')", s3);
      AssertSetEqual(expectedSymbols3, result3.Symbols);
    }

    [Fact]
    public void NestedFunctions()
    {
      var result1 = StringTokenizer.Tokenize("f(g(x))");
      var result2 = StringTokenizer.Tokenize("h(f(g(x)))");
      var result3 = StringTokenizer.Tokenize("f(g(x) + h(y))");

      string s1 = TokenListToTestString(result1.TokenizedExpression);
      string s2 = TokenListToTestString(result2.TokenizedExpression);
      string s3 = TokenListToTestString(result3.TokenizedExpression);

      var expectedSymbols1 = new HashSet<string> { "x" };
      var expectedSymbols2 = new HashSet<string> { "x" };
      var expectedSymbols3 = new HashSet<string> { "x", "y" };

      Assert.Equal("Function('f')LeftParenthesis('(')Function('g')LeftParenthesis('(')Variable('x')RightParenthesis(')')RightParenthesis(')')", s1);
      AssertSetEqual(expectedSymbols1, result1.Symbols);
      Assert.Equal("Function('h')LeftParenthesis('(')Function('f')LeftParenthesis('(')Function('g')LeftParenthesis('(')Variable('x')RightParenthesis(')')RightParenthesis(')')RightParenthesis(')')", s2);
      AssertSetEqual(expectedSymbols2, result2.Symbols);
      Assert.Equal("Function('f')LeftParenthesis('(')Function('g')LeftParenthesis('(')Variable('x')RightParenthesis(')')Operator('+')Function('h')LeftParenthesis('(')Variable('y')RightParenthesis(')')RightParenthesis(')')", s3);
      AssertSetEqual(expectedSymbols3, result3.Symbols);
    }

    [Fact]
    public void UnaryMinus()
    {
      var result1 = StringTokenizer.Tokenize("-3");
      var result2 = StringTokenizer.Tokenize("-5x + 4");
      var result3 = StringTokenizer.Tokenize("8 + (-9 + sin(x))");

      string s1 = TokenListToTestString(result1.TokenizedExpression);
      string s2 = TokenListToTestString(result2.TokenizedExpression);
      string s3 = TokenListToTestString(result3.TokenizedExpression);

      var expectedSymbols1 = new HashSet<string>();
      var expectedSymbols2 = new HashSet<string> { "x" };
      var expectedSymbols3 = new HashSet<string> { "x" };

      Assert.Equal("Number(-1)Operator('*')Number(3)", s1);
      AssertSetEqual(expectedSymbols1, result1.Symbols);

      Assert.Equal("Number(-1)Operator('*')Number(5)Operator('*')Variable('x')Operator('+')Number(4)", s2);
      AssertSetEqual(expectedSymbols2, result2.Symbols);

      Assert.Equal("Number(8)Operator('+')LeftParenthesis('(')Number(-1)Operator('*')Number(9)Operator('+')Function('sin')LeftParenthesis('(')Variable('x')RightParenthesis(')')RightParenthesis(')')", s3);
      AssertSetEqual(expectedSymbols3, result3.Symbols);
    }

    [Fact]
    public void InvalidExpressions()
    {
      Assert.Throws<ArgumentException>(() => StringTokenizer.Tokenize("(2 + 3"));           // Unbalanced parentheses
      Assert.Throws<ArgumentException>(() => StringTokenizer.Tokenize("f()"));              // Empty function call
      Assert.Throws<ArgumentException>(() => StringTokenizer.Tokenize("3 + @"));            // Invalid character
      Assert.Throws<ArgumentException>(() => StringTokenizer.Tokenize("3 - 8 + () - 3x"));  // Useless parentheses
    }
  }
}
