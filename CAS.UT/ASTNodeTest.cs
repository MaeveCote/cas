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
