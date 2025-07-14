using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAS.Core;
using CAS.Core.EquationParsing;

namespace CAS.UT
{
  public class ASTNodeTest
  {
    [Fact]
    public void Operators()
    {
      var tree1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("-"), new List<ASTNode>
        {
          new ASTNode(Token.Number("5"), new List<ASTNode>()),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
        }),
        new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      var operand1 = new ASTNode(Token.Operator("-"), new List<ASTNode>
      {
        new ASTNode(Token.Number("5"), new List<ASTNode>()),
        new ASTNode(Token.Number("2"), new List<ASTNode>())
      });

      var polynomial = new ASTNode(Token.Operator("+"), new List<ASTNode>
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

      var substituted = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                  new ASTNode(Token.Operator("-"), new List<ASTNode>
                  {
                      new ASTNode(Token.Number("5"), new List<ASTNode>()),
                      new ASTNode(Token.Number("2"), new List<ASTNode>())
                  }),
                  new ASTNode(Token.Number("2"), new List<ASTNode>())
              }),
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("2"), new List<ASTNode>()),
                  new ASTNode(Token.Operator("-"), new List<ASTNode>
                  {
                      new ASTNode(Token.Number("5"), new List<ASTNode>()),
                      new ASTNode(Token.Number("2"), new List<ASTNode>())
                  })
              })
          }),
          new ASTNode(Token.Number("1"), new List<ASTNode>())
      });

      Assert.Equal(2, tree1.NumOfOperands());
      Assert.True(operand1 == tree1.OperandAt(0));
      Assert.Throws<IndexOutOfRangeException>(() => tree1.OperandAt(2));
      Assert.False(tree1.FreeOf(operand1));
      Assert.True(tree1.FreeOf(new ASTNode(Token.Number("9"), new List<ASTNode>())));
      Assert.False(tree1.FreeOf(new ASTNode(Token.Number("5"), new List<ASTNode>())));

      polynomial.Substitute(new ASTNode(Token.Variable("x"), new List<ASTNode>()), operand1);
      Assert.True(polynomial == substituted);
    }

    [Fact]
    public void BaseAndExponent()
    {
      // Case 1: Variable x
      var x = new ASTNode(Token.Variable("x"));
      var y = x.Base();
      Assert.Equal(x, x.Base());
      Assert.Equal("1", x.Exponent().Token.Type.stringValue);

      // Case 2: Product (2 * x)
      var product = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
      new ASTNode(Token.Integer("2")),
      new ASTNode(Token.Variable("x"))
      });
      Assert.Equal(product, product.Base());
      Assert.Equal("1", product.Exponent().Token.Type.stringValue);

      // Case 3: Power (x^3)
      var baseNode = new ASTNode(Token.Variable("x"));
      var exponentNode = new ASTNode(Token.Integer("3"));
      var power = new ASTNode(Token.Operator("^"), new List<ASTNode> { baseNode, exponentNode });
      Assert.Equal(baseNode, power.Base());
      Assert.Equal(exponentNode, power.Exponent());

      // Case 4: Integer
      var intNode = new ASTNode(Token.Integer("5"));
      Assert.True(intNode.Base().Token.Type is Undefined);
      Assert.True(intNode.Exponent().Token.Type is Undefined);

      // Case 5: Fraction (2/3)
      var fraction = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
      new ASTNode(Token.Integer("2")),
      new ASTNode(Token.Integer("3"))
      });
      Assert.True(fraction.Base().Token.Type is Undefined);
      Assert.True(fraction.Exponent().Token.Type is Undefined);

      // Case 6: Function (sin(x))
      var func = new ASTNode(Token.Function("sin"), new List<ASTNode>
      {
      new ASTNode(Token.Variable("x"))
      });
      Assert.Equal(func, func.Base());
      Assert.Equal("1", func.Exponent().Token.Type.stringValue);
    }

    [Fact]
    public void TermAndConst()
    {
      // Helper to check unary product
      void AssertUnaryProduct(ASTNode result, ASTNode expectedChild)
      {
        Assert.Equal("*", result.Token.Type.stringValue);
        Assert.Single(result.Children);
        Assert.Equal(expectedChild, result.Children[0]);
      }

      // Case 1: Variable x
      var x = new ASTNode(Token.Variable("x"));
      AssertUnaryProduct(x.Terms(), x);
      Assert.Equal("1", x.Const().Token.Type.stringValue);

      // Case 2: Power (x^2)
      var power = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("2"))
      });
      AssertUnaryProduct(power.Terms(), power);
      Assert.Equal("1", power.Const().Token.Type.stringValue);

      // Case 3: Function (sin(x))
      var func = new ASTNode(Token.Function("sin"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x"))
      });
      AssertUnaryProduct(func.Terms(), func);
      Assert.Equal("1", func.Const().Token.Type.stringValue);

      // Case 4: Sum (x + 2)
      var sum = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("2"))
      });
      AssertUnaryProduct(sum.Terms(), sum);
      Assert.Equal("1", sum.Const().Token.Type.stringValue);

      // Case 5: Product of constant and variable (2 * x)
      var prodConstFirst = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Variable("x"))
      });
      AssertUnaryProduct(prodConstFirst.Terms(), prodConstFirst.Children[1]); // x
      Assert.Equal("2", prodConstFirst.Const().Token.Type.stringValue);

      // Case 7: Integer
      var integer = new ASTNode(Token.Integer("4"));
      Assert.True(integer.Terms().Token.Type is Undefined);
      Assert.True(integer.Const() == integer);

      // Case 8: Fraction
      var fraction = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer("5")),
        new ASTNode(Token.Integer("3"))
      });
      Assert.True(fraction.Terms().Token.Type is Undefined);
      Assert.True(fraction.Const() == fraction);
    }

    [Fact]
    public void Equality()
    {
      var tree1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("-"), new List<ASTNode>
        {
          new ASTNode(Token.Number("5"), new List<ASTNode>()),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
        }),
        new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      var tree2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("-"), new List<ASTNode>
        {
          new ASTNode(Token.Number("5"), new List<ASTNode>()),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
        }),
        new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      var tree3 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
              new ASTNode(Token.Number("6"), new List<ASTNode>()),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
          }),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
      });

      Assert.True(tree1 == tree2);
      Assert.False(tree1 != tree2);
      Assert.False(tree1 == tree3);
      Assert.True(tree1 != tree3);
    }

    [Fact]
    public void ComparisonsSameTypes()
    {
      // Constants
      var two = new ASTNode(Token.Integer("2"));
      var five = new ASTNode(Token.Integer("5"));
      Assert.True(two < five);
      Assert.False(five < two);
      Assert.False(two < two);

      // Symbols
      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));
      Assert.True(x < y);
      Assert.False(y < x);

      // Constant vs Symbol
      Assert.True(two < x);
      Assert.False(x < two);

      // Sums
      var sum1 = new ASTNode(Token.Operator("+"), new() { x });
      var sum2 = new ASTNode(Token.Operator("+"), new() { x, y });
      Assert.True(sum1 < sum2);
      Assert.False(sum2 < sum1);

      // Products
      var prod1 = new ASTNode(Token.Operator("*"), new() { x });
      var prod2 = new ASTNode(Token.Operator("*"), new() { x, y });
      Assert.True(prod1 < prod2);
      Assert.False(prod2 < prod1);

      // Powers
      var pow1 = new ASTNode(Token.Operator("^"), new() { x, two });
      var pow2 = new ASTNode(Token.Operator("^"), new() { x, five });
      Assert.True(pow1 < pow2);
      Assert.False(pow2 < pow1);

      // Factorials
      /*
      var fact1 = new ASTNode(Token.Factorial(), new() { two });
      var fact2 = new ASTNode(Token.Factorial(), new() { five });
      Assert.True(fact1 < fact2);
      Assert.False(fact2 < fact1);
      */

      // Functions
      var f1 = new ASTNode(Token.Function("f"), new() { x });
      var f2 = new ASTNode(Token.Function("g"), new() { x });
      var f1xy = new ASTNode(Token.Function("f"), new() { x, y });
      var xFunc = new ASTNode(Token.Function("x"), new() { y });

      // Lexical function name comparison
      Assert.True(f1 < f2);
      // f(x) < f(x, y)
      Assert.True(f1 < f1xy);
      Assert.False(f2 < f1);
      Assert.False(f1xy < f1);

      // Symbol vs Function
      Assert.False(x < f1);
      Assert.True(x < xFunc);
    }

    [Fact]
    public void ComparisonMixedTypes()
    {
      // Constant vs Symbol (Rule O-7)
      var const2 = new ASTNode(Token.Integer("2"));
      var symbolX = new ASTNode(Token.Variable("x"));
      Assert.True(const2 < symbolX);
      Assert.False(symbolX < const2);

      // Constant vs Function (Rule O-7)
      var funcSinX = new ASTNode(Token.Function("sin"), new() { symbolX });
      Assert.True(const2 < funcSinX);
      Assert.False(funcSinX < const2);

      // Symbol vs Function (Rule O-12)
      var symbolY = new ASTNode(Token.Variable("y"));
      var funcSinY = new ASTNode(Token.Function("sin"), new() { symbolY });
      Assert.False(symbolX < funcSinY);
      Assert.True(funcSinY < symbolX);

      // Symbol vs Factorial (Rule O-11)
      // var factX = new ASTNode(Token.Factorial(), new() { symbolX });
      // Assert.True(symbolX < factX);
      // Assert.False(factX < symbolX);

      // Function vs Factorial (Rule O-11)
      // Assert.True(funcSinX < factX || factX > funcSinX);

      // Constant vs Power (Rule O-9)
      var powerXY = new ASTNode(Token.Operator("^"), new() { symbolX, symbolY });
      Assert.True(const2 < powerXY);
      Assert.False(powerXY < const2);

      // Symbol vs Sum (Rule O-10)
      var sumXY = new ASTNode(Token.Operator("+"), new() { symbolX, symbolY });
      Assert.True(symbolX < sumXY);
      Assert.False(sumXY < symbolX);

      // Function vs Product (Rule O-8)
      var productXY = new ASTNode(Token.Operator("*"), new() { symbolX, symbolY });
      Assert.True(funcSinX < productXY || productXY > funcSinX);

      // Factorial vs Power (Rule O-13 fallback if not caught earlier)
      //Assert.True(factX < powerXY || powerXY > factX);

      // Function vs Function (Rule O-6): f(x) < g(x)
      var f_fx = new ASTNode(Token.Function("f"), new() { symbolX });
      var f_gx = new ASTNode(Token.Function("g"), new() { symbolX });
      Assert.True(f_fx < f_gx);
      Assert.False(f_gx < f_fx);

      // Function name same, different args: f(x) < f(x, y)
      var f_fxy = new ASTNode(Token.Function("f"), new() { symbolX, symbolY });
      Assert.True(f_fx < f_fxy);
      Assert.False(f_fxy < f_fx);

      // Factorial(x) vs Factorial(y)
      //var factY = new ASTNode(Token.Factorial(), new() { symbolY });
      //Assert.True(factX < factY);
      //Assert.False(factY < factX);
    }

    [Fact]
    public void AreLikeTerms()
    {
      // Helper to make nodes quickly
      ASTNode Int(string val) => new(Token.Integer(val));
      ASTNode Var(string name) => new(Token.Variable(name));
      ASTNode Mul(params ASTNode[] nodes) => new(Token.Operator("*"), nodes.ToList());
      ASTNode Pow(ASTNode b, ASTNode e) => new(Token.Operator("^"), new() { b, e });

      // 1. Same variable, different coefficients → alike
      Assert.True(ASTNode.AreLikeTerms(Mul(Int("2"), Var("x")), Mul(Int("3"), Var("x"))));

      // 2. Same variable and power, different coefficients → alike
      Assert.True(ASTNode.AreLikeTerms(Mul(Int("2"), Pow(Var("x"), Int("2"))), Mul(Int("5"), Pow(Var("x"), Int("2")))));

      // 3. Different variable → not alike
      Assert.False(ASTNode.AreLikeTerms(Mul(Int("2"), Var("x")), Mul(Int("2"), Var("y"))));

      // 4. Different exponent → not alike
      Assert.False(ASTNode.AreLikeTerms(Mul(Int("2"), Pow(Var("x"), Int("2"))), Mul(Int("5"), Pow(Var("x"), Int("3")))));

      // 5. Same constant → alike
      Assert.True(ASTNode.AreLikeTerms(Int("5"), Int("5")));

      // 6. Constant and variable → not alike
      Assert.False(ASTNode.AreLikeTerms(Int("5"), Var("x")));

      // 7. Same nested product: (2 * x * y) vs (5 * x * y) → alike
      Assert.True(ASTNode.AreLikeTerms(Mul(Int("2"), Var("x"), Var("y")), Mul(Int("5"), Var("x"), Var("y"))));

      // 8. Different nested symbolic part: (2 * x * y) vs (5 * x * z) → not alike
      Assert.False(ASTNode.AreLikeTerms(Mul(Int("2"), Var("x"), Var("y")), Mul(Int("5"), Var("x"), Var("z"))));

      // 9. Same function: sin(x) vs 2 * sin(x) → alike
      var sinX = new ASTNode(Token.Function("sin"), new List<ASTNode> { Var("x") });
      Assert.True(ASTNode.AreLikeTerms(sinX, Mul(Int("2"), sinX)));

      // 10. Powers with variables: x^2 vs 4 * x^2 → alike
      Assert.True(ASTNode.AreLikeTerms(Pow(Var("x"), Int("2")), Mul(Int("4"), Pow(Var("x"), Int("2")))));

      // 11. Constant vs product → not alike
      Assert.False(ASTNode.AreLikeTerms(Int("2"), Mul(Int("2"), Var("x"))));
    }

    [Fact]
    public void ToLatex()
    {
      // Variable
      var x = new ASTNode(Token.Variable("x"));
      Assert.Equal("x", x.ToLatex());

      // Simple sum: x + 1
      var sum = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("1"))
      });
      Assert.Equal("x + 1", sum.ToLatex());

      // Product: 2 * x
      var product = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        x
      });
      Assert.Equal("2x", product.ToLatex());

      // Fraction: 1/2
      var frac = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer("1")),
        new ASTNode(Token.Integer("2"))
      });
      Assert.Equal("\\frac{1}{2}", frac.ToLatex());

      // Power: (x + 1)^2
      var power = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        sum,
        new ASTNode(Token.Integer("2"))
      });
      Assert.Equal("\\left(x + 1\\right)^{2}", power.ToLatex());

      // Nested function: sin(x)
      var sin = new ASTNode(Token.Function("sin"), new List<ASTNode> { x });
      Assert.Equal("sin\\left(x\\right)", sin.ToLatex());

      // Nested fraction and power: (x^2)/(x+1)
      var frac2 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        sum
      });
      Assert.Equal("\\frac{x^{2}}{x + 1}", frac2.ToLatex());

      // More complex expression: sin(x)^2 + cos(x)^2
      var sin2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        sin,
        new ASTNode(Token.Integer("2"))
      });
      var cos = new ASTNode(Token.Function("cos"), new List<ASTNode> { x });
      var cos2 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        cos,
        new ASTNode(Token.Integer("2"))
      });
      var identity = new ASTNode(Token.Operator("+"), new List<ASTNode> { sin2, cos2 });
      Assert.Equal("sin\\left(x\\right)^{2} + cos\\left(x\\right)^{2}", identity.ToLatex());

      // Negative unary: -x
      var neg = new ASTNode(Token.Operator("-"), new List<ASTNode> { x });
      Assert.Equal("-x", neg.ToLatex());
    }

    [Fact]
    public void PolynomialAndDegreeGPE()
    {
      var x = new ASTNode(Token.Variable("x"));
      var y = new ASTNode(Token.Variable("y"));

      // Case 1: Simple monomial x^3
      var expr1 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Integer("3"))
      });
      Assert.True(expr1.IsPolynomialGPE(x));
      Assert.Equal(3, expr1.DegreeGPE(x));

      // Case 2: Constant 5
      var expr2 = new ASTNode(Token.Integer("5"));
      Assert.True(expr2.IsPolynomialGPE(x));
      Assert.Equal(0, expr2.DegreeGPE(x));

      // Case 3: Product 3 * x^2
      var expr3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("3")),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        })
      });
      Assert.True(expr3.IsPolynomialGPE(x));
      Assert.Equal(2, expr3.DegreeGPE(x));

      // Case 4: Sum 2x + 3
      var expr4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("2")),
          x
        }),
        new ASTNode(Token.Integer("3"))
      });
      Assert.True(expr4.IsPolynomialGPE(x));
      Assert.Equal(1, expr4.DegreeGPE(x));

      // Case 5: Sum x^2 + 2x + 1
      var expr5 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("2")),
          x
        }),
        new ASTNode(Token.Integer("1"))
      });
      Assert.True(expr5.IsPolynomialGPE(x));
      Assert.Equal(2, expr5.DegreeGPE(x));

      // Case 6: Product involving function coefficient: sin(y) * x^3
      var expr6 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Function("sin"), new List<ASTNode> { y }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("3"))
        })
      });
      Assert.True(expr6.IsPolynomialGPE(x));
      Assert.Equal(3, expr6.DegreeGPE(x));

      // Case 7: Non-polynomial: x^sqrt(2)
      var expr7 = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        x,
        new ASTNode(Token.Number("1.4142135")) // approximate sqrt(2)
      });
      Assert.False(expr7.IsPolynomialGPE(x));

      // Case 8: Non-polynomial: ln(x)
      var expr8 = new ASTNode(Token.Function("ln"), new List<ASTNode> { x });
      Assert.False(expr8.IsPolynomialGPE(x));

      // Case 9: GPE with multiple variables: x^2 + y^2
      var expr9 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          x,
          new ASTNode(Token.Integer("2"))
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          y,
          new ASTNode(Token.Integer("2"))
        })
      });
      Assert.True(expr9.IsPolynomialGPE(x));
      Assert.True(expr9.IsPolynomialGPE(y));
      Assert.Equal(2, expr9.DegreeGPE(x));

      // Case 10: GPE with function coefficient ln(x)*y
      var expr10 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Function("ln"), new List<ASTNode> { x }),
        y
      });
      Assert.True(expr10.IsPolynomialGPE(y));
      Assert.Equal(1, expr10.DegreeGPE(y));
      
      var notExpandedPoly = new ASTNode(Token.Operator("*"), new List<ASTNode>
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

      Assert.True(notExpandedPoly.IsPolynomialGPE(x));
      Assert.False(notExpandedPoly.IsPolynomialGPE(x, false));
      Assert.Equal(1, notExpandedPoly.OperandAt(0).DegreeGPE(x));
      Assert.Equal(1, notExpandedPoly.OperandAt(1).DegreeGPE(x));
    }

    [Fact]
    public void CoefficientGPE()
    {
      var x = new ASTNode(Token.Variable("x"));

      // Example: 3x^2 + 2x + 5
      var poly1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("3")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("2"))
          })
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("2")),
          x
        }),
        new ASTNode(Token.Integer("5"))
      });

      // Degree 2: coefficient should be 3
      var coeffDeg2 = poly1.CoefficientGPE(x, 2);
      var expected2 = new ASTNode(Token.Integer("3"));
      Assert.True(coeffDeg2 == expected2);

      // Degree 1: coefficient should be 2
      var coeffDeg1 = poly1.CoefficientGPE(x, 1);
      var expected1 = new ASTNode(Token.Integer("2"));
      Assert.True(coeffDeg1 == expected1);

      // Degree 0: coefficient should be 5
      var coeffDeg0 = poly1.CoefficientGPE(x, 0);
      var expected0 = new ASTNode(Token.Integer("5"));
      Assert.True(coeffDeg0 == expected0);

      // Degree 3: no such term, should be 0
      var coeffDeg3 = poly1.CoefficientGPE(x, 3);
      var expected3 = new ASTNode(Token.Integer("0"));
      Assert.True(coeffDeg3 == expected3);

      var leadingCoeff = poly1.LeadingCoefficient(x);
      Assert.True(coeffDeg2 == leadingCoeff);
      
      // More complex coefficients
      var a = new ASTNode(Token.Variable("a"));
      var b = new ASTNode(Token.Variable("b"));
      var c = new ASTNode(Token.Variable("c"));

      // Expression: (3*a*b)x^2 + (2*c)x + 5
      var coeff1 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("3")),
        a,
        b
      });

      var coeff2 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        c
      });

      var poly2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          coeff1,
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            x,
            new ASTNode(Token.Integer("2"))
          })
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          coeff2,
          x
        }),
        new ASTNode(Token.Integer("5"))
      });

      // Degree 2 coefficient: 3 * a * b
      coeffDeg2 = poly2.CoefficientGPE(x, 2);
      expected2 = coeff1;
      Assert.True(coeffDeg2 == expected2);

      // Degree 1 coefficient: 2 * c
      coeffDeg1 = poly2.CoefficientGPE(x, 1);
      expected1 = coeff2;
      Assert.True(coeffDeg1 == expected1);

      // Degree 0 coefficient: 5
      coeffDeg0 = poly2.CoefficientGPE(x, 0);
      expected0 = new ASTNode(Token.Integer("5"));
      Assert.True(coeffDeg0 == expected0);

      // Degree 3: no such term, should return 0
      coeffDeg3 = poly2.CoefficientGPE(x, 3);
      expected3 = new ASTNode(Token.Integer("0"));
      Assert.True(coeffDeg3 == expected3);

      leadingCoeff = poly2.LeadingCoefficient(x);
      Assert.True(coeffDeg2 == leadingCoeff);
    }

    [Fact]
    public void GetVariables_MultipleEquations()
    {
      // ---- Expression 1: x ----
      var expr1 = new ASTNode(Token.Variable("x"), new List<ASTNode>());
      var vars1 = expr1.GetVariables();
      Assert.Single(vars1);
      Assert.Contains(new Variable("x"), vars1);

      // ---- Expression 2: x + y ----
      var expr2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x"), new List<ASTNode>()),
        new ASTNode(Token.Variable("y"), new List<ASTNode>())
      });
      var vars2 = expr2.GetVariables();
      Assert.Equal(2, vars2.Count);
      Assert.Contains(new Variable("x"), vars2);
      Assert.Contains(new Variable("y"), vars2);

      // ---- Expression 3: (a * b) + (c ^ 2) ----
      var expr3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Variable("a"), new List<ASTNode>()),
          new ASTNode(Token.Variable("b"), new List<ASTNode>())
        }),
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Variable("c"), new List<ASTNode>()),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
        })
      });
      var vars3 = expr3.GetVariables();
      Assert.Equal(3, vars3.Count);
      Assert.Contains(new Variable("a"), vars3);
      Assert.Contains(new Variable("b"), vars3);
      Assert.Contains(new Variable("c"), vars3);

      // ---- Expression 4: 2 + 5 ----
      var expr4 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Number("2"), new List<ASTNode>()),
        new ASTNode(Token.Number("5"), new List<ASTNode>())
      });
      var vars4 = expr4.GetVariables();
      Assert.Empty(vars4);
    }
  }
}
