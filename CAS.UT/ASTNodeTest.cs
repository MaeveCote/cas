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
  }
}
