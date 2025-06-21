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
    public void SimplifyRNE_Addition()
    {
      var input = Op("+", Int(2), Int(3)); // 2 + 3
      var simplifier = new Simplifier();
      var result = simplifier.SimplifyRNE(input);

      Assert.Equal("5", result.Token.Type.stringValue); // Should simplify to 5
    }

    [Fact]
    public void SimplifyRNE_NestedExpression()
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
    public void SimplifyRNE_DeepExpression()
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
    public void SimplifyRNE_InvalidDecimalInputThrows()
    {
      var invalid = Decimal("2.5");
      var simplifier = new Simplifier();

      Assert.Throws<ArgumentException>(() => simplifier.SimplifyRNE(invalid));
    }

    [Fact]
    public void AutomaticSimplify_TrivialEquations()
    {
      var simplifier = new Simplifier();

      // 1. 2 + 3 => 5
      var expr1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Integer("3"))
      });
      Assert.Equal("5", simplifier.AutomaticSimplify(expr1).Token.Type.stringValue);

      // 2. x * 1 => x
      var expr2 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("1"))
      });
      Assert.Equal("x", simplifier.AutomaticSimplify(expr2).Token.Type.stringValue);

      // 3. x^1 => x
      var expr3 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("1"))
      });
      Assert.Equal("x", simplifier.AutomaticSimplify(expr3).Token.Type.stringValue);

      // 4. x^0 => 1
      var expr4 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("0"))
      });
      Assert.Equal("1", simplifier.AutomaticSimplify(expr4).Token.Type.stringValue);

      // 5. 6 / 3 => 2
      var expr5 = new ASTNode(Token.Fraction(), new()
      {
        new ASTNode(Token.Integer("6")),
        new ASTNode(Token.Integer("3"))
      });
      Assert.Equal("2", simplifier.AutomaticSimplify(expr5).Token.Type.stringValue);

      // 6. x * 0 => 0
      var expr6 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("0"))
      });
      Assert.Equal("0", simplifier.AutomaticSimplify(expr6).Token.Type.stringValue);

      // 7. (2 + 3) + 4 => 9
      var expr7 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Integer("3"))
        }),
        new ASTNode(Token.Integer("4"))
      });
      Assert.Equal("9", simplifier.AutomaticSimplify(expr7).Token.Type.stringValue);

      // 8. 1 + 1/2 + 1/2 => 2
      var expr8 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });
      Assert.Equal("2", simplifier.AutomaticSimplify(expr8).Token.Type.stringValue);
    }

    [Fact]
    public void AutomaticSimplify_AlgebraicEquations()
    {
      var simplifier = new Simplifier();

      // (2 * x) + (3 * x) => 5 * x
      var expr1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Variable("x"))
        }),
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("3")),
          new ASTNode(Token.Variable("x"))
        })
      });
      var expected1 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Integer("5")),
        new ASTNode(Token.Variable("x"))
      });
      Assert.True(simplifier.AutomaticSimplify(expr1) == expected1);

      // x^2 * x^3 => x^5
      var expr2 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("3"))
        })
      });
      var expected2 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("5"))
      });
      Assert.True(simplifier.AutomaticSimplify(expr2) == expected2);

      // (x^2)^3 => x^6
      var expr3 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Integer("3"))
      });
      var expected3 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("6"))
      });
      Assert.True(simplifier.AutomaticSimplify(expr3) == expected3);

      // 6x / 3 => 2x
      var expr4 = new ASTNode(Token.Operator("/"), new()
      {
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("6")),
          new ASTNode(Token.Variable("x"))
        }),
        new ASTNode(Token.Integer("3"))
      });
      var expected4 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Variable("x"))
      });
      Assert.True(simplifier.AutomaticSimplify(expr4) == expected4);

      // (x + x) * 2 => 4x
      var expr5 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Variable("x"))
        }),
        new ASTNode(Token.Integer("2"))
      });
      var expected5 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Integer("4")),
        new ASTNode(Token.Variable("x"))
      });
      Assert.True(simplifier.AutomaticSimplify(expr5) == expected5);

      // 2x - x => x
      var expr6 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Variable("x"))
        }),
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("-1")),
          new ASTNode(Token.Variable("x"))
        })
      });
      var expected6 = new ASTNode(Token.Variable("x"), new List<ASTNode>());
      Assert.True(simplifier.AutomaticSimplify(expr6) == expected6);
    }

    [Fact]
    public void AutomaticSimplify_ComplexEquations()
    {
      var simplifier = new Simplifier();

      // ((2 * x + 3 * x) + (x^2 + x^2)) => 5x + 2x^2
      var expr1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Integer("2")),
            new ASTNode(Token.Variable("x"))
          }),
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Integer("3")),
            new ASTNode(Token.Variable("x"))
          }),
        }),
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Operator("^"), new()
          {
            new ASTNode(Token.Variable("x")),
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Operator("^"), new()
          {
            new ASTNode(Token.Variable("x")),
            new ASTNode(Token.Integer("2"))
          })
        })
      });
      var expected1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("5")),
          new ASTNode(Token.Variable("x"))
        }),
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Operator("^"), new()
          {
            new ASTNode(Token.Variable("x")),
            new ASTNode(Token.Integer("2"))
          })
        })
      });
      Assert.True(simplifier.AutomaticSimplify(expr1) == expected1);

      // ((x^2)^3) * x^4 => x^10
      var expr2 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Operator("^"), new()
          {
            new ASTNode(Token.Variable("x")),
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Integer("3"))
        }),
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("4"))
        })
      });
      var expected2 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("10"))
      });
      Assert.True(simplifier.AutomaticSimplify(expr2) == expected2);

      // (x * 0) + (x + -x) + (3 * (2 + 4)) => 0 + 0 + 18 => 18
      var expr3 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("0"))
        }),
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Integer("-1")),
            new ASTNode(Token.Variable("x"))
          })
        }),
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("3")),
          new ASTNode(Token.Operator("+"), new()
          {
            new ASTNode(Token.Integer("2")),
            new ASTNode(Token.Integer("4"))
          })
        })
      });
      var expected3 = new ASTNode(Token.Integer("18"));
      var res = simplifier.AutomaticSimplify(expr3);
      Assert.True(simplifier.AutomaticSimplify(expr3) == expected3);

      // sqrt(3 * x + 2 * x) => sqrt(5x) [OK because simplification is inside argument]
      var expr4 = new ASTNode(Token.Function("sqrt"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Integer("3")),
            new ASTNode(Token.Variable("x"))
          }),
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Integer("2")),
            new ASTNode(Token.Variable("x"))
          })
        })
      });
      var expected4 = new ASTNode(Token.Function("sqrt"), new()
      {
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("5")),
          new ASTNode(Token.Variable("x"))
        })
      });
      Assert.True(simplifier.AutomaticSimplify(expr4) == expected4);
    }

    [Fact]
    public void AutomaticSimplify_DeeplyNestedEquations()
    {
      var simplifier = new Simplifier();

      // --- CASE 1: Deeply nested sum and product ---
      // ((((x + x) + (x + x)) + ((2 * x) + (3 * x))) + (((x ^ 2) + (2 * x ^ 2)) + (x + (x + x))))
      // Expected: 5x^2 + 10x
      var expr1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Operator("+"), new()
          {
            new ASTNode(Token.Operator("+"), new()
            {
              new ASTNode(Token.Variable("x")),
              new ASTNode(Token.Variable("x"))
            }),
            new ASTNode(Token.Operator("+"), new()
            {
              new ASTNode(Token.Variable("x")),
              new ASTNode(Token.Variable("x"))
            })
          }),
          new ASTNode(Token.Operator("+"), new()
          {
            new ASTNode(Token.Operator("*"), new()
            {
              new ASTNode(Token.Integer("2")),
              new ASTNode(Token.Variable("x"))
            }),
            new ASTNode(Token.Operator("*"), new()
            {
              new ASTNode(Token.Integer("3")),
              new ASTNode(Token.Variable("x"))
            })
          })
        }),
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Operator("+"), new()
          {
            new ASTNode(Token.Operator("^"), new()
            {
              new ASTNode(Token.Variable("x")),
              new ASTNode(Token.Integer("2"))
            }),
            new ASTNode(Token.Operator("*"), new()
            {
              new ASTNode(Token.Integer("2")),
              new ASTNode(Token.Operator("^"), new()
              {
                new ASTNode(Token.Variable("x")),
                new ASTNode(Token.Integer("2"))
              })
            })
          }),
          new ASTNode(Token.Operator("+"), new()
          {
            new ASTNode(Token.Variable("x")),
            new ASTNode(Token.Operator("+"), new()
            {
              new ASTNode(Token.Variable("x")),
              new ASTNode(Token.Variable("x"))
            })
          })
        })
      });

      var expected1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("12")),
          new ASTNode(Token.Variable("x"))
        }),
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("3")),
          new ASTNode(Token.Operator("^"), new()
          {
            new ASTNode(Token.Variable("x")),
            new ASTNode(Token.Integer("2"))
          })
        })
      });

      var result1 = simplifier.AutomaticSimplify(expr1);
      Assert.True(result1 == expected1);

      // --- CASE 2: Deeply nested power ((x^2)^3)^2 => x^12 ---
      var x = new ASTNode(Token.Variable("x"));
      var power = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Operator("^"), new()
          {
            x,
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Integer("3"))
        }),
        new ASTNode(Token.Integer("2"))
      });

      var expected2 = new ASTNode(Token.Operator("^"), new()
      {
        x,
        new ASTNode(Token.Integer("12"))
      });

      var result2 = simplifier.AutomaticSimplify(power);
      Assert.True(result2 == expected2);
    }

    [Fact]
    public void AutomaticSimplify_FractionalExponents()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));

      // Case 1: (x^(1/2))^2 => x
      var expr1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")),
            new ASTNode(Token.Integer("2"))
          })
        }),
        new ASTNode(Token.Integer("2"))
      });

      var expected1 = x;
      Assert.True(simplifier.AutomaticSimplify(expr1) == expected1);

      // Case 2: (x^2)^(1/2) => x
      var expr2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var expected2 = x;
      Assert.True(simplifier.AutomaticSimplify(expr2) == expected2);

      // Case 3: ((x^2)^(1/3))^3 => x^2
      var expr3 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")),
            new ASTNode(Token.Integer("3"))
          })
        }),
        new ASTNode(Token.Integer("3"))
      });

      var expected3 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("2"))
      });

          Assert.True(simplifier.AutomaticSimplify(expr3) == expected3);

          // Case 4: ((x^(1/2))^(1/2))^(1/2) => x^(1/8)
          var half = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Integer("2"))
      });

      var expr4 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x, half
          }),
          half
        }),
        half
      });

      var expected4 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("8"))
        })
      });

      Assert.True(simplifier.AutomaticSimplify(expr4) == expected4);
    }

  }
}
