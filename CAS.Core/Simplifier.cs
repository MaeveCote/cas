using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Formats.Asn1;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.XPath;
using CAS.Core.EquationParsing;

namespace CAS.Core
{
  /// <summary>
  /// Provides various functions to simplify algebraic equations.
  /// </summary>
  public class Simplifier
  {
    // Constants
    private const double EPSILON = 0.000001;

    // Fields
    private bool SIMPLIFIER_EVAL_FUNCTIONS;
    private bool USE_RADIANS;


    public Simplifier(bool simplifierEvalFunctions = false, bool useRadians = false)
    {
      SIMPLIFIER_EVAL_FUNCTIONS = simplifierEvalFunctions;
      USE_RADIANS = useRadians;
    }

    /// <summary>
    /// Will make a tree entirely made of rational numbers or decimal numbers. If there is a single decimal number, it will convert the whole tree to decimals.
    /// If you want to opposite, you can signal to convert the whole tree to rationnal by doing an approximate conversion.
    /// </summary>
    /// <remarks>This should be applied after building the AST.</remarks>
    /// <param name="root">The root of the tree.</param>
    /// <param name="applyDecimalToRationalConversion">Wheter to apply the algorithm to approximately convert decimals to fractions.</param>
    public void FormatTree(ASTNode root, bool applyDecimalToRationalConversion = false)
    {
      bool convertToDecimal = FormatTree_Rec(root, applyDecimalToRationalConversion);

      if (convertToDecimal)
      {
        ConvertTreeToDecimal(root);
      }
    }

    /// <summary>
    /// Simplify a rational number into an irreducible fraction or an integer.
    /// </summary>
    /// <param name="input">A rationnal number. Either a fraction or an integer</param>
    /// <returns>An irreducible fraction or interger</returns>
    /// <exception cref="ArgumentException">The input is not a rationnal number.</exception>
    public ASTNode SimplifyRationalNumber(ASTNode input)
    {
      if (input.IsIntegerNum())
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

      // Support for decimal number computations
      if ((powBase.Token.Type is Number numberBase && !powBase.IsIntegerNum()) 
        && powExp.Token.Type is Number numberExp)
      {
        double result = Math.Pow(numberBase.value, numberExp.value);
        return new ASTNode(Token.Number(result.ToString()));
      }

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
      // SPOW-4 special
      if (powExp.Token.Type is Fraction)
        return SimplifyFractionalPower(powBase, powExp);

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
      var recSimplified = SimplifyProduct_Rec(input.Children);

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
      // SSUM-1
      foreach (ASTNode child in input.Children)
      {
        if (child.Token.Type is Undefined)
          return ASTNode.NewUndefined();
      }

      // SSUM-2
      if (input.Children.Count == 1)
        return input.Children[0];

      // SSUM-3
      var recSimplified = SimplifySum_Rec(input.Children);

      if (recSimplified.Count == 0)
        return new ASTNode(Token.Integer("0"));
      if (recSimplified.Count == 1)
        return recSimplified[0];

      return new ASTNode(Token.Operator("+"), recSimplified);
    }

    /// <summary>
    /// Simplifies a quotient of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyQuotient(ASTNode input)
    {
      // Convert the '/' operators to multiplications and powers or fractions
      if (input.Children.All(child => child.Token.Type is Number))
      {
        // This is a fraction
        input.Token = Token.Fraction();
        return SimplifyRNE(input);
      }

      // This is an operation
      input.Token = Token.Operator("*");
      var tempChild = input.Children[1];
      if (tempChild.Token.Type is Number)
      {
        input.Children[1] = new ASTNode(Token.Fraction(), new List<ASTNode> { new ASTNode(Token.Integer("-1"), new List<ASTNode>()), tempChild });
      }
      input.Children[1] = new ASTNode(Token.Operator("^"), new List<ASTNode> { tempChild, new ASTNode(Token.Integer("-1"), new List<ASTNode>()) });

      input.Children[1] = SimplifyPower(input.Children[1]);
      return SimplifyProduct(input);
    }

    /// <summary>
    /// Simplifies a difference of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyDifference(ASTNode input)
    {
      // Convert difference into multiplications and addition
      if (input.Children.Count == 1)
        return SimplifyProduct(new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("-1"), new List<ASTNode>()),
          input.Children[0]}));

      var prod = SimplifyProduct(new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Integer("-1"), new List<ASTNode>()),
          input.Children[1]}));

      return SimplifySum(new ASTNode(Token.Operator("+"), new List<ASTNode> { input.Children[0], prod } ));
    }

    /// <summary>
    /// Simplifies a function of ASAE.
    /// </summary>
    /// <returns>An ASAE</returns>
    public ASTNode SimplifyFunction(ASTNode input)
    {
      foreach (ASTNode operands in input.Children)
      {
        if (operands.Token.Type is Undefined)
          return ASTNode.NewUndefined();
      }

      // Add support for symbols 
      if (!USE_RADIANS && input.Children.Count == 1 && input.Children[0].Token.Type is Number num)
      {
        if (input.Kind() == "sin")
        {
          if (EqualApprox(num, 0.0)) return new ASTNode(Token.Integer("0"));
          if (EqualApprox(num, 30.0)) return new ASTNode(Token.Fraction(), new() { new(Token.Integer("1")), new(Token.Integer("2")) });
          if (EqualApprox(num, 45.0)) return new ASTNode(Token.Number("0.70710678118")); // √2/2
          if (EqualApprox(num, 45.0)) return new ASTNode(Token.Number("0.86602")); // √3/2
          if (EqualApprox(num, 90.0)) return new ASTNode(Token.Integer("1"));
          if (EqualApprox(num, 180.0)) return new ASTNode(Token.Integer("0"));
          if (EqualApprox(num, 270.0)) return new ASTNode(Token.Integer("-1"));
          if (EqualApprox(num, 360.0)) return new ASTNode(Token.Integer("0"));
        }

        else if (input.Kind() == "cos")
        {
          if (EqualApprox(num, 0.0)) return new ASTNode(Token.Integer("1"));
          if (EqualApprox(num, 45.0)) return new ASTNode(Token.Number("0.8660254")); // √3/2
          if (EqualApprox(num, 45.0)) return new ASTNode(Token.Number("0.7071067")); // √2/2
          if (EqualApprox(num, 60.0)) return new ASTNode(Token.Fraction(), new() { new(Token.Integer("1")), new(Token.Integer("2")) });
          if (EqualApprox(num, 90.0)) return new ASTNode(Token.Integer("0"));
          if (EqualApprox(num, 180.0)) return new ASTNode(Token.Integer("-1"));
          if (EqualApprox(num, 270.0)) return new ASTNode(Token.Integer("0"));
          if (EqualApprox(num, 360.0)) return new ASTNode(Token.Integer("1"));
        }

        else if (input.Kind() == "tan")
        {
          if (EqualApprox(num, 0.0)) return new ASTNode(Token.Integer("0"));
          if (EqualApprox(num, 30.0)) return new ASTNode(Token.Number("0.5773502")); // 1/√3
          if (EqualApprox(num, 45.0)) return new ASTNode(Token.Integer("1"));
          if (EqualApprox(num, 60.0)) return new ASTNode(Token.Number("2.7320508")); // √3
          if (EqualApprox(num, 90.0)) return new ASTNode(Token.Undefined());
          if (EqualApprox(num, 180.0)) return new ASTNode(Token.Integer("0"));
          if (EqualApprox(num, 270.0)) return new ASTNode(Token.Undefined());
          if (EqualApprox(num, 360.0)) return new ASTNode(Token.Integer("0"));
        }
      }
      if (input.Kind() == "log")
      {
        if (input.Children.Count() == 1 && input.Children[0].Token.Type is Number b)
        {
          var res = Math.Log(b.value, 10.0);
          if (IsIntegerApprox(res))
            return new ASTNode(Token.Integer((Math.Round(res)).ToString()));
        }
      }
      if (input.Kind() == "ln")
      {
        if (input.Children.Count() == 1 && input.Children[0].Token.Type is Number a)
        {
          var res = Math.Log(a.value);
          if (IsIntegerApprox(res))
            return new ASTNode(Token.Integer((Math.Round(res)).ToString()));
        }
      }

      if (SIMPLIFIER_EVAL_FUNCTIONS)
      {
        if (input.Children.All(child => child.IsConstant()))
        {
          var result = Calculator.Evaluate(input);
          return new ASTNode(Token.Number(result.ToString()));
        }
      }

      return input;
    }

    #region Private methods

    private static bool IsIntegerApprox(Number num)
    {
      return Math.Abs(num.value - Math.Round(num.value)) < EPSILON;
    }

    private static bool IsIntegerApprox(double num)
    {
      return Math.Abs(num - Math.Round(num)) < EPSILON;
    }

    private static bool EqualApprox(Number num, double expected)
    {
      return Math.Abs(num.value - expected) < EPSILON;
    }

    private bool FormatTree_Rec(ASTNode root, bool applyDecimalToRationalConversion)
    {
      // Format the children of the root
      foreach (ASTNode child in root.Children)
      {
        if (FormatTree_Rec(child, applyDecimalToRationalConversion))
          return true;
      }

      if (applyDecimalToRationalConversion)
      {
        if (root.IsConstant())
          ConvertDecimalToRationnal(root);

        return false;
      }

      else
      {
        if (root.Token.Type is Number num)
        {
          if (IsIntegerApprox((Number)root.Token.Type))
          {
            int intValue = (int)num.value;
            root.Token = Token.Integer(intValue.ToString());
          }

          // Need to convert the whole tree to decimal
          return true;
        }
      }

      return false;
    }

    private void ConvertTreeToDecimal(ASTNode root)
    {
      if (root.IsFraction())
      {
        var frac = root.GetNumAndDenum();
        var val = (double)frac[0] / (double)frac[1];
        root.Token = Token.Number(val.ToString());
        root.Children = new List<ASTNode>();
        return;
      }

      // Recursively convert the nodes
      foreach (ASTNode child in root.Children)
        ConvertTreeToDecimal(child);

      if (root.IsIntegerNum())
      {
        root.Token = Token.Number(root.Token.Type.stringValue);
        return;
      }
    }

    private void ConvertDecimalToRationnal(ASTNode number)
    {
      
    }

    private ASTNode SimplifyRNE_Rec(ASTNode input)
    {
      if (input.IsIntegerNum())
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
        if (p.Token.Type is Fraction)
          return SimplifyFractionalPower(r, p);

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

    private ASTNode SimplifyFractionalPower(ASTNode powBase, ASTNode frac)
    {
      // SPOW-1
      if (powBase.Token.Type is Undefined || frac.Token.Type is Undefined)
        return new ASTNode(Token.Undefined());

      // SPOW-2
      if (powBase.Token.Type is IntegerNum num && num.intVal == 0)
      {
        if (frac.Token.Type is Fraction && frac.Children[0].IsPositive())
          return new ASTNode(Token.Integer("0"));
        return new ASTNode(Token.Undefined());
      }

      // SPOW-3
      if (powBase.Token.Type is IntegerNum one && one.intVal == 1)
        return new ASTNode(Token.Integer("1"));

      // SINTPOW-4
      if (powBase.Token.Type.stringValue == "^")
      {
        var r = powBase.Children[0];
        var s = powBase.Children[1];
        var st = SimplifyProduct(new ASTNode(Token.Operator("*"), new() { s, frac }));

        // recurse if possible
        if (st.Token.Type is IntegerNum intSt)
          return SimplifyIntegerPower(r, intSt.intVal);
        if (st.Token.Type is Fraction)
          return SimplifyFractionalPower(r, st);

        return new ASTNode(Token.Operator("^"), new() { r, st });
      }

      // SINTPOW-5
      if (powBase.Token.Type.stringValue == "*")
      {
        var poweredOperands = new List<ASTNode>();
        foreach (var child in powBase.Children)
          poweredOperands.Add(SimplifyFractionalPower(child, frac));

        return SimplifyProduct(new ASTNode(Token.Operator("*"), poweredOperands));
      }

      // SINTPOW-6
      return new ASTNode(Token.Operator("^"), new() { powBase, frac });
    }

    private List<ASTNode> SimplifyProduct_Rec(List<ASTNode> operands)
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
          // Both are not products
          // SPRDREC-1.1
          if (u1.IsConstant() && u2.IsConstant())
          {
            if (u1.IsRational() && u2.IsRational())
            {
              var result = SimplifyRNE(new ASTNode(Token.Operator("*"), operands));
              if (result.Token.Type.stringValue == "1")
                return new List<ASTNode>();

              return new List<ASTNode> { result };
            }

            var res = Calculator.Evaluate(new ASTNode(Token.Operator("*"), operands));
            return new List<ASTNode> { new ASTNode(Token.Number(res.ToString())) };
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
          if (u2 < u1)
            return new List<ASTNode> { u2, u1 };

          // SPRDREC-1.5
          return operands;
        }

        // SPRDREC-2
        // Both are products
        if (u1IsProduct && u2IsProduct)
        {
          // SPRDREC-2.1
          return MergeProducts(u1.Children, u2.Children);
        }

        // One of the is a product
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
      var restSimplified = SimplifyProduct_Rec(operands.GetRange(1, operands.Count() - 1));

      if (operands[0].IsProduct())
        return MergeProducts(operands[0].Children, restSimplified);
      return MergeProducts(new List<ASTNode> { operands[0] }, restSimplified);
    }

    private List<ASTNode> SimplifySum_Rec(List<ASTNode> operands)
    {
      // SSUMREC-1
      if (operands.Count == 2)
      {
        var u1 = operands[0];
        var u2 = operands[1];

        bool u1IsSum = u1.IsSum();
        bool u2IsSum = u2.IsSum();

        if (!u1IsSum && !u2IsSum)
        {
          // Neither is a sum
          // SSUMREC-1.1
          if (u1.IsConstant() && u2.IsConstant())
          {
            if (u1.IsRational() && u2.IsRational())
            {
              var result = SimplifyRNE(new ASTNode(Token.Operator("+"), operands));
              if (result.Token.Type.stringValue == "0")
                return new List<ASTNode>();

              return new List<ASTNode> { result };
            }

            var res = Calculator.Evaluate(new ASTNode(Token.Operator("+"), operands));
            return new List<ASTNode> { new ASTNode(Token.Number(res.ToString())) };
          }

          // SSUMREC-1.2
          if (u1.Token.Type.stringValue == "0")
            return new List<ASTNode> { u2 };
          if (u2.Token.Type.stringValue == "0")
            return new List<ASTNode> { u1 };

          // SPRDREC-1.3
          if (ASTNode.AreLikeTerms(u1, u2))
          {
            // Combine the constants
            var const1 = u1.Const();
            var const2 = u2.Const();

            var combinedConst = SimplifyRNE(new ASTNode(Token.Operator("+"), new() { const1, const2 }));

            if (combinedConst.Token.Type.stringValue == "0")
              return new List<ASTNode> { new ASTNode(Token.Integer("0"), new List<ASTNode>()) };

            var terms = u1.Terms();
            if (combinedConst.Token.Type.stringValue == "1")
            {
              if (terms.Children.Count() == 1)
                return new List<ASTNode> { terms.Children[0] };
              return terms.Children;
            }

            terms.Children.Insert(0, combinedConst);
            return new List<ASTNode> { terms };
          }

          // SPRDREC-1.4
          if (u2 < u1)
            return new List<ASTNode> { u2, u1 };

          // SPRDREC-1.5
          return operands;
        }

        // SPRDREC-2
        if (u1IsSum && u2IsSum)
        {
          // SPRDREC-2.1
          return MergeSums(u1.Children, u2.Children);
        }
        if (u1IsSum)
        {
          // SPRDREC-2.2
          return MergeSums(u1.Children, new List<ASTNode> { u2 });
        }
        // u2 is a sum
        // SPRDREC-2.3
        return MergeSums(new List<ASTNode> { u1 }, u2.Children);
      }

      // SPRDREC-3
      var restSimplified = SimplifySum_Rec(operands.GetRange(1, operands.Count() - 1));

      if (operands[0].IsSum())
        return MergeSums(operands[0].Children, restSimplified);
      return MergeSums(new List<ASTNode> { operands[0] }, restSimplified);
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
      var h = SimplifyProduct_Rec(new List<ASTNode> { p1, q1 });

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

    private List<ASTNode> MergeSums(List<ASTNode> p, List<ASTNode> q)
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
      var h = SimplifySum_Rec(new List<ASTNode> { p1, q1 });

      // MPRD-3.1
      if (h.Count() == 0)
        return MergeSums(p.GetRange(1, p.Count() - 1), q.GetRange(1, q.Count() - 1));
      // MPRD-3.2
      if (h.Count() == 1)
      {
        var merged = MergeSums(p.GetRange(1, p.Count() - 1), q.GetRange(1, q.Count() - 1));
        merged.Insert(0, h[0]);
        return merged;
      }

      // h.Count() must be 2
      // MPRD-3.3
      if (h[0] == p1)
      {
        var merged = MergeSums(p.GetRange(1, p.Count() - 1), q);
        merged.Insert(0, p1);
        return merged;
      }

      // MPRD-3.4
      var mergeRes = MergeSums(p, q.GetRange(1, q.Count() - 1));
      mergeRes.Insert(0, q1);
      return mergeRes;
    }

    #endregion
  }
}


