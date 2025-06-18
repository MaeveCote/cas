using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
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
            return new ASTNode(Token.Integer("0"));
          else if (denum == 0)
            return ASTNode.NewUndefined();

          if (Calculator.Remainder(num, denum) == 0)
            return new ASTNode(Token.Integer(Calculator.Quotient(num, denum).ToString()));

          int gcd = Calculator.GCD(num, denum);
          if (denum > 0)
            return new ASTNode(Token.Fraction(),
              new List<ASTNode> { new ASTNode(Token.Integer(Calculator.Quotient(num, gcd).ToString())),
            new ASTNode(Token.Integer(Calculator.Quotient(denum, gcd).ToString()))});

          return new ASTNode(Token.Fraction(),
            new List<ASTNode> { new ASTNode(Token.Integer(Calculator.Quotient(-num, gcd).ToString())),
          new ASTNode(Token.Integer(Calculator.Quotient(-denum, gcd).ToString()))});
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

    /// <summary>
    /// Simplifies a Basic Algebraic Expression (BAE) into an Automatically Simplifes Algebraic Expression (ASAE)
    /// by applying recursively a combination of simplify power, product, sum, quotien and difference.
    /// </summary>
    /// <param name="input">A BAE</param>
    /// <returns>An ASAE</returns>
    public ASTNode AutomaticSimplify(ASTNode input)
    {
      if (input.Token.Type is Number || input.Token.Type is Variable)
        return input;
      else if (input.Token.Type is Fraction)
        return SimplifyRationalNumber(input);
      else
      {
        // Simplify the nodes recursively
        for (int i = 0; i < input.Children.Count(); i++)
          input.Children[i] = AutomaticSimplify(input.Children[i]);

        if (input.Kind() == "^")
          return SimplifyPower(input);
        if (input.Kind() == "*")
          return SimplifyProduct(input);
        if (input.Kind() == "+")
          return SimplifySum(input);
        if (input.Kind() == "-")
          return SimplifyDifference(input);
        if (input.Kind() == "/")
          return SimplifyQuotient(input);
        if (input.Token.Type is Function)
          return SimplifyFunction(input);
      }

      return ASTNode.NewUndefined();
    }

    /// <summary>
    /// Simplifies a power of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyPower(ASTNode input)
    {
      var powBase = input.Base();
      var powExp = input.Exponent();

      // SPOW-1
      if (powBase.Token.Type is Undefined || powExp.Token.Type is Undefined)
        return ASTNode.NewUndefined();

      if (powBase.Token.Type is IntegerNum intNum)
      {
        // SPOW-2
        if (intNum.intVal == 0)
        {
          if (powExp.Token.Type is Number || powExp.Token.Type is Fraction)
            return new ASTNode(Token.Integer("0"), new List<ASTNode>());
          else
            return ASTNode.NewUndefined();
        }
        // SPOW-3
        else if (intNum.intVal == 1)
          return new ASTNode(Token.Integer("1"), new List<ASTNode>());
      }
      // SPOW-4
      if (powExp.Token.Type is IntegerNum num)
        return SimplifyIntegerPower(powBase, num.intVal);

      // SPOW-5
      return input;
    }
    
    /// <summary>
    /// Simplifies a product of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyProduct(ASTNode input)
    {
      // SPRD-1 and 2
      bool isZero = false;
      foreach (ASTNode child in input.Children)
      {
        if (child.Token.Type is Undefined)
          return ASTNode.NewUndefined();
        if (!isZero && child.Token.Type.stringValue == "0")
          isZero = true;
      }

      if (isZero)
        return new ASTNode(Token.Integer("0"));

      // SPRD-3
      if (input.Children.Count == 1)
        return input.Children[0];

      // SPRD-4
      var recSimplified = SimplifyProductRec(input.Children);

      if (recSimplified.Count == 0)
        return new ASTNode(Token.Integer("1"));
      if (recSimplified.Count == 1)
        return recSimplified[0];

      return new ASTNode(Token.Operator("*"), recSimplified);
    }

    /// <summary>
    /// Simplifies a sum of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifySum(ASTNode input)
    {
      return null;
    }

    /// <summary>
    /// Simplifies a quotient of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyQuotient(ASTNode input)
    {
      return null;
    }

    /// <summary>
    /// Simplifies a difference of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyDifference(ASTNode input)
    {
      return null;
    }

    /// <summary>
    /// Simplifies a function of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyFunction(ASTNode input)
    {
      return null;
    }

    #region Private methods

    private static bool IsInteger(Number num)
    {
      return Math.Abs(num.value % 1) < EPSILON;
    }

    private ASTNode SimplifyRNE_Rec(ASTNode input)
    {
      if (input.Token.Type is IntegerNum)
        return input;
      else if (input.Token.Type is Fraction)
      {
        var frac = Calculator.GetNumAndDenum(input);
        if (frac[1] == 0)
          return ASTNode.NewUndefined();
        return input;
      }
      else if (input.Token.Type is Operator op)
      {
        if (op.stringValue == "^")
        {
          var simplifiedBase = SimplifyRNE_Rec(input.OperandAt(0));
          if (simplifiedBase.Token.Type is Undefined)
            return ASTNode.NewUndefined();
          return Calculator.EvaluatePowerRationnal(simplifiedBase, input.OperandAt(1));
        }

        List<ASTNode> simplifiedNodes = new List<ASTNode>();
        foreach (ASTNode child in input.Children)
        {
          var simplifiedChild = SimplifyRNE_Rec(child);
          if (simplifiedChild.Token.Type is Undefined)
            return ASTNode.NewUndefined();
          simplifiedNodes.Add(SimplifyRNE_Rec(child));
        }

        if (op.stringValue == "+")
        {
          ASTNode result = simplifiedNodes[0];
          for (int i = 1; i < simplifiedNodes.Count(); i++)
            result = Calculator.EvaluateSumRationnal(result, simplifiedNodes[i]);

          return result;
        }
        else if (op.stringValue == "-")
        {
          ASTNode result = simplifiedNodes[0];
          for (int i = 1; i < simplifiedNodes.Count(); i++)
            result = Calculator.EvaluateDiffRationnal(result, simplifiedNodes[i]);

          return result;
        }
        else if (op.stringValue == "*")
        {
          ASTNode result = simplifiedNodes[0];
          for (int i = 1; i < simplifiedNodes.Count(); i++)
            result = Calculator.EvaluateProductRationnal(result, simplifiedNodes[i]);

          return result;
        }
        else if (op.stringValue == "/")
        {
          ASTNode result = simplifiedNodes[0];
          for (int i = 1; i < simplifiedNodes.Count(); i++)
            result = Calculator.EvaluateQuotientRationnal(result, simplifiedNodes[i]);

          return result;
        }

        throw new ArgumentException("Unknow operator : '" + op.stringValue + "'");
      }

      throw new ArgumentException("The input is not a RNE.");
    }

    private ASTNode SimplifyIntegerPower(ASTNode powBase, int exp)
    {
      // SINTPOW-1
      if (powBase.Token.Type is IntegerNum || powBase.Token.Type is Fraction)
        return SimplifyRNE(new ASTNode(Token.Operator("^"), new List<ASTNode> { powBase, new ASTNode(Token.Integer(exp.ToString())) }));

      // SINTPOW-2
      if (exp == 0)
        return new ASTNode(Token.Integer("1"));
      // SINTPOW-3
      if (exp == 1)
        return powBase;

      // SINTPOW-4
      if (powBase.Token.Type.stringValue == "^")
      {
        var r = powBase.OperandAt(0);
        var s = powBase.OperandAt(1);
        var p = SimplifyProduct(new ASTNode(Token.Operator("*"), new List<ASTNode> { s, new ASTNode(Token.Integer(exp.ToString())) } ));

        if (p.Token.Type is IntegerNum num)
          return SimplifyIntegerPower(r, num.intVal);

        return new ASTNode(Token.Operator("^"), new List<ASTNode> { r, p });
      }

      // SINTPOW-5
      if (powBase.Token.Type.stringValue == "*")
      {
        // Distribute the exponent to the product
        var mappingResult = new List<ASTNode>();
        foreach (ASTNode operand in powBase.Children)
          mappingResult.Add(SimplifyIntegerPower(operand, exp));

        return SimplifyProduct(new ASTNode(Token.Operator("*"), mappingResult));
      }

      // SINTPOW-6
      return new ASTNode(Token.Operator("^"), new List<ASTNode> { powBase, new ASTNode(Token.Integer(exp.ToString())) });
    }

    private List<ASTNode> SimplifyProductRec(List<ASTNode> operands)
    {
      // SPRDREC-1
      if (operands.Count == 2)
      {
        var u1 = operands[0];
        var u2 = operands[1];

        bool u1IsProduct = u1.IsProduct();
        bool u2IsProduct = u2.IsProduct();

        if (!u1IsProduct && !u2IsProduct)
        {
          // SPRDREC-1.1
          if (u1.IsConstant() && u2.IsConstant())
          {
            var result = SimplifyRNE(new ASTNode(Token.Operator("*"), operands));
            if (result.Token.Type.stringValue == "1")
              return new List<ASTNode>();

            return new List<ASTNode> { result };
          }

          // SPRDREC-1.2
          if (u1.Token.Type.stringValue == "1")
            return new List<ASTNode> { u2 };
          if (u2.Token.Type.stringValue == "1")
            return new List<ASTNode> { u1 };

          // SPRDREC-1.3
          if (u1.Base() == u2.Base())
          {
            var combinedExp = SimplifySum(new ASTNode(Token.Operator("+"), new List<ASTNode> { u1.Exponent(), u2.Exponent() }));
            var combinedPow = SimplifyPower(new ASTNode(Token.Operator("^"), new List<ASTNode> { u1.Base(), combinedExp }));

            if (combinedPow.Token.Type.stringValue == "1")
              return new List<ASTNode>();
            return new List<ASTNode> { combinedPow };
          }

          // SPRDREC-1.4
          if (u1 < u2)
            return new List<ASTNode> { u2, u1 };

          // SPRDREC-1.5
          return operands;
        }

        // SPRDREC-2
        if (u1IsProduct && u2IsProduct)
        {
          // SPRDREC-2.1
          return MergeProducts(u1.Children, u2.Children);
        }
        if (u1IsProduct)
        {
          // SPRDREC-2.2
          return MergeProducts(u1.Children, new List<ASTNode> { u2 });
        }
        // u2 is a product
        // SPRDREC-2.3
        return MergeProducts(new List<ASTNode> { u1 }, u2.Children);
      }

      // SPRDREC-3
      var restSimplified = SimplifyProductRec(operands.GetRange(1, operands.Count() - 1));

      if (operands[0].IsProduct())
        return MergeProducts(operands[0].Children, restSimplified);
      return MergeProducts(new List<ASTNode> { operands[0] }, restSimplified);
    }

    private List<ASTNode> MergeProducts(List<ASTNode> p, List<ASTNode> q)
    {
      // MPRD-1
      if (q.Count() == 0)
        return p;
      // MPRD-2
      if (p.Count() == 0)
        return q;

      // MPRD-3
      var p1 = p[0];
      var q1 = q[0];
      var h = SimplifyProductRec(new List<ASTNode> { p1, q1 });

      // MPRD-3.1
      if (h.Count() == 0)
        return MergeProducts(p.GetRange(1, p.Count() - 1), q.GetRange(1, q.Count() - 1));
      // MPRD-3.2
      if (h.Count() == 1)
      {
        var merged = MergeProducts(p.GetRange(1, p.Count() - 1), q.GetRange(1, q.Count() - 1));
        merged.Insert(0, h[0]);
        return merged;
      }
      
      // h.Count() must be 2
      // MPRD-3.3
      if (h[0] == p1)
      {
        var merged = MergeProducts(p.GetRange(1, p.Count() - 1), q);
        merged.Insert(0, p1);
        return merged;
      }

      // MPRD-3.4
      var mergeRes = MergeProducts(p, q.GetRange(1, q.Count() - 1));
      mergeRes.Insert(0, q1);
      return mergeRes;
    }

    #endregion
  }
}


