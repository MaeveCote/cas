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
    public void TestTermAndConst()
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
      AssertUnaryProduct(x.Term(), x);
      Assert.Equal("1", x.Const().Token.Type.stringValue);

      // Case 2: Power (x^2)
      var power = new ASTNode(Token.Operator("^"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("2"))
      });
      AssertUnaryProduct(power.Term(), power);
      Assert.Equal("1", power.Const().Token.Type.stringValue);

      // Case 3: Function (sin(x))
      var func = new ASTNode(Token.Function("sin"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x"))
      });
      AssertUnaryProduct(func.Term(), func);
      Assert.Equal("1", func.Const().Token.Type.stringValue);

      // Case 4: Sum (x + 2)
      var sum = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Variable("x")),
        new ASTNode(Token.Integer("2"))
      });
      AssertUnaryProduct(sum.Term(), sum);
      Assert.Equal("1", sum.Const().Token.Type.stringValue);

      // Case 5: Product of constant and variable (2 * x)
      var prodConstFirst = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        new ASTNode(Token.Integer("2")),
        new ASTNode(Token.Variable("x"))
      });
      AssertUnaryProduct(prodConstFirst.Term(), prodConstFirst.Children[1]); // x
      Assert.Equal("2", prodConstFirst.Const().Token.Type.stringValue);

      // Case 7: Integer
      var integer = new ASTNode(Token.Integer("4"));
      Assert.True(integer.Term().Token.Type is Undefined);
      Assert.True(integer.Const().Token.Type is Undefined);

      // Case 8: Fraction
      var fraction = new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer("5")),
        new ASTNode(Token.Integer("3"))
      });
      Assert.True(fraction.Term().Token.Type is Undefined);
      Assert.True(fraction.Const().Token.Type is Undefined);
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
  }
}
