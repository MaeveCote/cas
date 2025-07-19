using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAS.Core;
using CAS.Core.EquationParsing;

namespace CAS.UT
{
  public class DifferentiaterTest
  {
    [Fact]
    public void ConstantRule()
    {
      var simplifier = new Simplifier();
      var differentiater = new Differentiater(simplifier);
      var x = new ASTNode(Token.Variable("x"));

      // 4 => 0
      var expr1 = new ASTNode(Token.Integer("4"));
      var expected1 = new ASTNode(Token.Integer("0"));
      var result1 = differentiater.Differentiate(expr1, x);
      Assert.True(result1 == expected1);

      // 1/3 => 0
      var expr2 = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Integer("3"))
      });
      var expected2 = new ASTNode(Token.Integer("0"));
      var result2 = differentiater.Differentiate(expr2, x);
      Assert.True(result2 == expected2);

      // 2.3456 => 0
      var expr3 = new ASTNode(Token.Number("2.3456"));
      var expected3 = new ASTNode(Token.Integer("0"));
      var result3 = differentiater.Differentiate(expr3, x);
      Assert.True(result3 == expected3);

      // a => 0
      var expr4 = new ASTNode(Token.Variable("a"));
      var expected4 = new ASTNode(Token.Integer("0"));
      var result4 = differentiater.Differentiate(expr4, x);
      Assert.True(result4 == expected4);

      // x => 1
      var expr5 = new ASTNode(Token.Variable("x"));
      var expected5 = new ASTNode(Token.Integer("1"));
      var result5 = differentiater.Differentiate(expr5, x);
      Assert.True(result5 == expected5);    
    }

    [Fact]
    public void PowerRule()
    {
      var simplifier = new Simplifier();
      var differentiater = new Differentiater(simplifier);
      var x = new ASTNode(Token.Variable("x"));

      // x^3 => 3 * x^2
      var expr1 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("3"))
      });
      var expected1 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Integer("3")),
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("2"))
        })
      });
      var result1 = differentiater.Differentiate(expr1, x);
      Assert.True(result1 == expected1);

      // x^-1.2345 => -1.2345 * x^-2.2345
      var expr2 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Number("-1.2345"))
      });
      var expected2 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Number("-1.2345")),
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Number("-2.2345"))
        })
      });
      var result2 = differentiater.Differentiate(expr2, x);
      Assert.True(result2 == expected2);

      // x^1 => 1
      var expr3 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("1"))
      });
      var expected3 = new ASTNode(Token.Integer("1"));
      var result3 = differentiater.Differentiate(expr3, x);
      Assert.True(result3 == expected3);

      // a^x => ln(a) * (a^x)
      var expr4 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("a")),
        new ASTNode(Token.Variable("x"))
      });
      var expected4 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Function("ln"), new()
        {
          new ASTNode(Token.Variable("a"))
        }),
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("a")),
          new ASTNode(Token.Variable("x"))
        })
      });
      var result4 = differentiater.Differentiate(expr4, x);
      Assert.True(result4 == expected4);

      // x^x => (x^x) * (ln(x) + 1)
      var expr5 = new ASTNode(Token.Operator("^"), new()
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Variable("x"))
      });
      var expected5 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Variable("x"))
        }),
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Function("ln"), new()
          {
            new ASTNode(Token.Variable("x"))
          }),
          new ASTNode(Token.Integer("1"))
        })
      });
      var result5 = differentiater.Differentiate(expr5, x);
      Assert.True(result5 == expected5);
    }

    [Fact]
    public void SumDifferenceRule()
    {
      var simplifier = new Simplifier();
      var differentiater = new Differentiater(simplifier);
      var x = new ASTNode(Token.Variable("x"));

      // +(x^2) => 2 * x
      var expr1 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("2"))
        })
      });
      var expected1 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Variable("x"))
      });
      var result1 = differentiater.Differentiate(expr1, x);
      Assert.True(result1 == expected1);

      // -(x^2) => -2 * x
      var expr2 = new ASTNode(Token.Operator("-"), new()
      {
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("2"))
        })
      });
      var expected2 = new ASTNode(Token.Operator("*"), new()
      {
        new ASTNode(Token.Integer("-2")),
        new ASTNode(Token.Variable("x"))
      });
      var result2 = differentiater.Differentiate(expr2, x);
      Assert.True(result2 == expected2);

      // 1 + x + x^3 => 1 + 3 * x^2
      var expr3 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Operator("^"), new()
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Integer("3"))
        })
      });
      var expected3 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Integer("1")),
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
      var result3 = differentiater.Differentiate(expr3, x);
      Assert.True(result3 == expected3);

      // (1 + x^2) - (a + x) => -1 + 2 * x
      var expr4 = new ASTNode(Token.Operator("-"), new()
      {
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Operator("^"), new()
          {
            new ASTNode(Token.Variable("x")),
            new ASTNode(Token.Integer("2"))
          })
        }),
        new ASTNode(Token.Operator("+"), new()
        {
          new ASTNode(Token.Variable("a")),
          new ASTNode(Token.Variable("x"))
        })
      });
      var expected4 = new ASTNode(Token.Operator("+"), new()
      {
        new ASTNode(Token.Integer("-1")),
        new ASTNode(Token.Operator("*"), new()
        {
          new ASTNode(Token.Integer("2")),
          new ASTNode(Token.Variable("x"))
        })
      });
      var result4 = differentiater.Differentiate(expr4, x);
      Assert.True(result4 == expected4);
    }

    [Fact]
    public void FunctionRule()
    {
      var simplifier = new Simplifier();
      var differentiater = new Differentiater(simplifier);
      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));

      ASTNode f = new ASTNode(Token.Function("f"), new List<ASTNode> { x });
      ASTNode a = new ASTNode(Token.Variable("a"));

      // f(x) => f'(x) generic fallback
      var expectedF = new ASTNode(Token.Function("f'"), new List<ASTNode> { x });
      var resultF = differentiater.Differentiate(f, x);
      simplifier.PostFormatTree(resultF);
      simplifier.PostFormatTree(expectedF);
      Assert.True(resultF == expectedF);

      // potato(x, y) => potato'(x, y)
      var potato = new ASTNode(Token.Function("potato"), new List<ASTNode> { x, y });
      var expectedPotato = new ASTNode(Token.Function("potato'"), new List<ASTNode> { x, y });
      var resultPotato = differentiater.Differentiate(potato, x);
      simplifier.PostFormatTree(resultPotato);
      simplifier.PostFormatTree(expectedPotato);
      Assert.True(resultPotato == expectedPotato);

      // ln(f(x)) => f'(x) / f(x)
      var ln = new ASTNode(Token.Function("ln"), new List<ASTNode> { f });
      var expectedLn = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        differentiater.Differentiate(f, x, false),
        new ASTNode(f)
      });
      var resultLn = differentiater.Differentiate(ln, x);
      simplifier.PostFormatTree(resultLn);
      simplifier.PostFormatTree(expectedLn);
      Assert.True(resultLn == expectedLn);

      // log(f(x), a) => f'(x) / (f(x) * ln(a))
      var log = new ASTNode(Token.Function("log"), new List<ASTNode> { f, a });
      var expectedLog = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        differentiater.Differentiate(f, x, false),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(f),
          new ASTNode(Token.Function("ln"), new List<ASTNode> { new ASTNode(a) })
        })
      });
      var resultLog = differentiater.Differentiate(log, x);
      simplifier.PostFormatTree(resultLog);
      simplifier.PostFormatTree(expectedLog);
      Assert.True(resultLog == expectedLog);

      // sin(f(x)) => cos(f(x)) * f'(x)
      var sin = new ASTNode(Token.Function("sin"), new List<ASTNode> { f });
      var expectedSin = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(f) }),
        differentiater.Differentiate(f, x, false)
      });
      var resultSin = differentiater.Differentiate(sin, x);
      simplifier.PostFormatTree(resultSin);
      simplifier.PostFormatTree(expectedSin);
      Assert.True(resultSin == expectedSin);

      // cos(f(x)) => (-1) * sin(f(x)) * f'(x)
      var cos = new ASTNode(Token.Function("cos"), new List<ASTNode> { f });
      var expectedCos = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("-1")),
        new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(f) }),
        differentiater.Differentiate(f, x, false)
      });
      var resultCos = differentiater.Differentiate(cos, x);
      simplifier.PostFormatTree(resultCos);
      simplifier.PostFormatTree(expectedCos);
      Assert.True(resultCos == expectedCos);

      // tan(f(x)) => f'(x) / (cos(f(x)))^2
      var tan = new ASTNode(Token.Function("tan"), new List<ASTNode> { f });
      var expectedTan = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        differentiater.Differentiate(f, x, false),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(f) }),
          new ASTNode(Token.Integer("2"))
        })
      });
      var resultTan = differentiater.Differentiate(tan, x);
      simplifier.PostFormatTree(resultTan);
      simplifier.PostFormatTree(expectedTan);
      Assert.True(resultTan == expectedTan);

      // sec(f(x)) => d/dx(1 / cos(f(x)))
      var sec = new ASTNode(Token.Function("sec"), new List<ASTNode> { f });
      var expectedSecInput = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(f) })
      });
      var expectedSec = differentiater.Differentiate(expectedSecInput, x, false);
      var resultSec = differentiater.Differentiate(sec, x);
      simplifier.PostFormatTree(resultSec);
      simplifier.PostFormatTree(expectedSec);
      Assert.True(resultSec == expectedSec);

      // csc(f(x)) => d/dx(1 / sin(f(x)))
      var csc = new ASTNode(Token.Function("csc"), new List<ASTNode> { f });
      var expectedCscInput = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(f) })
      });
      var expectedCsc = differentiater.Differentiate(expectedCscInput, x, false);
      var resultCsc = differentiater.Differentiate(csc, x);
      simplifier.PostFormatTree(resultCsc);
      simplifier.PostFormatTree(expectedCsc);
      Assert.True(resultCsc == expectedCsc);

      // cot(f(x)) => d/dx(cos(f(x)) / sin(f(x)))
      var cot = new ASTNode(Token.Function("cot"), new List<ASTNode> { f });
      var expectedCotInput = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(f) }),
        new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(f) })
      });
      var expectedCot = differentiater.Differentiate(expectedCotInput, x, false);
      var resultCot = differentiater.Differentiate(cot, x);
      simplifier.PostFormatTree(resultCot);
      simplifier.PostFormatTree(expectedCot);
      Assert.True(resultCot == expectedCot);

      // arcsin(f(x)) => f'(x) / sqrt(1 - f(x)^2)
      var arcsin = new ASTNode(Token.Function("arcsin"), new List<ASTNode> { f });
      var denumArcsin = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Operator("*"), new List<ASTNode> { 
            new ASTNode(Token.Integer("-1")),new ASTNode(Token.Operator("^"), 
            new List<ASTNode> { new ASTNode(f), new ASTNode(Token.Integer("2")) 
            })  
          })
        }),
        new ASTNode(Token.Fraction(), new List<ASTNode> { new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2")) })
      });
      var expectedArcsin = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        differentiater.Differentiate(f, x, false),
        denumArcsin
      });
      var resultArcsin = differentiater.Differentiate(arcsin, x);
      simplifier.PostFormatTree(resultArcsin);
      simplifier.PostFormatTree(expectedArcsin);
      Assert.True(resultArcsin == expectedArcsin);

      // arccos(f(x)) => (-1) * f'(x) / sqrt(1 - f(x)^2)
      var arccos = new ASTNode(Token.Function("arccos"), new List<ASTNode> { f });
      var denumArccos = denumArcsin; // same denominator as arcsin
      var expectedArccos = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("-1")),
          differentiater.Differentiate(f, x, false)
        }),
        denumArccos
      });
      var resultArccos = differentiater.Differentiate(arccos, x);
      simplifier.PostFormatTree(resultArccos);
      simplifier.PostFormatTree(expectedArccos);
      Assert.True(resultArccos == expectedArccos);

      // arctan(f(x)) => f'(x) / (1 + f(x)^2)
      var arctan = new ASTNode(Token.Function("arctan"), new List<ASTNode> { f });
      var denumArctan = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Operator("^"), new List<ASTNode> { new ASTNode(f), new ASTNode(Token.Integer("2")) })
      });
      var expectedArctan = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        differentiater.Differentiate(f, x, false),
        denumArctan
      });
      var resultArctan = differentiater.Differentiate(arctan, x);
      simplifier.PostFormatTree(resultArctan);
      simplifier.PostFormatTree(expectedArctan);
      Assert.True(resultArctan == expectedArctan);

      // arcsec(f(x)) => f'(x) / (f(x) * sqrt(f(x)^2 - 1))
      var arcsec = new ASTNode(Token.Function("arcsec"), new List<ASTNode> { f });
      var denumArcsec = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(f),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("^"), new List<ASTNode> { new ASTNode(f), new ASTNode(Token.Integer("2")) }),
            new ASTNode(Token.Integer("-1"))
          }),
          new ASTNode(Token.Fraction(), new List<ASTNode> { new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2")) })
        })
      });
      var expectedArcsec = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        differentiater.Differentiate(f, x, false),
        denumArcsec
      });
      var resultArcsec = differentiater.Differentiate(arcsec, x);
      simplifier.PostFormatTree(resultArcsec);
      simplifier.PostFormatTree(expectedArcsec);
      Assert.True(resultArcsec == expectedArcsec);

      // arccsc(f(x)) => (-1) * f'(x) / (f(x) * sqrt(f(x)^2 - 1))
      var arccsc = new ASTNode(Token.Function("arccsc"), new List<ASTNode> { f });
      var denumArccsc = denumArcsec; // same denominator as arcsec
      var expectedArccsc = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("-1")),
          differentiater.Differentiate(f, x, false)
        }),
        denumArccsc
      });
      var resultArccsc = differentiater.Differentiate(arccsc, x);
      simplifier.PostFormatTree(resultArccsc);
      simplifier.PostFormatTree(expectedArccsc);
      Assert.True(resultArccsc == expectedArccsc);

      // arccot(f(x)) => (-1) * f'(x) / (1 + f(x)^2)
      var arccot = new ASTNode(Token.Function("arccot"), new List<ASTNode> { f });
      var denumArccot = denumArctan; // same denominator as arctan
      var expectedArccot = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("-1")),
          differentiater.Differentiate(f, x, false)
        }),
        denumArccot
      });
      var resultArccot = differentiater.Differentiate(arccot, x);
      simplifier.PostFormatTree(resultArccot);
      simplifier.PostFormatTree(expectedArccot);
      Assert.True(resultArccot == expectedArccot);
    }

    [Fact]
    public void ProductRule()
    {
      var simplifier = new Simplifier();
      var differentiator = new Differentiater(simplifier);
      var x = new ASTNode(Token.Variable("x"));

      // x * sin(x) => sin(x) + x * cos(x)
      var expr1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(Token.Variable("x")) })
      });

      var expected1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(Token.Variable("x")) }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Variable("x")),
          new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(Token.Variable("x")) })
        })
      });

      simplifier.PostFormatTree(expected1);
      var result1 = differentiator.Differentiate(expr1, x);
      simplifier.PostFormatTree(result1);
      Assert.True(result1 == expected1);

      // (x^2 + 1) * (sin(x) * cos(x)) =>
      // 2 * x * sin(x) * cos(x) + (cos(x)^2 + (-1) * sin(x)^2) * (x^2 + 1)
      var x2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(x),
        new ASTNode(Token.Integer("2"))
      });
      var left = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        x2,
        new ASTNode(Token.Integer("1"))
      });

      var sinx = new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) });
      var cosx = new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(x) });
      var fx = new ASTNode(Token.Operator("*"), new List<ASTNode> { sinx, cosx });

      var expr2 = new ASTNode(Token.Operator("*"), new List<ASTNode> { left, fx });

      var leftPrime = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(x)
      });

      var cosSquared = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(x) }),
        new ASTNode(Token.Integer("2"))
      });

      var sinSquared = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) }),
        new ASTNode(Token.Integer("2"))
      });

      var sinSquaredNeg = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("-1")),
        sinSquared
      });

      var fxPrime = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        cosSquared,
        sinSquaredNeg
      });

      var expected2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          leftPrime,
          new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) }),
          new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(x) })
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          fxPrime,
          new ASTNode(left)
        })
      });

      expected2 = simplifier.AutomaticSimplify(expected2);
      simplifier.PostFormatTree(expected2);
      var result2 = differentiator.Differentiate(expr2, x);
      simplifier.PostFormatTree(result2);
      Assert.True(result2 == expected2);
    }

    [Fact]
    public void QuotientRule()
    {
      var simplifier = new Simplifier();
      var differentiater = new Differentiater(simplifier);
      var x = new ASTNode(Token.Variable("x"));

      // 1 / (x^2) => (-2) / (x^3)
      var denom1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(x),
        new ASTNode(Token.Integer("2"))
      });

      var expr1 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        denom1
      });

      var expected1 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("-2")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(x),
          new ASTNode(Token.Integer("3"))
        })
      });

      simplifier.PostFormatTree(expected1);
      var result1 = differentiater.Differentiate(expr1, x);
      simplifier.PostFormatTree(result1);
      Assert.True(result1 == expected1);

      // sin(x) / (2 * x^3) => (x * cos(x) + (-3) * sin(x)) / (2 * x^4)
      var sinx = new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) });

      var x3 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(x),
        new ASTNode(Token.Integer("3"))
      });

      var denom2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        x3
      });

      var expr2 = new ASTNode(Token.Operator("/"), new List<ASTNode> { sinx, denom2 });

      var term1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("-6")),
        new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(x),
          new ASTNode(Token.Integer("2"))
        })
      });

      var term2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(x) }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(x),
          new ASTNode(Token.Integer("3"))
        })
      });

      var innerNumerator = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        term1,
        term2
      });

      var innerDenominator = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(x),
        new ASTNode(Token.Integer("6"))
      });

      var expected2 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")),
            new ASTNode(Token.Integer("4"))
          }),
          innerNumerator 
        }),
        innerDenominator
      });

      var result2 = differentiater.Differentiate(expr2, x);
      simplifier.PostFormatTree(result2);
      Assert.True(result2 == expected2);
    }

    [Fact]
    public void ComplexPowerRule()
    {
      var x = new ASTNode(Token.Variable("x"));
      var f = new ASTNode(Token.Function("f"), new List<ASTNode> { new ASTNode(x) });
      var g = new ASTNode(Token.Function("g"), new List<ASTNode> { new ASTNode(x) });

      var input = new ASTNode(Token.Operator("^"), new List<ASTNode> { new ASTNode(f), new ASTNode(g) });

      var differentiater = new Differentiater(new Simplifier());
      var result = differentiater.Differentiate(input, x);

      // Expected tree:
      // (ln(f(x)) * g'(x) + (f'(x) * g(x)) / f(x)) * f(x)^g(x)

      var ln_f = new ASTNode(Token.Function("ln"), new List<ASTNode> { new ASTNode(f) });
      var g_prime = differentiater.Differentiate(g, x);

      var f_prime = differentiater.Differentiate(f, x);

      var left = new ASTNode(Token.Operator("*"), new List<ASTNode> { ln_f, g_prime });

      var right = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode> { f_prime, new ASTNode(g) }),
        new ASTNode(f)
      });

      var sum = new ASTNode(Token.Operator("+"), new List<ASTNode> { left, right });

      var power = new ASTNode(Token.Operator("^"), new List<ASTNode> { new ASTNode(f), new ASTNode(g) });

      var expected = new ASTNode(Token.Operator("*"), new List<ASTNode> { sum, power });

      var simplifier = new Simplifier();
      simplifier.PostFormatTree(result);
      simplifier.PostFormatTree(expected);

      Assert.Equal(expected, result);
    }

    [Fact]
    public void NDifferentiate()
    {
      var x = new ASTNode(Token.Variable("x"));
      var differentiater = new Differentiater(new Simplifier());

      {
        // x^6 => 120 * x^3 (n = 3)
        var input = new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(x),
          new ASTNode(Token.Integer("6"))
        });

        var expected = new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("120")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(x),
            new ASTNode(Token.Integer("3"))
          })
        });

        var result = differentiater.NDifferentiate(input, x, 3);
        var simplifier = new Simplifier();
        simplifier.PostFormatTree(result);
        simplifier.PostFormatTree(expected);
        Assert.Equal(expected, result);
      }

      {
        // sin(x) => -sin(x) (n = 2)
        var input = new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) });

        var expected = new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("-1")),
          new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) })
        });

        var result = differentiater.NDifferentiate(input, x, 2);
        var simplifier = new Simplifier();
        simplifier.PostFormatTree(result);
        simplifier.PostFormatTree(expected);
        Assert.Equal(expected, result);
      }

      {
        // sin(x) * x^2 => -x^2 * sin(x) + 4 * x * cos(x) + 2 * sin(x) (n = 2)
        var sinX = new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(x) });
        var x2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(x),
          new ASTNode(Token.Integer("2"))
        });

        var input = new ASTNode(Token.Operator("*"), new List<ASTNode> { sinX, x2 });

        var expected = new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          // -x^2 * sin(x)
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("-1")),
            new ASTNode(x2), 
            new ASTNode(sinX)
          }),

          // 4 * x * cos(x)
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("4")),
            new ASTNode(x),
            new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(x) })
          }),

          // 2 * sin(x)
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("2")),
            new ASTNode(sinX)
          })
        });

        var result = differentiater.NDifferentiate(input, x, 2);
        var simplifier = new Simplifier();
        simplifier.PostFormatTree(result);
        simplifier.PostFormatTree(expected);
        Assert.Equal(expected, result);
      }

      {
        // x^3 => 0 (n = 4)
        var input = new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(x),
          new ASTNode(Token.Integer("3"))
        });

        var expected = new ASTNode(Token.Integer("0"));
        var result = differentiater.NDifferentiate(input, x, 4);
        var simplifier = new Simplifier();
        simplifier.PostFormatTree(result);
        simplifier.PostFormatTree(expected);
        Assert.Equal(expected, result);
      }
    }
  }
}
