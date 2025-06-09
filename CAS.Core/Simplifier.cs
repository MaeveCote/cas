using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.XPath;
using CAS.Core.EquationParsing;

namespace CAS.Core
{
  /// <summary>
  /// Provides various functions to simplify algebraic equations.
  /// </summary>
  public class Simplifier
  {
    const double EPSILON = 1e-10;

    public Simplifier() { }

    /// <summary>
    /// Applys Depth First Search to format the nodes of the tree.
    /// Converts division into multiplications with negative powers or fractions.
    /// Groups the multiplication and additions together.
    /// Converts Number to IntegerNum if it is not decimal.
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

      // Convert Number to IntegerNum if it is not a decimal number
      if (root.Token.Type is Number && IsInteger((Number)root.Token.Type))
        root.Token = Token.Integer(root.Token.Type.stringValue);
    }

    /// <summary>
    /// Simplify a rational number into an irreducible fraction or an integer.
    /// </summary>
    /// <param name="input">A rationnal number. Either a fraction or an integer</param>
    /// <returns>An irreducible fraction or interger</returns>
    /// <exception cref="ArgumentException">The input is not a rationnal number.</exception>
    public ASTNode SimplifyRationalNumber(ASTNode input)
    {
      if (input.Token.Type is IntegerNum)
        return input;

      else if (input.Token.Type is Fraction)
      {
        if (input.OperandAt(0).Token.Type is IntegerNum numToken &&
            input.OperandAt(1).Token.Type is IntegerNum denToken)
        {
          int num = numToken.intVal;
          int denum = denToken.intVal;
          
          if (num == 0)
            return new ASTNode(Token.Integer("0"), new List<ASTNode>());
          else if (denum == 0)
            return new ASTNode(Token.Undefined(), new List<ASTNode>());

          if (Calculator.Remainder(num, denum) == 0)
            return new ASTNode(Token.Integer(Calculator.Quotient(num, denum).ToString()), new List<ASTNode>());

          int gcd = Calculator.GCD(num, denum);
          if (denum > 0)
            return new ASTNode(Token.Fraction(),
              new List<ASTNode> { new ASTNode(Token.Integer(Calculator.Quotient(num, gcd).ToString()), new List<ASTNode>()),
            new ASTNode(Token.Integer(Calculator.Quotient(denum, gcd).ToString()), new List<ASTNode>()) });

          return new ASTNode(Token.Fraction(),
            new List<ASTNode> { new ASTNode(Token.Integer(Calculator.Quotient(-num, gcd).ToString()), new List<ASTNode>()),
          new ASTNode(Token.Integer(Calculator.Quotient(-denum, gcd).ToString()), new List<ASTNode>()) });
        }

        return input;
      }

      throw new ArgumentException("The input is not a rationnal number.");
    }


    /// <summary>
    /// Simplifies a Rational Number Expression (RNE) into an integer or an irreducible fraction.
    /// </summary>
    /// <param name="input">A RNE</param>
    /// <returns>An irreducible fraction or an integer.</returns>
    public ASTNode SimplifyRNE(ASTNode input)
    {
      var result = SimplifyRNE_Rec(input);

      if (result.Token.Type is Undefined)
        return result;
      
      return SimplifyRationalNumber(result);
    }

    public static bool IsInteger(Number num)
    {
      return Math.Abs(num.value % 1) < EPSILON;
    }
    
    private ASTNode SimplifyRNE_Rec(ASTNode input)
    {
      


      return null;
    }
  }
}


