using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAS.Core.EquationParsing;
using CAS.Core;
using Xunit;

namespace CAS.UT
{
  public class SimplifierTest
  {
    [Fact]
    public void FormatTree()
    {
      var inside = new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
              new ASTNode(Token.Number("6"), new List<ASTNode>()),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
          });

      var equation1 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
              new ASTNode(Token.Number("6"), new List<ASTNode>()),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
          }),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
      });

      var equation2 = new ASTNode(Token.Operator("-"), new List<ASTNode>
      {
        // Left: f(x^2 + 3) + g(sin(x) * ln(y))
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          // f(x^2 + 3)
          new ASTNode(Token.Function("f"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("+"), new List<ASTNode>
            {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                new ASTNode(Token.Variable("x"), new List<ASTNode>()),
                new ASTNode(Token.Number("2"), new List<ASTNode>())
              }),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
            })
          }),
          // g(sin(x) * ln(y))
          new ASTNode(Token.Function("g"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("*"), new List<ASTNode>
            {
              new ASTNode(Token.Function("sin"), new List<ASTNode>
              {
                new ASTNode(Token.Variable("x"), new List<ASTNode>())
              }),
              new ASTNode(Token.Function("ln"), new List<ASTNode>
              {
                new ASTNode(Token.Variable("y"), new List<ASTNode>())
              })
            })
          })
        }),
        // Right: 4 / (x + 1)
        new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Number("4"), new List<ASTNode>()),
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            new ASTNode(Token.Variable("x"), new List<ASTNode>()),
            new ASTNode(Token.Number("1"), new List<ASTNode>())
          })
        })
      });
      
      var equation3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("+"), new List<ASTNode>
              {
                  new ASTNode(Token.Operator("/"), new List<ASTNode>
                  {
                      new ASTNode(Token.Number("2"), new List<ASTNode>()),
                      new ASTNode(Token.Number("3"), new List<ASTNode>())
                  }),
                  new ASTNode(Token.Operator("/"), new List<ASTNode>
                  {
                      new ASTNode(Token.Number("4"), new List<ASTNode>()),
                      new ASTNode(Token.Number("5"), new List<ASTNode>())
                  })
              }),
              new ASTNode(Token.Operator("/"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("6"), new List<ASTNode>()),
                  new ASTNode(Token.Number("7"), new List<ASTNode>())
              })
          }),
          new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      Dictionary<string, double> symbolTable = new Dictionary<string, double> { { "x", 1.75 }, { "y", 30 } };

      var customFunctionTable = new Dictionary<string, Func<List<double>, double>>
      {
        { "f", (args) => (Math.Pow(args[0], 2) + 2 * args[0] + 3)},
        { "g", (args) => (Math.Pow(Math.Sin(args[0]), 2) + Math.Cos(args[0]))},
      };

      Simplifier simplifier = new Simplifier();

      simplifier.FormatTree(equation1);
      simplifier.FormatTree(equation2);
      simplifier.FormatTree(equation3);

      Console.WriteLine(equation1.ToString());
      Console.WriteLine(equation2.ToString());
      Console.WriteLine(equation3.ToString());

      var result1 = Calculator.Evaluate(equation1);
      var result2 = Calculator.Evaluate(equation2, symbolTable, customFunctionTable);
      var result3 = Calculator.Evaluate(equation3);

      Assert.Equal(9, result1, 0.001);
      Assert.Equal(49.481, result2, 0.01);
      Assert.Equal(5.3238, result3, 0.01);
    }

    [Fact]
    public void SimplifyRationalNumber()
    {
      var frac1 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("5"), new List<ASTNode>()),
          new ASTNode(Token.Integer("3"), new List<ASTNode>())
      });

      var frac2 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("15"), new List<ASTNode>()),
          new ASTNode(Token.Integer("3"), new List<ASTNode>())
      });

      var frac3 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("14"), new List<ASTNode>()),
          new ASTNode(Token.Integer("1"), new List<ASTNode>())
      });

      var frac4 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("2"), new List<ASTNode>()),
          new ASTNode(Token.Integer("0"), new List<ASTNode>())
      });

      var frac5 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("-16"), new List<ASTNode>()),
          new ASTNode(Token.Integer("4"), new List<ASTNode>())
      });

      var frac6 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("-4"), new List<ASTNode>()),
          new ASTNode(Token.Integer("16"), new List<ASTNode>())
      });

      var frac7 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("4"), new List<ASTNode>()),
          new ASTNode(Token.Integer("-16"), new List<ASTNode>())
      });

      var frac8 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Number("0.2"), new List<ASTNode>()),
          new ASTNode(Token.Integer("2"), new List<ASTNode>())
      });

      var frac9 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("0"), new List<ASTNode>()),
          new ASTNode(Token.Integer("2"), new List<ASTNode>())
      });
      var frac10 = new ASTNode(Token.Integer("16"), new List<ASTNode>());

      var simplified1 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("5"), new List<ASTNode>()),
          new ASTNode(Token.Integer("3"), new List<ASTNode>())
      });
      var simplified2 = new ASTNode(Token.Integer("5"), new List<ASTNode>());
      var simplified3 = new ASTNode(Token.Integer("14"), new List<ASTNode>());
      var simplified4 = new ASTNode(Token.Undefined(), new List<ASTNode>());
      var simplified5 = new ASTNode(Token.Integer("-4"), new List<ASTNode>());
      var simplified6 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("-1"), new List<ASTNode>()),
          new ASTNode(Token.Integer("4"), new List<ASTNode>())
      });
      var simplified7 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Integer("-1"), new List<ASTNode>()),
          new ASTNode(Token.Integer("4"), new List<ASTNode>())
      });
      var simplified8 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
          new ASTNode(Token.Number("0.2"), new List<ASTNode>()),
          new ASTNode(Token.Integer("2"), new List<ASTNode>())
      });
      var simplified9 = new ASTNode(Token.Integer("0"), new List<ASTNode>());
      var simplified10 = new ASTNode(Token.Integer("16"), new List<ASTNode>());

      Simplifier simplifier = new Simplifier();
      Assert.True(simplifier.SimplifyRationalNumber(frac1) == simplified1);
      Assert.True(simplifier.SimplifyRationalNumber(frac2) == simplified2);
      Assert.True(simplifier.SimplifyRationalNumber(frac3) == simplified3);
      Assert.True(simplifier.SimplifyRationalNumber(frac4) == simplified4);
      Assert.True(simplifier.SimplifyRationalNumber(frac5) == simplified5);
      Assert.True(simplifier.SimplifyRationalNumber(frac6) == simplified6);
      Assert.True(simplifier.SimplifyRationalNumber(frac7) == simplified7);
      Assert.True(simplifier.SimplifyRationalNumber(frac8) == simplified8);
      Assert.True(simplifier.SimplifyRationalNumber(frac9) == simplified9);
      Assert.True(simplifier.SimplifyRationalNumber(frac10) == simplified10);
    }
  }
}
