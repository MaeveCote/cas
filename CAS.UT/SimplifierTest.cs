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
    private ASTNode Int(int value) =>
        new ASTNode(Token.Integer(value.ToString()), new List<ASTNode>());

    private ASTNode Frac(int num, int den) =>
      new ASTNode(Token.Fraction(), new List<ASTNode>
      {
      Int(num),
      Int(den)
      });

    private ASTNode Op(string symbol, params ASTNode[] operands) =>
      new ASTNode(Token.Operator(symbol), new List<ASTNode>(operands));

    private ASTNode Decimal(string val) =>
      new ASTNode(Token.Number(val), new List<ASTNode>());

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
    [Fact]
    public void SimplifyRNE_SimpleAddition()
    {
      var input = Op("+", Int(2), Int(3)); // 2 + 3
      var simplifier = new Simplifier();
      var result = simplifier.SimplifyRNE(input);

      Assert.Equal("5", result.Token.Type.stringValue); // Should simplify to 5
    }

    [Fact]
    public void SimplifyRNE_MediumNestedExpression()
    {
      // (2/3 + 3/4) * 2
      var frac1 = Frac(2, 3);
      var frac2 = Frac(3, 4);
      var sum = Op("+", frac1, frac2);
      var input = Op("*", sum, Int(2));

      var simplifier = new Simplifier();
      var result = simplifier.SimplifyRNE(input);

      // Expected: (2/3 + 3/4) = 17/12, then *2 = 34/12 = 17/6
      Assert.Equal("17", result.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("6", result.OperandAt(1).Token.Type.stringValue);
    }

    [Fact]
    public void SimplifyRNE_HardDeepExpression()
    {
      // ((1 + 1/2) ^ 2) / (3 - 1/3)
      var half = Frac(1, 2);
      var one = Int(1);
      var two = Int(2);
      var onePlusHalf = Op("+", one, half);             // 1 + 1/2
      var pow = Op("^", onePlusHalf, two);              // (1 + 1/2)^2
      var three = Int(3);
      var third = Frac(1, 3);
      var denom = Op("-", three, third);                // 3 - 1/3
      var input = Op("/", pow, denom);                  // full expression

      var simplifier = new Simplifier();
      var result = simplifier.SimplifyRNE(input);

      // (3/2)^2 = 9/4, 3 - 1/3 = 8/3, 9/4 ÷ 8/3 = 27/32
      Assert.Equal("27", result.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("32", result.OperandAt(1).Token.Type.stringValue);
    }

    [Fact]
    public void SimplifyRNE_InvalidDecimalInput_Throws()
    {
      var invalid = Decimal("2.5");
      var simplifier = new Simplifier();

      Assert.Throws<ArgumentException>(() => simplifier.SimplifyRNE(invalid));
    }
  }
}
