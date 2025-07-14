using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Formats.Asn1;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Xml;
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
    private bool APPLY_DECIMAL_2_RATIONAL_CONVERSION;
    private int MAX_APPROX_ITERATIONS;

    /// <summary>
    /// Constructs a mew Simplifier.
    /// </summary>
    /// <param name="simplifierEvalFunctions">Apply function evaluation if possible when simplifyina.</param>
    /// <param name="useRadians">Use radians in trigonometric simplifying.</param>
    public Simplifier(bool simplifierEvalFunctions = false, bool useRadians = false, bool applyDecimalToRationalConversion = false, int maxApproxIterations = 30)
    {
      SIMPLIFIER_EVAL_FUNCTIONS = simplifierEvalFunctions;
      USE_RADIANS = useRadians;
      APPLY_DECIMAL_2_RATIONAL_CONVERSION = applyDecimalToRationalConversion;
      MAX_APPROX_ITERATIONS = maxApproxIterations;
    }

    public void SetSimplifierEvalFunctions(bool val) => SIMPLIFIER_EVAL_FUNCTIONS = val;
    public void SetUseRadians(bool val) => USE_RADIANS = val;
    public void SetApplyDecimal2RationnalConverstion(bool val) => APPLY_DECIMAL_2_RATIONAL_CONVERSION = val;

    /// <summary>
    /// Will make a tree entirely made of rational numbers or decimal numbers. If there is a single decimal number, it will convert the whole tree to decimals.
    /// If you want to opposite, you can signal to convert the whole tree to rationnal by doing an approximate conversion.
    /// </summary>
    /// <remarks>This should be applied after building the AST.</remarks>
    /// <param name="root">The root of the tree.</param>
    /// <param name="applyDecimalToRationalConversion">Wheter to apply the algorithm to approximately convert decimals to fractions.</param>
    public void FormatTree(ASTNode root)
    {
      bool convertToDecimal = FormatTree_Rec(root, APPLY_DECIMAL_2_RATIONAL_CONVERSION);

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
    /// Also performs a polynomial factorization at the end.
    /// </summary>
    /// <param name="input">A BAE</param>
    /// <returns>An ASAE</returns>
    public ASTNode AutomaticSimplify(ASTNode input, bool simplifyPolynomials = true)
    {
      ASTNode result;
      if (input.Token.Type is Number || input.Token.Type is Variable)
        result = input;
      else if (input.Token.Type is Fraction)
        result =  SimplifyRationalNumber(input);
      else
      {
        // Simplify the nodes recursively
        for (int i = 0; i < input.Children.Count(); i++)
          input.Children[i] = AutomaticSimplify(input.Children[i], false);

        if (input.Kind() == "^")
          result = SimplifyPower(input);
        else if (input.Kind() == "*")
          result = SimplifyProduct(input);
        else if (input.Kind() == "+")
          result = SimplifySum(input);
        else if (input.Kind() == "-")
          result = SimplifyDifference(input);
        else if (input.Kind() == "/")
          result = SimplifyQuotient(input);
        else if (input.Token.Type is Function)
          result = SimplifyFunction(input);
        else
          result = ASTNode.NewUndefined();
      }

      if (simplifyPolynomials)
      {
        var variables = result.GetVariables();
        if (variables.Count == 1)
          return PolynomialSimplify(result, new ASTNode(Token.Variable(variables.First().stringValue)));
      }

      return result;
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

      return SimplifySum(new ASTNode(Token.Operator("+"), new List<ASTNode> { input.Children[0], prod }));
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

    /// <summary>
    /// Expands the mutiplications and powers of the tree rooted at input and applies <see cref="Simplifier.AutomaticSimplify(ASTNode)"/> after.
    /// </summary>
    /// <remarks>This algorithm is in place but will work with a copy of the input.</remarks>
    /// <param name="input">The root of the tree to expand.</param>
    public ASTNode Expand(ASTNode input)
    {
      // Create a copy and remove differences before expanding
      var root = new ASTNode(RemoveDifferences(input));
      ExpandInPlace(root);

      // Simplify the resulting tree.
      return AutomaticSimplify(root, false);
    }

    /// <summary>
    /// Implements the SimplifyDifference of AutomaticSimplify only, on the whole tree./
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public ASTNode RemoveDifferences(ASTNode input)
    {
      // Simplify the nodes recursively
      for (int i = 0; i < input.Children.Count(); i++)
        input.Children[i] = RemoveDifferences(input.Children[i]);

      if (input.Kind() == "-")
        return SimplifyDifference(input);

      return input;
    }

    /// <summary>
    /// Implements the SimplifyQuotient of AutomaticSimplify only, on the whole tree.
    /// </summary>
    /// <returns>A tree without differences.</returns>
    public ASTNode RemoveQuotients(ASTNode input)
    {
      // Simplify the nodes recursively
      for (int i = 0; i < input.Children.Count(); i++)
        input.Children[i] = RemoveQuotients(input.Children[i]);

      if (input.Kind() == "/")
        return RemoveQuotients(input);

      return input;
    }

    #region Polynomials

    /// <summary>
    /// Computes the polynomial division of u / v.
    /// </summary>
    /// <param name="u">A GPE</param>
    /// <param name="v">A GPE</param>
    /// <param name="x">The polynomial variable</param>
    /// <returns>An array of thw quotien 'q' and remainder 'r': [q, r]</returns>
    public ASTNode[] PolynomialDivision(ASTNode u, ASTNode v, ASTNode x)
    {
      if (v.Token.Type is IntegerNum num && num.intVal == 0)
        throw new DivideByZeroException("Can't divide a polynomial by 0.");

      ASTNode q = new ASTNode(Token.Integer("0"));
      var r = u;
      var m = r.DegreeGPE(x);
      var n = v.DegreeGPE(x);
      var lc_v = v.LeadingCoefficient(x);

      while (m >= n)
      {
        var lc_r = r.LeadingCoefficient(x);
        var s = AutomaticSimplify(new ASTNode(Token.Operator("/"), new List<ASTNode> { lc_r, lc_v }), false);

        var mMinusN = new ASTNode(Token.Integer((m - n).ToString()));
        q = AutomaticSimplify(new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          q, new ASTNode(Token.Operator("*"), new List<ASTNode>{ 
            s, new ASTNode(Token.Operator("^"), new List<ASTNode> {
              new ASTNode(x), new ASTNode(mMinusN) }) })
        }), false);

        var mNode = new ASTNode(Token.Integer(m.ToString()));
        var nNode = new ASTNode(Token.Integer(n.ToString()));

        var xPowM = new ASTNode(Token.Operator("^"), new List<ASTNode> { x, mNode });
        var lcr_xm = new ASTNode(Token.Operator("*"), new List<ASTNode> { lc_r, xPowM });
        var leftTerm = new ASTNode(Token.Operator("-"), new List<ASTNode> { r, lcr_xm });

        var xPowN = new ASTNode(Token.Operator("^"), new List<ASTNode> { x, nNode });
        var lcv_xn = new ASTNode(Token.Operator("*"), new List<ASTNode> { lc_v, xPowN });
        var rightInner = new ASTNode(Token.Operator("-"), new List<ASTNode> { v, lcv_xn });

        var xPowMMinusN = new ASTNode(Token.Operator("^"), new List<ASTNode> { x, mMinusN });

        var rightProduct = new ASTNode(Token.Operator("*"), new List<ASTNode> { rightInner, s, xPowMMinusN });

        r = Expand(new ASTNode(Token.Operator("-"), new List<ASTNode> { leftTerm, rightProduct }));
        m = r.DegreeGPE(x);
      }

      return new ASTNode[] { q, r };
    }

    /// <summary>
    /// Computes the polynomial expansion resulting polynomial from the quotients of u / v.
    /// </summary>
    /// <param name="u">A GPE</param>
    /// <param name="v">A GPE</param>
    /// <param name="x">u and v polynomial variable</param>
    /// <param name="t">The resulting polynomial variable</param>
    /// <returns></returns>
    public ASTNode PolynomialExpansion(ASTNode u, ASTNode v, ASTNode x, ASTNode t)
    {
      if (u.Token.Type is IntegerNum num && num.intVal == 0)
        return new ASTNode(Token.Integer("0"));

      var d = PolynomialDivision(u, v, x);

      return Expand(new ASTNode(Token.Operator("+"), new List<ASTNode> {
        new ASTNode(Token.Operator("*"), new List<ASTNode> { new ASTNode(t), PolynomialExpansion(d[0], v, x, t) }),
        d[1]}));
    }

    /// <summary>
    /// Factors a polynomial. This method only factors polynomials of degree 2, 3, 4 or pulling common factors out.
    /// </summary>
    /// <remarks>This is done over the reals.</remarks>
    /// <remarks>This method assumes the input is a polynomial of single variable.</remarks>
    /// <param name="poly">A GPE</param>
    /// <param name="x">The variable (simple variable)</param>
    /// <returns>The factored polynomial or the input if it is not factorizable.</returns>
    public ASTNode PolynomialFactorization(ASTNode poly, ASTNode x)
    {
      var expandedPoly = Expand(poly);

      // Validate that "x" is a variable and that it is the only one in the polynomial
      var variableSet = expandedPoly.GetVariables();
      if (!(x.Token.Type is Variable v) || !variableSet.Contains(v) || variableSet.Count() != 1)
        return poly;

      // 1: Get the common factor out if there is one
      var commonFactorRes = PolynomialPullCommonFactor(expandedPoly, x);

      // 2: Factor the polynomial if the degree allows it
      // 2nd degree factorization is the only case supported for now. 3rd and 4th requires complex arithmetic, which is not supported.
      var insidePoly = commonFactorRes[1];
      if (insidePoly == null)
        throw new Exception("Unexpected null value, the common factor pull out should not return a null factored polynomial.");
      int degree = insidePoly.DegreeGPE(x);

      if (degree == 2)
      {
        var factor2ndDeg = Polynomial2ndDegreeFactorization(insidePoly, x);
        insidePoly = new ASTNode(Token.Operator("*"), factor2ndDeg);
      }

      ASTNode result = new ASTNode(Token.Operator("*"));
      if (commonFactorRes[0] != null)
        result.Children = [commonFactorRes[0], insidePoly];
      else
        result.Children = [insidePoly];

      return AutomaticSimplify(result, false);
    }

    /// <summary>
    /// Factors all the polynomials inside the given input.
    /// </summary>
    /// <param name="input">A mathematical equation in a single variable</param>
    /// <param name="x">The polynomial variable</param>
    /// <returns>The factored equation.</returns>
    public ASTNode PolynomialSimplify(ASTNode input, ASTNode x)
    {
      if (input.Token.Type is Undefined)
        return input;

      if (input.IsPolynomialGPE(x, false))
        return AutomaticSimplify(PolynomialFactorization(input, x), false);

      // Simplify the children if they are polynomials
      var result = new ASTNode(input);
      result.Children = new List<ASTNode>();
      foreach (var child in input.Children)
        result.Children.Add(PolynomialSimplify(child, x));

      return AutomaticSimplify(result, false);
    }

    #endregion

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
        {
          var frac = ConvertDecimalToRationnal(root);
          root.Token = frac.Token;
          root.Children = frac.Children;
        }

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
            return false;
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

    private ASTNode ConvertDecimalToRationnal(ASTNode number, double accuracy = EPSILON)
    {
      double value = ((Number)number.Token.Type).value;

      // Split value in a sign, an integer part, a fractional part
      int sign = value < 0 ? -1 : 1;
      value = value < 0 ? -value : value;
      int integerpart = (int)value;
      value -= integerpart;

      // check if the fractional part is near 0
      double minimalvalue = value - accuracy;
      if (minimalvalue < 0.0)
        return new ASTNode(Token.Fraction(), new List<ASTNode> {
          new ASTNode(Token.Integer((sign * integerpart).ToString())),
          new ASTNode(Token.Integer("1")) });

      // check if the fractional part is near 1
      double maximumvalue = value + accuracy;
      if (maximumvalue > 1.0)
        return new ASTNode(Token.Fraction(), new List<ASTNode> {
          new ASTNode(Token.Integer((sign * (integerpart + 1)).ToString())),
          new ASTNode(Token.Integer("1")) });

      // The lower fraction is 0/1
      int lower_numerator = 0;
      int lower_denominator = 1;

      // The upper fraction is 1/1
      int upper_numerator = 1;
      int upper_denominator = 1;

      while (true)
      {
        // The middle fraction is (lower_numerator + upper_numerator) / (lower_denominator + upper_denominator)
        int middle_numerator = lower_numerator + upper_numerator;
        int middle_denominator = lower_denominator + upper_denominator;

        if (middle_denominator * maximumvalue < middle_numerator)
        {
          // real + error < middle : middle is our new upper
          upper_numerator = middle_numerator;
          upper_denominator = middle_denominator;
        }
        else if (middle_numerator < minimalvalue * middle_denominator)
        {
          // middle < real - error : middle is our new lower
          lower_numerator = middle_numerator;
          lower_denominator = middle_denominator;
        }
        else
        {
          return new ASTNode(Token.Fraction(), new List<ASTNode> {
            new ASTNode(Token.Integer((sign * (integerpart * middle_denominator + middle_numerator)).ToString())),
            new ASTNode(Token.Integer(middle_denominator.ToString())) });
        }
      }
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
        var p = SimplifyProduct(new ASTNode(Token.Operator("*"), new List<ASTNode> { s, new ASTNode(Token.Integer(exp.ToString())) }));

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

      if (powBase.Token.Type is IntegerNum num)
      {
        var numDenum = Calculator.GetNumAndDenum(frac);
        // SPOW-2
        if (num.intVal == 0)
        {
          if (frac.Token.Type is Fraction && frac.Children[0].IsPositive())
            return new ASTNode(Token.Integer("0"));
          return new ASTNode(Token.Undefined());
        }
        // SPOW-3
        if (num.intVal == 1)
          return new ASTNode(Token.Integer("1"));

        // ADDED Undefine even roots of negative
        if (num.intVal < 0 && numDenum[1] % 2 == 0)
          return ASTNode.NewUndefined();

        // ADDED Simplify perfect exponent
        if (numDenum[0] == 1)
        {
          double root = Math.Pow(num.intVal, (double)numDenum[0] / (double)numDenum[1]);
          if (IsIntegerApprox(root))
            return new ASTNode(Token.Integer((Math.Round(root)).ToString()));
          int[]? compositeRoot = GetCompositeRoot(num.intVal, numDenum[1], MAX_APPROX_ITERATIONS);
          if (compositeRoot != null)
          {
            return new ASTNode(Token.Operator("*"), new List<ASTNode>
            {
              new ASTNode(Token.Integer(Math.Round(Math.Pow(compositeRoot[0], (double)numDenum[0] / (double)numDenum[1])).ToString())),
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                new ASTNode(Token.Integer(compositeRoot[1].ToString())),
                new ASTNode(frac)
              })
            });
          }
        }
      }


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
              return new List<ASTNode>();

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

    private void ExpandInPlace(ASTNode root)
    {
      // Expand the children
      foreach (ASTNode child in root.Children)
        ExpandInPlace(child);

      // Expand this node.
      if (root.IsProduct())
        ExpandProduct(root);
      if (root.IsPower())
        ExpandPower(root);
    }
    
    private void ExpandProduct(ASTNode root)
    {
      // Return if a unary product
      if (root.Children.Count() == 1)
      {
        root.Token = root.Children[0].Token;
        root.Children = root.Children[0].Children;
        return;
      }

      // Find if there is an addtion, otherwise do nothing
      for (int i = 0; i < root.Children.Count(); i++)
      {
        if (root.Children[i].IsSum())
        {
          // Create new addition
          ASTNode subAddition = new ASTNode(Token.Operator("+"));

          // Add a product for each sub operand in the addition
          foreach (ASTNode operand in root.Children[i].Children)
          {
            ASTNode subProduct = new ASTNode(Token.Operator("*"), new List<ASTNode> { operand });
            for (int j = 0; j < root.Children.Count; j++)
            {
              // Add the other operand other than this addition
              if (j == i) continue;
              subProduct.Children.Add(new ASTNode(root.Children[j]));
            }

            // Expand the new product
            ExpandProduct(subProduct);

            subAddition.Children.Add(subProduct);
          }

          // Set the addition to the previous product
          root.Children = new List<ASTNode> { subAddition };
          return;
        }
      }
    }

    private void ExpandPower(ASTNode root)
    {
      var powBase = root.Children[0];
      var powExp = root.Children[1];

      if (powExp.Token.Type is IntegerNum intNum && intNum.intVal > 0 &&
        (powBase.Kind() == "+" || powBase.Kind() == "-"))
      {
        // Create a product of 'intNum' times the base.
        var newChildren = new List<ASTNode>();
        for (int i = 0; i < intNum.intVal; i++)
          newChildren.Add(new ASTNode(powBase));

        root.Token = Token.Operator("*");
        root.Children = newChildren;

        ExpandProduct(root);
      }
    }

    // returns [power of, remainder]
    private int[]? GetCompositeRoot(int a, int b, int maxIterations)
    {
      int[]? result = null;
      int A = a;
      for (int i = 0; i < maxIterations; i++)
      {
        var inter = GetCompositeRoot_Rec(A, b);
        if (inter == null)
          return result;

        A /= (int)inter;

        if (result == null)
          result = [(int)inter, A];
        else
          result = [result[0] * (int)inter, A];
      }
      return result;
    }

    private int? GetCompositeRoot_Rec(int a, int b)
    {
      for (int i = 2; i * i < a; i++)
      {
        int B = (int)Math.Pow(i, b);
        while (B <= a)
        {
          if (IsIntegerApprox((double)a / B))
            return B;
          B = B * B;
        }
      }
      return null;
    }

    // Returns [common factor, factored polynomial]
    private List<ASTNode?> PolynomialPullCommonFactor(ASTNode poly, ASTNode x)
    {
      List<int>? constants = new List<int>();
      int smallestDegree = int.MaxValue;
      if (poly.Token.Type is Operator op && op.stringValue == "+" && poly.Children.Count() > 1)
      {
        foreach (var operand in poly.Children)
        {
          var constant = operand.Const();
          if (constants != null)
          {
            if (constant.Token.Type is IntegerNum num)
              constants.Add(num.intVal);
            else
              constants = null;
          }

          int deg = operand.DegreeGPE(x);
          if (deg < smallestDegree)
            smallestDegree = deg;
        }

        ASTNode? constantPullOut = null;
        int? gcd = null;
        if (constants != null)
        {
          gcd = Calculator.SetGCD(constants);

          if (gcd != 1)
            constantPullOut = new ASTNode(Token.Integer(gcd.ToString()));
        }

        ASTNode? variablePullOut = null;
        if (smallestDegree != 0)
        {
          if (smallestDegree == 1)
            variablePullOut = new ASTNode(Token.Variable("x"));
          else
            variablePullOut = new ASTNode(Token.Operator("^"), new List<ASTNode>
            {
              new ASTNode(Token.Variable("x")),
              new ASTNode(Token.Integer(smallestDegree.ToString()))
            });
        }

        ASTNode? productPullOut = null;
        if (variablePullOut != null && constantPullOut != null)
          productPullOut = new ASTNode(Token.Operator("*"), new List<ASTNode> { constantPullOut, variablePullOut });
        else if (variablePullOut != null)
          productPullOut = variablePullOut;
        else if (constantPullOut != null)
          productPullOut = constantPullOut;

        if (productPullOut == null)
          return [null, poly];

        // Reduce poly
        ASTNode newPoly = new ASTNode(Token.Operator("+"));
        foreach (var operand in poly.Children)
        {
          var constant = operand.Const();
          if (gcd != null)
          {
            int newVal = ((IntegerNum)constant.Token.Type).intVal / (int)gcd;
            constant = new ASTNode(Token.Integer(newVal.ToString()));
          }

          int deg = operand.DegreeGPE(x);
          deg -= smallestDegree;

          var newChild = new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            constant,
            new ASTNode(Token.Operator("^"), new List<ASTNode>
            {
              x,
              new ASTNode(Token.Integer(deg.ToString()))
            })
          });

          newChild = AutomaticSimplify(newChild, false);
          newPoly.Children.Add(newChild);
        }

        return [productPullOut, newPoly];
      }
      else
        return [ null, poly ];
    }
    
    private List<ASTNode> Polynomial2ndDegreeFactorization(ASTNode poly, ASTNode x)
    {
      ASTNode a = new ASTNode(Token.Integer("0"));
      ASTNode b = new ASTNode(Token.Integer("0"));
      ASTNode c = new ASTNode(Token.Integer("0"));

      if (poly.Token.Type.stringValue == "^")
      {
        int deg = poly.DegreeGPE(x);
        if (deg == 0)
          c = poly.Const();
        else if (deg == 1)
          b = poly.Const();
        else if (deg == 2)
          a = poly.Const();
        else
          throw new ArgumentException("'poly' is not a 2nd degree polynomial.");
      }

      foreach (var operand in poly.Children)
      {
        int deg = operand.DegreeGPE(x);
        if (deg == 0)
          c = operand.Const();
        else if (deg == 1)
          b = operand.Const();
        else if (deg == 2)
          a = operand.Const();
        else
          throw new ArgumentException("'poly' is not a 2nd degree polynomial.");
      }

      var discriminant = AutomaticSimplify(new ASTNode(Token.Operator("-"), new List<ASTNode> 
      {
        new ASTNode(Token.Operator("^"), new List<ASTNode> {b, new ASTNode(Token.Integer("2"))}),
        new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("4")),
          a,
          c
        })
      }), false);

      if (discriminant.Token.Type is Undefined)
        throw new ArgumentException("'poly' is invalid, it's discriminant is undefined.");
      double value = discriminant.EvaluateAsDouble();
      if (value < 0)
      {
        // No roots
        return [poly];
      }
      else if (value == 0)
      {
        // Repeated root
        var root = AutomaticSimplify(new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode> { b }),
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("2")),
            a
          })
        }), false);
        return [AutomaticSimplify(new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode> {
            new ASTNode(x),
            root
          }),
          new ASTNode(Token.Integer("2"))
        }), false)];
      }
      else
      {
        // Two distinct roots
        var root1 = AutomaticSimplify(new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("-"), new List<ASTNode> { b }),
            new ASTNode(Token.Operator("^"), new List<ASTNode>
            {
              discriminant,
              new ASTNode(Token.Fraction(), new List<ASTNode> { new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2")) })
            })
          }),
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("2")),
            a
          })
        }), false);
        var root2 = AutomaticSimplify(new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("-"), new List<ASTNode> { b }),
            new ASTNode(Token.Operator("^"), new List<ASTNode>
            {
              discriminant,
              new ASTNode(Token.Fraction(), new List<ASTNode> { new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2")) })
            })
          }),
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("2")),
            a
          })
        }), false);

        var linear1 = AutomaticSimplify(new ASTNode(Token.Operator("-"), new List<ASTNode>
        {
          new ASTNode(x),
          root1
        }), false);
        var linear2 = AutomaticSimplify(new ASTNode(Token.Operator("-"), new List<ASTNode>
        {
          new ASTNode(x),
          root2
        }), false);

        return [linear1, linear2];
      }
    }

    #endregion
  }
}


