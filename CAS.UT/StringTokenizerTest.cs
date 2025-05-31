using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CAS.Core;
using System.Windows.Input;

namespace CAS.UT
{
  public class StringTokenizerTest
  {
    private string TokenListToTestString(List<Token> tokenizedString)
    {
      string result = "";
      foreach (Token t in tokenizedString)
        result += t.ToString();

      return result;
    }

    // Useful to generate tests
    // Console.WriteLine("Assert.Equal(" + "\"" + s1 + "\"" + ", s1);");

    [Fact]
    public void Arithmetic()
    {
      StringTokenizer tokenizer = new StringTokenizer();

      var result1 = tokenizer.Tokenize("5 - 2 + 3");
      var result2 = tokenizer.Tokenize("6 * 3 / 2");
      var result3 = tokenizer.Tokenize("(1 + 2) * 3");

      string s1 = TokenListToTestString(result1);
      string s2 = TokenListToTestString(result2);
      string s3 = TokenListToTestString(result3);

      Assert.Equal("Number(5)Operator('-')Number(2)Operator('+')Number(3)", s1);
      Assert.Equal("Number(6)Operator('*')Number(3)Operator('/')Number(2)", s2);
      Assert.Equal("LeftParenthesis('(')Number(1)Operator('+')Number(2)RightParenthesis(')')Operator('*')Number(3)", s3);
    }

    [Fact]
    public void Polynomials()
    {
      StringTokenizer tokenizer = new StringTokenizer();

      var result1 = tokenizer.Tokenize("x^2 + 2x + 1");
      var result2 = tokenizer.Tokenize("a^3 + b^2 + c");
      var result3 = tokenizer.Tokenize("3x^2 - 4x + 5");

      string s1 = TokenListToTestString(result1);
      string s2 = TokenListToTestString(result2);
      string s3 = TokenListToTestString(result3);

      Assert.Equal("Variable('x')Operator('^')Number(2)Operator('+')Number(2)Operator('*')Variable('x')Operator('+')Number(1)", s1);
      Assert.Equal("Variable('a')Operator('^')Number(3)Operator('+')Variable('b')Operator('^')Number(2)Operator('+')Variable('c')", s2);
      Assert.Equal("Number(3)Operator('*')Variable('x')Operator('^')Number(2)Operator('-')Number(4)Operator('*')Variable('x')Operator('+')Number(5)", s3);
    }

    [Fact]
    public void Functions()
    {
      StringTokenizer tokenizer = new StringTokenizer();

      var result1 = tokenizer.Tokenize("sin(x)");
      var result2 = tokenizer.Tokenize("log(10, 100)");
      var result3 = tokenizer.Tokenize("sqrt(4 + (2 * x))");
      
      string s1 = TokenListToTestString(result1);
      string s2 = TokenListToTestString(result2);
      string s3 = TokenListToTestString(result3);
      
      Assert.Equal("Function('sin')LeftParenthesis('(')Variable('x')RightParenthesis(')')", s1);
      Assert.Equal("Function('log')LeftParenthesis('(')Number(10)FunctionArgumentSeparator(',')Number(100)RightParenthesis(')')", s2);
      Assert.Equal("Function('sqrt')LeftParenthesis('(')Number(4)Operator('+')LeftParenthesis('(')Number(2)Operator('*')Variable('x')RightParenthesis(')')RightParenthesis(')')", s3);
    }

    [Fact]
    public void ImplicitMultiplication()
    {
      StringTokenizer tokenizer = new StringTokenizer();

      var result1 = tokenizer.Tokenize("3x");
      var result2 = tokenizer.Tokenize("2sin(x)");
      var result3 = tokenizer.Tokenize("4(b + c)");

      string s1 = TokenListToTestString(result1);
      string s2 = TokenListToTestString(result2);
      string s3 = TokenListToTestString(result3);

      Assert.Equal("Number(3)Operator('*')Variable('x')", s1);
      Assert.Equal("Number(2)Operator('*')Function('sin')LeftParenthesis('(')Variable('x')RightParenthesis(')')", s2);
      Assert.Equal("Number(4)Operator('*')LeftParenthesis('(')Variable('b')Operator('+')Variable('c')RightParenthesis(')')", s3);
    }

    [Fact]
    public void NestedFunctions()
    {
      StringTokenizer tokenizer = new StringTokenizer();

      var result1 = tokenizer.Tokenize("f(g(x))");
      var result2 = tokenizer.Tokenize("h(f(g(x)))");
      var result3 = tokenizer.Tokenize("f(g(x) + h(y))");

      string s1 = TokenListToTestString(result1);
      string s2 = TokenListToTestString(result2);
      string s3 = TokenListToTestString(result3);

      Assert.Equal("Function('f')LeftParenthesis('(')Function('g')LeftParenthesis('(')Variable('x')RightParenthesis(')')RightParenthesis(')')", s1);
      Assert.Equal("Function('h')LeftParenthesis('(')Function('f')LeftParenthesis('(')Function('g')LeftParenthesis('(')Variable('x')RightParenthesis(')')RightParenthesis(')')RightParenthesis(')')", s2);
      Assert.Equal("Function('f')LeftParenthesis('(')Function('g')LeftParenthesis('(')Variable('x')RightParenthesis(')')Operator('+')Function('h')LeftParenthesis('(')Variable('y')RightParenthesis(')')RightParenthesis(')')", s3);
    }

    [Fact]
    public void InvalidExpressions()
    {
      StringTokenizer tokenizer = new StringTokenizer();
      Assert.Throws<ArgumentException>(() => tokenizer.Tokenize("(2 + 3"));           // Unbalanced parentheses
      Assert.Throws<ArgumentException>(() => tokenizer.Tokenize("f()"));              // Empty function call
      Assert.Throws<ArgumentException>(() => tokenizer.Tokenize("3 + @"));            // Invalid character
      Assert.Throws<ArgumentException>(() => tokenizer.Tokenize("3 - 8 + () - 3x"));  // Useless parentheses
    }
  }
}
