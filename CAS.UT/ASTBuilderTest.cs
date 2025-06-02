using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CAS.Core;

namespace CAS.UT
{
  public class ASTBuilderTest
  {

    private void AssertTreesEqual(ASTNode expected, ASTNode actual)
    {
      Assert.Equal(expected.Token.Type.stringValue, actual.Token.Type.stringValue);
      Assert.Equal(expected.Children.Count, actual.Children.Count);

      for (int i = 0; i < expected.Children.Count; i++)
      {
        AssertTreesEqual(expected.Children[i], actual.Children[i]);
      }
    }

    [Fact]
    public void Arithmetic()
    {
      List<Token> list1 = new List<Token>
      {
        Token.Number("5"),
        Token.Operator("-"),
        Token.Number("2"),
        Token.Operator("+"),
        Token.Number("3")
      };

      List<Token> list2 = new List<Token>
      {
        Token.Number("6"),
        Token.Operator("*"),
        Token.Number("3"),
        Token.Operator("/"),
        Token.Number("2")
       };

      List<Token> list3 = new List<Token>
      {
        Token.LeftParenthesis(),
        Token.Number("1"),
        Token.Operator("+"),
        Token.Number("2"),
        Token.RightParenthesis(),
        Token.Operator("*"),
        Token.Number("3")
      };

      var expected1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("-"), new List<ASTNode>
        {
          new ASTNode(Token.Number("5"), new List<ASTNode>()),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
        }),
        new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      var expected2 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
              new ASTNode(Token.Number("6"), new List<ASTNode>()),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
          }),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
      });

      var expected3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Number("1"), new List<ASTNode>()),
              new ASTNode(Token.Number("2"), new List<ASTNode>())
          }),
          new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      ASTBuilder builder = new ASTBuilder();

      var result1 = builder.ParseInfixToAST(list1);
      var result2 = builder.ParseInfixToAST(list2);
      var result3 = builder.ParseInfixToAST(list3);

      AssertTreesEqual(expected1, result1);
      AssertTreesEqual(expected2, result2);
      AssertTreesEqual(expected3, result3);
    }

    [Fact]
    public void Polynomials()
    {
      List<Token> list1 = new List<Token>
      {
        Token.Variable("x"),
        Token.Operator("^"),
        Token.Number("2"),
        Token.Operator("+"),
        Token.Number("2"),
        Token.Operator("*"),
        Token.Variable("x"),
        Token.Operator("+"),
        Token.Number("1")
      };

      List<Token> list2 = new List<Token>
      {
        Token.Variable("a"),
        Token.Operator("^"),
        Token.Number("3"),
        Token.Operator("+"),
        Token.Variable("b"),
        Token.Operator("^"),
        Token.Number("2"),
        Token.Operator("+"),
        Token.Variable("c")
      };

      List<Token> list3 = new List<Token>
      {
        Token.Number("3"),
        Token.Operator("*"),
        Token.Variable("x"),
        Token.Operator("^"),
        Token.Number("2"),
        Token.Operator("-"),
        Token.Number("4"),
        Token.Operator("*"),
        Token.Variable("x"),
        Token.Operator("+"),
        Token.Number("5")
      };

      var expected1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("x"), new List<ASTNode>()),
                  new ASTNode(Token.Number("2"), new List<ASTNode>())
              }),
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("2"), new List<ASTNode>()),
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          }),
          new ASTNode(Token.Number("1"), new List<ASTNode>())
      });

      var expected2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("a"), new List<ASTNode>()),
                  new ASTNode(Token.Number("3"), new List<ASTNode>())
              }),
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("b"), new List<ASTNode>()),
                  new ASTNode(Token.Number("2"), new List<ASTNode>())
              })
          }),
          new ASTNode(Token.Variable("c"), new List<ASTNode>())
      });

      var expected3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("-"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("3"), new List<ASTNode>()),
                  new ASTNode(Token.Operator("^"), new List<ASTNode>
                  {
                      new ASTNode(Token.Variable("x"), new List<ASTNode>()),
                      new ASTNode(Token.Number("2"), new List<ASTNode>())
                  })
              }),
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("4"), new List<ASTNode>()),
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          }),
          new ASTNode(Token.Number("5"), new List<ASTNode>())
      });

      ASTBuilder builder = new ASTBuilder();

      var result1 = builder.ParseInfixToAST(list1);
      var result2 = builder.ParseInfixToAST(list2);
      var result3 = builder.ParseInfixToAST(list3);

      AssertTreesEqual(expected1, result1);
      AssertTreesEqual(expected2, result2);
      AssertTreesEqual(expected3, result3);

    }

    [Fact]
    public void Functions()
    {
      List<Token> list1 = new List<Token>
      {
        Token.Function("sin"),
        Token.LeftParenthesis(),
        Token.Variable("x"),
        Token.RightParenthesis()
      };

      List<Token> list2 = new List<Token>
      {
        Token.Function("log"),
        Token.LeftParenthesis(),
        Token.Number("10"),
        Token.FunctionArgumentSeparator(),
        Token.Number("100"),
        Token.RightParenthesis()
      };

      List<Token> list3 = new List<Token>
      {
        Token.Function("sqrt"),
        Token.LeftParenthesis(),
        Token.Number("4"),
        Token.Operator("+"),
        Token.LeftParenthesis(),
        Token.Number("2"),
        Token.Operator("*"),
        Token.Variable("x"),
        Token.RightParenthesis(),
        Token.RightParenthesis()
      };

      var expected1 = new ASTNode(Token.Function("sin"), new List<ASTNode>
      {
          new ASTNode(Token.Variable("x"), new List<ASTNode>())
      });

      var expected2 = new ASTNode(Token.Function("log"), new List<ASTNode>
      {
          new ASTNode(Token.Number("100"), new List<ASTNode>()),
          new ASTNode(Token.Number("10"), new List<ASTNode>())
      });

      var expected3 = new ASTNode(Token.Function("sqrt"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Number("4"), new List<ASTNode>()),
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("2"), new List<ASTNode>()),
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          })
      });

      ASTBuilder builder = new ASTBuilder();

      var result1 = builder.ParseInfixToAST(list1);
      var result2 = builder.ParseInfixToAST(list2);
      var result3 = builder.ParseInfixToAST(list3);

      AssertTreesEqual(expected1, result1);
      AssertTreesEqual(expected2, result2);
      AssertTreesEqual(expected3, result3);
    }

    [Fact]
    public void ImplicitMultiplication()
    {
      List<Token> list1 = new List<Token>
      {
        Token.Number("3"),
        Token.Operator("*"),
        Token.Variable("x")
      };

      List<Token> list2 = new List<Token>
      {
        Token.Number("2"),
        Token.Operator("*"),
        Token.Function("sin"),
        Token.LeftParenthesis(),
        Token.Variable("x"),
        Token.RightParenthesis()
      };

      List<Token> list3 = new List<Token>
      {
        Token.Number("4"),
        Token.Operator("*"),
        Token.LeftParenthesis(),
        Token.Variable("b"),
        Token.Operator("+"),
        Token.Variable("c"),
        Token.RightParenthesis()
      };

      var expected1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
          new ASTNode(Token.Number("3"), new List<ASTNode>()),
          new ASTNode(Token.Variable("x"), new List<ASTNode>())
      });

      var expected2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
          new ASTNode(Token.Number("2"), new List<ASTNode>()),
          new ASTNode(Token.Function("sin"), new List<ASTNode>
          {
              new ASTNode(Token.Variable("x"), new List<ASTNode>())
          })
      });

      var expected3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
          new ASTNode(Token.Number("4"), new List<ASTNode>()),
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Variable("b"), new List<ASTNode>()),
              new ASTNode(Token.Variable("c"), new List<ASTNode>())
          })
      });

      ASTBuilder builder = new ASTBuilder();

      var result1 = builder.ParseInfixToAST(list1);
      var result2 = builder.ParseInfixToAST(list2);
      var result3 = builder.ParseInfixToAST(list3);

      AssertTreesEqual(expected1, result1);
      AssertTreesEqual(expected2, result2);
      AssertTreesEqual(expected3, result3);
    }

    [Fact]
    public void NestedFunctions()
    {
      List<Token> list1 = new List<Token>
      {
        Token.Function("f"),
        Token.LeftParenthesis(),
        Token.Function("g"),
        Token.LeftParenthesis(),
        Token.Variable("x"),
        Token.RightParenthesis(),
        Token.RightParenthesis()
      };

      List<Token> list2 = new List<Token>
      {
        Token.Function("h"),
        Token.LeftParenthesis(),
        Token.Function("f"),
        Token.LeftParenthesis(),
        Token.Function("g"),
        Token.LeftParenthesis(),
        Token.Variable("x"),
        Token.RightParenthesis(),
        Token.RightParenthesis(),
        Token.RightParenthesis()
      };

      List<Token> list3 = new List<Token>
      {
        Token.Function("f"),
        Token.LeftParenthesis(),
        Token.Function("g"),
        Token.LeftParenthesis(),
        Token.Variable("x"),
        Token.RightParenthesis(),
        Token.Operator("+"),
        Token.Function("h"),
        Token.LeftParenthesis(),
        Token.Variable("y"),
        Token.RightParenthesis(),
        Token.RightParenthesis()
      };

      var expected1 = new ASTNode(Token.Function("f"), new List<ASTNode>
      {
          new ASTNode(Token.Function("g"), new List<ASTNode>
          {
              new ASTNode(Token.Variable("x"), new List<ASTNode>())
          })
      });

      var expected2 = new ASTNode(Token.Function("h"), new List<ASTNode>
      {
          new ASTNode(Token.Function("f"), new List<ASTNode>
          {
              new ASTNode(Token.Function("g"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          })
      });

      var expected3 = new ASTNode(Token.Function("f"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Function("g"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              }),
              new ASTNode(Token.Function("h"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("y"), new List<ASTNode>())
              })
          })
      });

      ASTBuilder builder = new ASTBuilder();

      var result1 = builder.ParseInfixToAST(list1);
      var result2 = builder.ParseInfixToAST(list2);
      var result3 = builder.ParseInfixToAST(list3);

      AssertTreesEqual(expected1, result1);
      AssertTreesEqual(expected2, result2);
      AssertTreesEqual(expected3, result3);
    }
  }
}
