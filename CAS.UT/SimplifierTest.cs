﻿using System;
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

    #region Formatters

    [Fact]
    public void FormatTree()
    {
      var simplifier = new Simplifier();

      // Case 1: Should convert Number("3.0") to Integer("3")
      var tree1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Number("3.0")),
        new ASTNode(Token.Variable("x"))
      });

      var expected1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("3")),
        new ASTNode(Token.Variable("x"))
      });

      simplifier.FormatTree(tree1);
      Assert.True(tree1 == expected1);

      // Case 2: Should convert everything to Number() because 3.5 is not an integer
      var tree2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Number("3.5")),
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var expected2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Number("3.5")),
        new ASTNode(Token.Number("2")),
        new ASTNode(Token.Number("0.5"))
      });

      simplifier.FormatTree(tree2);
      Assert.True(tree2 == expected2);
    }

    [Fact]
    public void FormatTree_ConvertAllDecimalsToFractions()
    {
      var simplifier = new Simplifier(false, false, true);

      var tree = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Number("0.5")),            // → 1/2
        new ASTNode(Token.Number("0.3333333333")),   // → ~1/3
        new ASTNode(Token.Number("0.6666666667")),   // → ~2/3
        new ASTNode(Token.Number("1.25")),           // → 5/4
        new ASTNode(Token.Number("-0.75")),          // → -3/4
        new ASTNode(Token.Number("2.2")),            // → 11/5
        new ASTNode(Token.Number("0.142857")),       // → ~1/7
        new ASTNode(Token.Number("3.14159265")),     // → ~355/113 (π approx)
        new ASTNode(Token.Number("2.718281828")),    // → ~193/71 (e approx)
        new ASTNode(Token.Number("1234.56789")),     // large decimal
        new ASTNode(Token.Number("-0.0009765625"))   // → -1/1024
      });

      var expected = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        // 0.5 = 1/2
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        }),
        // 0.3333333333 ≈ 1/3
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("3"))
        }),
        // 0.6666666667 ≈ 2/3
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Integer("3"))
        }),
        // 1.25 = 5/4
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("5")),
          new ASTNode(Token.Integer("4"))
        }),
        // -0.75 = -3/4
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("-3")),
          new ASTNode(Token.Integer("4"))
        }),
        // 2.2 = 11/5
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("11")),
          new ASTNode(Token.Integer("5"))
        }),
        // 0.142857 ≈ 1/7
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("7"))
        }),
        // π ≈ 355/113
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("355")),
          new ASTNode(Token.Integer("113"))
        }),
        // e ≈ 193/71
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("2721")),
          new ASTNode(Token.Integer("1001"))
        }),
        // 1234.56789 ≈ 123456789 / 100000
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("1345679")),
          new ASTNode(Token.Integer("1090"))
        }),
        // -0.0009765625 = -1/1024
        new ASTNode(Token.Fraction(), new()
        {
          new ASTNode(Token.Integer("-1")),
          new ASTNode(Token.Integer("1023"))
        })
      });

      simplifier.FormatTree(tree);
      Assert.True(tree == expected);
    }

    [Fact]
    public void PostFormatTree_Power()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));

      // x^(-1) => 1 / x
      var power1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("-1"))
      });

      var expected1 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        x
      });

      simplifier.PostFormatTree(power1);
      Assert.True(power1 == expected1);

      // x^(-2) => 1 / (x^2)
      var power2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("-2"))
      });

      var expected2 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        })
      });

      simplifier.PostFormatTree(power2);
      Assert.True(power2 == expected2);

      // x^(-2/3) => 1 / (x^(2/3))
      var power3 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("-2")),
          new ASTNode(Token.Integer("3"))
        })
      });

      var expected3 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer("2")),
            new ASTNode(Token.Integer("3"))
          })
        })
      });

      simplifier.PostFormatTree(power3);
      Assert.True(power3 == expected3);

      // x^(-3y) => 1 / (x^(3 * y))
      var power4 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("-3")),
          y
        })
      });

      var expected4 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("3")),
            y
          })
        })
      });

      simplifier.PostFormatTree(power4);
      Assert.True(power4 == expected4);
    }

    [Fact]
    public void PostFormatTree_Multiplication()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));
      var z = new ASTNode(Token.Variable("z"));
      var v = new ASTNode(Token.Variable("v"));
      var w = new ASTNode(Token.Variable("w"));

      // x * y^(-1) => x / y
      var mul1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          y,
          new ASTNode(Token.Integer("-1"))
        })
      });

      var expected1 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        x,
        y
      });

      simplifier.PostFormatTree(mul1);
      Assert.True(mul1 == expected1);

      // x^(-1) * y^(-1) => 1 / (x * y)
      var mul2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("-1"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          y,
          new ASTNode(Token.Integer("-1"))
        })
      });

      var expected2 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          x,
          y
        })
      });

      simplifier.PostFormatTree(mul2);
      Assert.True(mul2 == expected2);

      // x * y * z => x * y * z (unchanged)
      var mul3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        x,
        y,
        z
      });

      var expected3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        x,
        y,
        z
      });

      simplifier.PostFormatTree(mul3);
      Assert.True(mul3 == expected3);

      // x^(-1) * y * z => (y * z) / x
      var mul4 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("-1"))
        }),
        y,
        z
      });

      var expected4 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          y,
          z
        }),
        x
      });

      simplifier.PostFormatTree(mul4);
      Assert.True(mul4 == expected4);

      // x * y^(-2) * z^(-3w) * v => (x * v) / (y^2 * z^(3w))
      var mul5 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          y,
          new ASTNode(Token.Integer("-2"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          z,
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("-3")),
            w
          })
        }),
        v
      });

      var expected5 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          x,
          v
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            y,
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            z,
            new ASTNode(Token.Operator("*"), new List<ASTNode>
            {
              new ASTNode(Token.Integer("3")),
              w
            })
          })
        })
      });

      simplifier.PostFormatTree(mul5);
      Assert.True(mul5 == expected5);
    }

    [Fact]
    public void PostFormatTree_FullEquations()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));

      // Case 1: (x + 2)^(-2) => 1 / (x + 2)^2
      var expr1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Integer("-2"))
      });

      var expected1 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Integer("2"))
        })
      });

      simplifier.PostFormatTree(expr1);
      Assert.True(expr1 == expected1);

      // Case 2: x^2 + x^(-1) => x^2 + 1/x
      var expr2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("-1"))
        })
      });

      var expected2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          x
        })
      });

      simplifier.PostFormatTree(expr2);
      Assert.True(expr2 == expected2);

      // Case 3: (x + 3)^(-(x+4)) => 1 / (x + 3)^(x+4)
      var expr3 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("3"))
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("-1")),
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("4"))
          })
        })
      });

      // The exponent simplifies to (x + 4) with negative sign
      var expected3 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("3"))
          }),
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("4"))
          })
        })
      });

      simplifier.PostFormatTree(expr3);
      Assert.True(expr3 == expected3);

      // Case 4: x^(-1) * y^2 * (x + 2)^(-3)
      var expr4 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("-1"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          y,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Integer("-3"))
        })
      });

      var expected4 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          y,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("+"), new List<ASTNode>
            {
              x,
              new ASTNode(Token.Integer("2"))
            }),
            new ASTNode(Token.Integer("3"))
          })
        })
      });

      simplifier.PostFormatTree(expr4);
      Assert.True(expr4 == expected4);

      // Case 5: x^3 + y^(-1) + (x + 1)^(-2)
      var expr5 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("3"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          y,
          new ASTNode(Token.Integer("-1"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("1"))
          }),
          new ASTNode(Token.Integer("-2"))
        })
      });

      var expected5 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("3"))
        }),
        new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          y
        }),
        new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("+"), new List<ASTNode>
            {
              x,
              new ASTNode(Token.Integer("1"))
            }),
            new ASTNode(Token.Integer("2"))
          })
        })
      });

      simplifier.PostFormatTree(expr5);
      Assert.True(expr5 == expected5);
    }


    [Fact]
    public void PostFormatTree_ConvertRationalExponent()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));

      // x^(1/2) => nroot(x, 2)
      var power1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var expected1 = new ASTNode(Token.Function("nroot"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("2"))
      });

      simplifier.PostFormatTree(power1);
      Assert.True(power1 == expected1);

      // x^(2/5) => x^(2/5) (no change)
      var power2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Integer("5"))
        })
      });

      var expected2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Integer("5"))
        })
      });

      simplifier.PostFormatTree(power2);
      Assert.True(power2 == expected2);

      // (3x + 5 + 24*(x^2+1))^(1/5) => nroot(3x + 5 + 24*(x^2+1), 5)
      var polyInner = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("3")),
          x
        }),
        new ASTNode(Token.Integer("5")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("24")),
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("^"), new List<ASTNode>
            {
              x,
              new ASTNode(Token.Integer("2"))
            }),
            new ASTNode(Token.Integer("1"))
          })
        })
      });

      var power3 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        polyInner,
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("5"))
        })
      });

      var expected3 = new ASTNode(Token.Function("nroot"), new List<ASTNode>
      {
        polyInner,
        new ASTNode(Token.Integer("5"))
      });

      simplifier.PostFormatTree(power3);
      Assert.True(power3 == expected3);

      // (x + y)^(-1/2) => 1 / nroot((x + y), 2))
      var innerSum = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        x,
        y
      });

      var power4 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        innerSum,
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("-1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var expected4 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Function("nroot"), new List<ASTNode>
        {
          new ASTNode(innerSum),
          new ASTNode(Token.Integer("2"))
        })
      });

      simplifier.PostFormatTree(power4);
      Assert.True(power4 == expected4);
    }

    #endregion

    #region RNE Simplify

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

    #endregion

    #region Automatic Simplify

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

      // ((2 * x + 3 * x) + (x^2 + x^2)) => x(5 + 2x^2)
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
      var expected1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Integer("5")),
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Integer("2")),
            new ASTNode(Token.Variable("x"))
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
      // Expected: 3x(4 + x)
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

      var expected1 = new ASTNode(Token.Operator("*"), new List<ASTNode> { 
        new ASTNode(Token.Integer("3")),
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Integer("4")),
          new ASTNode(Token.Variable("x")),
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

    [Fact]
    public void AutomaticSimplify_RootSimplify()
    {
      var simplifier = new Simplifier();

      var power1 = new ASTNode(Token.Operator("^"), new List<ASTNode> {
        new ASTNode(Token.Integer("4")), 
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });
      var result1 = new ASTNode(Token.Integer("2"));

      Assert.True(simplifier.AutomaticSimplify(power1) == result1);

      var power2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("27")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("3"))
        })
      });
      var result2 = new ASTNode(Token.Integer("3"));
      Assert.True(simplifier.AutomaticSimplify(power2) == result2);

      var power3 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("144")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });
      var result3 = new ASTNode(Token.Integer("12"));
      Assert.True(simplifier.AutomaticSimplify(power3) == result3);

      var power4 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("7776")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("5"))
        })
      });
      var result4 = new ASTNode(Token.Integer("6"));
      Assert.True(simplifier.AutomaticSimplify(power4) == result4);

      var power5 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("7")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });
      var result5 = new ASTNode(power5);
      Assert.True(simplifier.AutomaticSimplify(power5) == result5);

      var power6 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("8")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var factor1 = new ASTNode(Token.Integer("2"));
      var radicalPart1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var result6 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        factor1,
        radicalPart1
      });

      Assert.True(simplifier.AutomaticSimplify(power6) == result6);

      // 162^(1/2) = 9 * 2^(1/2)
      var power7 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("162")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var factor2 = new ASTNode(Token.Integer("9"));
      var radicalPart2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        })
      });

      var result7 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        factor2,
        radicalPart2
      });

      Assert.True(simplifier.AutomaticSimplify(power7) == result7);
    }

    [Fact]
    public void AutomaticSimplify_WithDecimalNumbers()
    {
      var simplifier = new Simplifier();

      // Case 1: 2.0 + 3.0 => 5.0
      var expr1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Number("2.0")),
        new ASTNode(Token.Number("3.0"))
      });
      var expected1 = new ASTNode(Token.Number("5"));
      Assert.True(simplifier.AutomaticSimplify(expr1) == expected1);

      // Case 2: 6.0 * 2.5 => 15.0
      var expr2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Number("6.0")),
        new ASTNode(Token.Number("2.5"))
      });
      var expected2 = new ASTNode(Token.Number("15"));
      Assert.True(simplifier.AutomaticSimplify(expr2) == expected2);

      // Case 3: (4.0 + 1.0) * 2.0 => 10.0
      var expr3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          new ASTNode(Token.Number("4.0")),
          new ASTNode(Token.Number("1.0"))
        }),
        new ASTNode(Token.Number("2.0"))
      });
      var expected3 = new ASTNode(Token.Number("10"));
      Assert.True(simplifier.AutomaticSimplify(expr3) == expected3);

      // Case 4: 3.5 - 1.5 => 2.0
      var expr4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Number("3.5")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Number("-1.0")),
          new ASTNode(Token.Number("1.5"))
        })
      });
      var expected4 = new ASTNode(Token.Number("2"));
      Assert.True(simplifier.AutomaticSimplify(expr4) == expected4);

      // Case 5: (2.0^0.37867) * x => 4.0 * x
      var x = new ASTNode(Token.Variable("x"));
      var expr5 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Number("2.0")),
          new ASTNode(Token.Number("0.37867"))
        }),
        x
      });
      var expected5 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Number("1.3001427197473157")),
        x
      });
      Assert.True(simplifier.AutomaticSimplify(expr5) == expected5);
    }

    [Fact]
    public void AutomaticSimplify_SimplifyFunctions()
    {
      var simplifier = new Simplifier(true, false); // SIMPLIFIER_EVAL_FUNCTIONS = true, USE_RADIANS = false

      // sin(30) => 1/2
      var sin30 = new ASTNode(Token.Function("sin"), new()
      {
        new ASTNode(Token.Number("30.0"))
      });
      var expectedSin30 = new ASTNode(Token.Fraction(), new()
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Integer("2"))
      });
      Assert.True(simplifier.AutomaticSimplify(sin30) == expectedSin30);

      // cos(60) => 1/2
      var cos60 = new ASTNode(Token.Function("cos"), new()
      {
        new ASTNode(Token.Number("60.0"))
      });
      var expectedCos60 = new ASTNode(Token.Fraction(), new()
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Integer("2"))
      });
      Assert.True(simplifier.AutomaticSimplify(cos60) == expectedCos60);

      // tan(90) => Undefined
      var tan90 = new ASTNode(Token.Function("tan"), new()
      {
        new ASTNode(Token.Number("90.0"))
      });
      Assert.True(simplifier.AutomaticSimplify(tan90).Token.Type is Undefined);

      // log(1000) => 3
      var log1000 = new ASTNode(Token.Function("log"), new()
      {
        new ASTNode(Token.Number("1000.0"))
      });
      var expectedLog = new ASTNode(Token.Integer("3"));
      Assert.True(simplifier.AutomaticSimplify(log1000) == expectedLog);

      // ln(e=2.718281828) => 1 approx
      var lnE = new ASTNode(Token.Function("ln"), new()
      {
        new ASTNode(Token.Number("2.718281828"))
      });
      var expectedLn = new ASTNode(Token.Integer("1")); // approximate to 1
      Assert.True(simplifier.AutomaticSimplify(lnE) == expectedLn);

      // min(3, 2) => 2 via Calculator.Evaluate
      var minNode = new ASTNode(Token.Function("min"), new()
      {
        new ASTNode(Token.Number("3.0")),
        new ASTNode(Token.Number("2.0"))
      });
      var expectedMin = new ASTNode(Token.Number("2")); // Calculator should return 2
      Assert.True(simplifier.AutomaticSimplify(minNode) == expectedMin);
    }

    [Fact]
    public void AutomaticSimplify_EveryConcepts()
    {
      var simplifier = new Simplifier(true, false); // Eval functions, degrees

      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));

      // === Equation 1 ===
      // (sin(30) + log(1000)) * (x^0 + tan(45))^1  =>  (1/2 + 3) * (1 + 1) => 3.5 * 2 => 7
      var expr1 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Function("sin"), new() { new ASTNode(Token.Number("30")) }),
          new ASTNode(Token.Function("log"), new() { new ASTNode(Token.Number("1000")) })
        }),
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Operator("+"), new()
          {
            new ASTNode(Token.Operator("^"), new()
            {
              x,
              new ASTNode(Token.Integer("0"))
            }),
            new ASTNode(Token.Function("tan"), new() { new ASTNode(Token.Number("45")) })
          }),
          new ASTNode(Token.Integer("1"))
        })
      });

      var expected1 = new ASTNode(Token.Integer("7"));
      Assert.True(simplifier.AutomaticSimplify(expr1) == expected1);


      // === Equation 2 ===
      // ((x^2)^1/2 * cos(60) + ln(2.718281828)) / min(3, 2) => x * 1/2 + 1 => (x/2 + 1) / 2
      var expr2 = new ASTNode(Token.Operator("/"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Operator("^"), new()
            {
              new ASTNode(Token.Operator("^"), new()
              {
                x,
                new ASTNode(Token.Integer("2"))
              }),
              new ASTNode(Token.Fraction(), new()
              {
                new ASTNode(Token.Integer("1")),
                new ASTNode(Token.Integer("2"))
              })
            }),
            new ASTNode(Token.Function("cos"), new() { new ASTNode(Token.Number("60")) })
          }),
          new ASTNode(Token.Function("ln"), new() { new ASTNode(Token.Number("2.718281828")) })
        }),
        new ASTNode(Token.Function("min"), new()
        {
          new ASTNode(Token.Number("3")),
          new ASTNode(Token.Number("2"))
        })
      });

      var expected2 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Operator("*"), new()
          {
            new ASTNode(Token.Fraction(), new()
            {
              new ASTNode(Token.Integer("1")),
              new ASTNode(Token.Integer("2"))
            }),
            x
          }),
          new ASTNode(Token.Integer("1"))
        }),
        new ASTNode(Token.Number("0.5"))
      });

      var res = simplifier.AutomaticSimplify(expr2);
      Assert.True(simplifier.AutomaticSimplify(expr2) == expected2);
    }

    #endregion

    #region Expand

    [Fact]
    public void Expand_SimplifyProducts()
    {
      var simplifier = new Simplifier();

      var x = new ASTNode(Token.Variable("x"));

      // Case 1: 2 * (x + 3) => 2x + 6
      var expr1 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Operator("+"), new()
        {
          x,
          new ASTNode(Token.Integer("3"))
        })
      });

      var expected1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Integer("6")),
        new ASTNode(Token.Operator("*"), new() { new ASTNode(Token.Integer("2")), x })
      });

      Assert.True(simplifier.Expand(expr1) == expected1);

      // Case 2: (x + 1)(x - 1) => x^2 - 1
      var expr2 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          x,
          new ASTNode(Token.Integer("1"))
        }),
        new ASTNode(Token.Operator("+"), new()
        {
          x,
          new ASTNode(Token.Integer("-1"))
        })
      });

      var expected2 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("^"), new() { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Integer("-1"))
      });

      var res = simplifier.Expand(expr2);
      Assert.True(simplifier.Expand(expr2) == expected2);
    }

    [Fact]
    public void Expand_SimplifyPowers()
    {
      var simplifier = new Simplifier();

      var x = new ASTNode(Token.Variable("x"));

      // Case 1: (x + 1)^2 => x^2 + 2x + 1
      var expr1 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          x,
          new ASTNode(Token.Integer("1"))
        }),
        new ASTNode(Token.Integer("2"))
      });

      var expected1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("^"), new() { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new() { new ASTNode(Token.Integer("2")), x }),
        new ASTNode(Token.Integer("1"))
      });

      Assert.True(simplifier.Expand(expr1) == expected1);

      // Case 2: (x + 1)^5 => x^5 + 5x^4 + 10x^3 + 10x^2 + 5x + 1
      var expr2 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          x,
          new ASTNode(Token.Integer("1"))
        }),
        new ASTNode(Token.Integer("5"))
      });

      var expected2 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("^"), new() { x, new ASTNode(Token.Integer("5")) }),
        new ASTNode(Token.Operator("*"), new() { new ASTNode(Token.Integer("5")), new ASTNode(Token.Operator("^"), new() { x, new ASTNode(Token.Integer("4")) }) }),
        new ASTNode(Token.Operator("*"), new() { new ASTNode(Token.Integer("10")), new ASTNode(Token.Operator("^"), new() { x, new ASTNode(Token.Integer("3")) }) }),
        new ASTNode(Token.Operator("*"), new() { new ASTNode(Token.Integer("10")), new ASTNode(Token.Operator("^"), new() { x, new ASTNode(Token.Integer("2")) }) }),
        new ASTNode(Token.Operator("*"), new() { new ASTNode(Token.Integer("5")), x }),
        new ASTNode(Token.Integer("1"))
      });

      Assert.True(simplifier.Expand(expr2) == expected2);
    }

    #endregion

    #region Polynomials

    [Fact]
    public void PolynomialDivision_BasicCases()
    {
      var simplifier = new Simplifier();

      var x = new ASTNode(Token.Variable("x"));

      // Example 1: (x^3 + 2x^2 + 4) / (x + 1)
      var numerator = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("3")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("2")), new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }) }),
        new ASTNode(Token.Integer("4"))
      });

      var denominator = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("1"))
      });

      var result = simplifier.PolynomialDivision(numerator, denominator, x);
      var quotient = result[0];
      var remainder = result[1];

      // Expected quotient: x^2 + x - 1
      var expectedQuotient = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        x,
        new ASTNode(Token.Integer("-1"))
      });

      // Expected remainder: 5
      var expectedRemainder = new ASTNode(Token.Integer("5"));

      Assert.True(simplifier.AutomaticSimplify(quotient) == simplifier.AutomaticSimplify(expectedQuotient));
      Assert.True(simplifier.AutomaticSimplify(remainder) == expectedRemainder);

      // Example 2: (x^2 + 3x + 2) / (x + 1)
      var num2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("3")), x }),
        new ASTNode(Token.Integer("2"))
      });

      var den2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("1"))
      });

      var result2 = simplifier.PolynomialDivision(num2, den2, x);
      var q2 = result2[0];
      var r2 = result2[1];

      // Expected quotient: x + 2
      var expectedQ2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("2"))
      });

      // Expected remainder: 0
      var expectedR2 = new ASTNode(Token.Integer("0"));

      Assert.True(simplifier.AutomaticSimplify(q2) == simplifier.AutomaticSimplify(expectedQ2));
      Assert.True(simplifier.AutomaticSimplify(r2) == expectedR2);

      // Numerator: x^2 + 1
      var numerator3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Integer("1"))
      });

      // Denominator: x^5 + x + 1 (degree higher than numerator)
      var denominator3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("5")) }),
        x,
        new ASTNode(Token.Integer("1"))
      });

      var result3 = simplifier.PolynomialDivision(numerator3, denominator3, x);
      var q = result3[0];
      var r = result3[1];

      // Expected quotient: 0
      var expectedQ = new ASTNode(Token.Integer("0"));

      // Expected remainder: numerator itself
      var expectedR = numerator3;

      Assert.True(q == expectedQ);
      Assert.True(r == expectedR);
    }

    [Fact]
    public void PolynomialDivision_ComplexCases()
    {
      var simplifier = new Simplifier();

      var x = new ASTNode(Token.Variable("x"));

      // Example 1: (2x^4 + 3x^3 + x + 5) / (x^2 + 1)
      var numerator1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("2")), new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("4")) }) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("3")), new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("3")) }) }),
        x,
        new ASTNode(Token.Integer("5"))
      });

      var denominator1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Integer("1"))
      });

      var result1 = simplifier.PolynomialDivision(numerator1, denominator1, x);
      var q1 = result1[0];
      var r1 = result1[1];

      // Expected quotient: 2x^2 + 3x - 2
      var expectedQ1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("2")), new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("3")), x }),
        new ASTNode(Token.Integer("-2"))
      });

      // Expected remainder: -2x + 7
      var expectedR1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("-2")), x }),
        new ASTNode(Token.Integer("7"))
      });

      Assert.True(simplifier.AutomaticSimplify(q1) == simplifier.AutomaticSimplify(expectedQ1));
      Assert.True(simplifier.AutomaticSimplify(r1) == simplifier.AutomaticSimplify(expectedR1));


      // Example 2: (x^5 - x^4 + x^2 - x + 1) / (x^2 + 1)
      var numerator2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("5")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("-1")), new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("4")) }) }),
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("-1")), x }),
        new ASTNode(Token.Integer("1"))
      });

      var denominator2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Integer("1"))
      });

      var result2 = simplifier.PolynomialDivision(numerator2, denominator2, x);
      var q2 = result2[0];
      var r2 = result2[1];

      // Expected quotient: x^3 - x^2 - x + 2
      var expectedQ2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("3")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("-1")), new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("-1")), x }),
        new ASTNode(Token.Integer("2"))
      });

      // Expected remainder: -1
      var expectedR2 = new ASTNode(Token.Integer("-1"));

      Assert.True(simplifier.AutomaticSimplify(q2) == simplifier.AutomaticSimplify(expectedQ2));
      Assert.True(simplifier.AutomaticSimplify(r2) == simplifier.AutomaticSimplify(expectedR2));
    }

    [Fact]
    public void PolynomialExpansion()
    {
      var simplifier = new Simplifier();

      var x = new ASTNode(Token.Variable("x"));
      var t = new ASTNode(Token.Variable("t")); // used for expansion symbol

      // u = x^5 + 11x^4 + 51x^3 + 124x^2 + 159x + 86
      var u = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("5")) }),

        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("11")),
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("4")) })
        }),

        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("51")),
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("3")) })
        }),

        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("124")),
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) })
        }),

        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("159")),
          x
        }),

        new ASTNode(Token.Integer("86"))
      });

      // v = x^2 + 4x + 5
      var v = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("4")), x }),
        new ASTNode(Token.Integer("5"))
      });

      var expansion = simplifier.PolynomialExpansion(u, v, x, t);

      // Expected: (x + 3) * v^2 + (x + 2) * v + (x + 1)
      var vSquared = new ASTNode(Token.Operator("^"), new List<ASTNode> { v, new ASTNode(Token.Integer("2")) });

      var term1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("3")) }),
        new ASTNode(Token.Operator("^"), new List<ASTNode> { t, new ASTNode(Token.Integer("2")) })
      });

      var term2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        t
      });

      var term3 = new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("1")) });

      var expected = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        term1,
        term2,
        term3
      });

      Assert.True(expansion == simplifier.Expand(expected));
    }

    [Fact]
    public void PolynomialFactorization_CommonFactors()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));

      // 1️⃣ 5 + 15x
      var poly1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("5")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("15")),
          x
        })
      });

      var expectedCommon1 = new ASTNode(Token.Integer("5"));
      var expectedRest1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("3")),
          x
        })
      });
      var expected1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        expectedCommon1,
        expectedRest1
      });

      var factorized1 = simplifier.PolynomialFactorization(poly1, x);
      Assert.True(factorized1 == expected1);

      // 2️⃣ 3x^6
      var poly2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("3")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("6"))
        })
      });

      var expected2 = poly2;
      var factorized2 = simplifier.PolynomialFactorization(poly2, x);
      Assert.True(factorized2 == expected2);

      // 3️⃣ 2x + 4x^2 + 8x^4
      var poly3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("2")),
          x
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("4")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("2"))
          })
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("8")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("4"))
          })
        })
      });

      var expectedCommon3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        x
      });
      var expectedRest3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("2")),
          x
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("4")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("3"))
          })
        })
      });
      var expected3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        expectedCommon3,
        expectedRest3
      });
      expected3 = simplifier.AutomaticSimplify(expected3);

      var factorized3 = simplifier.PolynomialFactorization(poly3, x);
      Assert.True(factorized3 == expected3);

      // 4️⃣ 1/2 * x^3 + x^4 + x^6
      var poly4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")),
            new ASTNode(Token.Integer("2"))
          }),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("3"))
          })
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("4"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("6"))
        })
      });

      var expectedCommon4 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("3"))
      });
      var expectedRest4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Integer("2"))
        }),
        x,
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("3"))
        })
      });
      var expected4 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        expectedCommon4,
        expectedRest4
      });

      var factorized4 = simplifier.PolynomialFactorization(poly4, x);
      Assert.True(factorized4 == expected4);
    }

    [Fact]
    public void PolynomialFactorization_QuadraticCases()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));

      // 1️⃣ x^2 + 2x + 1 = (x + 1)^2
      var poly1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("2")), x }),
        new ASTNode(Token.Integer("1"))
      });

      var expected1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("1"))
        }),
        new ASTNode(Token.Integer("2"))
      });

      var factorized1 = simplifier.PolynomialFactorization(poly1, x);
      Assert.True(factorized1 == expected1);

      // 2️⃣ x^2 + 6x + 8 = (x + 2)(x + 4)
      var poly2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("6")), x }),
        new ASTNode(Token.Integer("8"))
      });

      var expected2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("4"))
        })
      });

      var factorized2 = simplifier.PolynomialFactorization(poly2, x);
      Assert.True(factorized2 == expected2);

      // 3️⃣ x^2 + 1 = x^2 + 1 (irreducible over reals)
      var poly3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Integer("1"))
      });

      var expected3 = poly3;
      var factorized3 = simplifier.PolynomialFactorization(poly3, x);
      Assert.True(factorized3 == expected3);

      // 4️⃣ x^2 - 1 = (x + 1)(x - 1)
      var poly4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("-1")), new ASTNode(Token.Integer("1")) })
      });

      var expected4 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("1"))
        }),
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("-1"))
        })
      });

      var factorized4 = simplifier.PolynomialFactorization(poly4, x);
      Assert.True(factorized4 == expected4);

      // 5️⃣ x^2 - 2 = (x + sqrt(2))(x - sqrt(2))
      var poly5 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("-1")), new ASTNode(Token.Integer("2")) })
      });

      var sqrt2 = new ASTNode(Token.Operator("^"), new List<ASTNode> 
      { 
        new ASTNode(Token.Integer("2")), new ASTNode(Token.Fraction(), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2"))
        }) 
      });

      var expected5 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, sqrt2 }),
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("-1")), sqrt2
        }) })
      });

      var factorized5 = simplifier.PolynomialFactorization(poly5, x);
      Assert.True(factorized5 == expected5);

      var poly6 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("2"))
      });

      var expected6 = new ASTNode(poly6);

      var factorized6 = simplifier.PolynomialFactorization(poly6, x);
      Assert.True(factorized6 == expected6);
    }

    [Fact]
    public void PolynomialSimplification_FactorExamples()
    {
      var simplifier = new Simplifier();
      var x = new ASTNode(Token.Variable("x"));

      // x^2 + 2x + 1 => (x + 1)^2
      var poly1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("2")), x }),
        new ASTNode(Token.Integer("1"))
      });

      var expected1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("1")) }),
        new ASTNode(Token.Integer("2"))
      });

      Assert.True(simplifier.PolynomialSimplify(poly1, x) == expected1);

      // (x + 3)(x^2 + 6x + 8) => (x + 2)(x + 3)(x + 4)
      var poly2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("3"))
        }),
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
          new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("6")), x }),
          new ASTNode(Token.Integer("8"))
        })
      });

      var expected2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("3")) }),
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("4")) })
      });

      Assert.True(simplifier.PolynomialSimplify(poly2, x) == expected2);

      // (x + 2)(x^2 + 6x + 8) => (x + 2)^2 * (x + 4)
      var poly3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
          new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(Token.Integer("6")), x }),
          new ASTNode(Token.Integer("8"))
        })
      });

      var expected3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("4")) })
      });

      Assert.True(simplifier.PolynomialSimplify(poly3, x) == expected3);

      // 4 + 5x^5 + 3x * (2x^3 - 2) => 4 + 5x^5 + 6x^2 * (x + 1)(x - 1)
      var poly4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("4")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("5")),
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("5")) })
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("3")),
          x,
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("*"), new List<ASTNode>
            {
              new ASTNode(Token.Integer("2")),
              new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("3")) })
            }),
            new ASTNode(Token.Operator("*"), new List<ASTNode>
            {
              new ASTNode(Token.Integer("-2")),
              x
            })
          })
        })
      });

      var expected4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("4")),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("5")),
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("5")) })
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("6")),
          new ASTNode(Token.Operator("^"), new List<ASTNode> { x, new ASTNode(Token.Integer("2")) }),
          new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("1")) }),
          new ASTNode(Token.Operator("+"), new List<ASTNode> { x, new ASTNode(Token.Integer("-1")) })
        })
      });

      Assert.True(simplifier.PolynomialSimplify(poly4, x) == expected4);
    }

    #endregion
  }
}
