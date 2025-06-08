using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CAS.Core.EquationParsing;

namespace CAS.Core
{
  /// <summary>
  /// Provides various functions to simplify algebraic equations.
  /// </summary>
  public class Simplifier
  {
    public Simplifier() { }

    /// <summary>
    /// Applys Depth First Search to format the nodes of the tree.
    /// Converts division into multiplications with negative powers or fractions.
    /// Groups the multiplication and additions together.
    /// </summary>
    /// <remarks>This should be applied after building the AST.</remarks>
    /// <param name="root">The root of the tree.</param>
    public void FormatTree(ASTNode root)
    {
      // Format the children of the root
      foreach (ASTNode child in root.Children)
        FormatTree(child);

      // Convert the '/' operators to multiplications and powers or fractions
      if (root.Token.Type.stringValue == "/")
      {
        if (root.Children.All(child => child.Token.Type is Number))
        {
          // This is a fraction
          root.Token = Token.Fraction();
        }
        else
        {
          // This is an operation
          root.Token = Token.Operator("*");
          var tempChild = root.Children[1];
          if (tempChild.Token.Type is Number)
          {
            root.Children[1] = new ASTNode(Token.Fraction(), new List<ASTNode> { new ASTNode(Token.Number("-1"), new List<ASTNode>()), tempChild });
          }
          root.Children[1] = new ASTNode(Token.Operator("^"), new List<ASTNode> { tempChild, new ASTNode(Token.Number("-1"), new List<ASTNode>()) });
        }
      }

      // Group the multiplications and additions together
      if (root.Token.Type.stringValue == "*" || root.Token.Type.stringValue == "+")
      {
        for (int i = 0; i < root.Children.Count(); i++)
        {
          if (root.Children[i].Token.Type.stringValue == root.Token.Type.stringValue)
          {
            foreach (ASTNode child in root.Children[i].Children)
            {
              root.Children.Add(child);
            }
            root.Children.RemoveAt(i);
          }
        }
      }
    }
  }
}
