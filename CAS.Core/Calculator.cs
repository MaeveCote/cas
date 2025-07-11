using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using CAS.Core.EquationParsing;

namespace CAS.Core
{
  /// <summary>
  /// Does operations on ASTs.
  /// </summary>
  public static class Calculator
  {
    /// <summary>
    /// Evaluates an AST to find the result of the equation represented by it. 
    /// Replaces the values of the variables by the values found in the symbol table.
    /// </summary>
    /// <remarks>Uses a Depth First Search to traverse the tree.</remarks>
    /// <param name="root">The root of the AST representing the equation.</param>
    /// <param name="symbolTable">The symbol table to associate variables with values.</param>
    /// <exception cref="KeyNotFoundException">A variable in the equation is not found in the symbol table.</exception>
    /// <returns>The result of the equation</returns>
    public static double Evaluate(ASTNode root, Dictionary<string, double>? symbolTable = null,
      Dictionary<string, Func<List<double>, double>>? customFunctionTable = null)
    {
      // Default variables trick
      symbolTable ??= new Dictionary<string, double>();
      customFunctionTable ??= new Dictionary<string, Func<List<double>, double>>();

      // A Dictionnary that converts string functions to evaluate it.
      var functionTable = new Dictionary<string, Func<List<ASTNode>, Dictionary<string, double>, Dictionary<string, Func<List<double>, double>>, double>>
      {
          { "abs", (args, symTable, customFunc) => Math.Abs(Evaluate(args[0], symTable, customFunc)) },
          { "sign", (args, symTable, customFunc) => Math.Sign(Evaluate(args[0], symTable, customFunc)) },
          { "sqrt", (args, symTable, customFunc) => Math.Sqrt(Evaluate(args[0], symTable, customFunc)) },
          { "ln", (args, symTable, customFunc) => Math.Log(Evaluate(args[0], symTable, customFunc)) },
          { "log", (args, symTable, customFunc) =>
              {
                  if (args.Count == 2)
                      return Math.Log(Evaluate(args[1], symTable, customFunc), Evaluate(args[0], symTable, customFunc)); // log(base, value)
                  else
                      return Math.Log10(Evaluate(args[0], symTable, customFunc)); // default to base 10
              }
          },
          { "exp", (args, symTable, customFunc) => Math.Exp(Evaluate(args[0], symTable, customFunc)) },
          { "sin", (args, symTable, customFunc) => Math.Sin(Evaluate(args[0], symTable, customFunc)) },
          { "cos", (args, symTable, customFunc) => Math.Cos(Evaluate(args[0], symTable, customFunc)) },
          { "tan", (args, symTable, customFunc) => Math.Tan(Evaluate(args[0], symTable, customFunc)) },
          { "asin", (args, symTable, customFunc) => Math.Asin(Evaluate(args[0], symTable, customFunc)) },
          { "acos", (args, symTable, customFunc) => Math.Acos(Evaluate(args[0], symTable, customFunc)) },
          { "atan", (args, symTable, customFunc) => Math.Atan(Evaluate(args[0], symTable, customFunc)) },
          { "floor", (args, symTable, customFunc) => Math.Floor(Evaluate(args[0], symTable, customFunc)) },
          { "ceil", (args, symTable, customFunc) => Math.Ceiling(Evaluate(args[0], symTable, customFunc)) },
          { "round", (args, symTable, customFunc) => Math.Round(Evaluate(args[0], symTable, customFunc)) },
          { "min", (args, symTable, customFunc) => args.Select(arg => Evaluate(arg, symTable, customFunc)).Min() },
          { "max", (args, symTable, customFunc) => args.Select(arg => Evaluate(arg, symTable, customFunc)).Max() }
      };

      if (root.Token.Type is Number)
        return double.Parse(root.Token.Type.stringValue);

      if (root.Token.Type is Fraction)
      {
        double num = double.Parse(root.Children[0].Token.Type.stringValue);
        double denum = double.Parse(root.Children[1].Token.Type.stringValue);

        return num / denum;
      }

      if (root.Token.Type is Variable)
        return symbolTable[root.Token.Type.stringValue];

      if (root.Token.Type is Function)
      {
        string funcName = root.Token.Type.stringValue;
        try
        {
          // Use the function table to find the function.
          double funcResult = functionTable[funcName](root.Children, symbolTable, customFunctionTable);
          return funcResult;
        }
        catch (KeyNotFoundException e)
        {
          // Use the custom function table if not found.
          double funcResult = customFunctionTable[funcName](EvaluateArgs(root.Children, symbolTable, customFunctionTable));
          return funcResult;
        }
      }

      if (root.Token.Type is Operator)
      {
        List<double> resultValues = new List<double>();
        foreach (ASTNode node in root.Children)
          resultValues.Add(Evaluate(node, symbolTable, customFunctionTable));

        return root.Token.Type.stringValue switch
        {
          "+" => ComputeAddition(resultValues),
          "-" => ComputeSubstraction(resultValues),
          "*" => ComputeMultiplication(resultValues),
          "/" => ComputeDivision(resultValues),
          "^" => ComputePower(resultValues),
          _ => throw new InvalidOperationException("Unknown operator")
        };
      }

      throw new InvalidOperationException("Unsupported token type, the node is not evaluable.");
    }

    public static int Remainder(int a, int b)
    {
      return ((a % b) + Math.Abs(b)) % Math.Abs(b);
    }

    public static int Quotient(int a, int b)
    {
      return (a - Remainder(a, b)) / b;
    }

    /// <summary>
    /// Computes the Greatest Common Divisor of a and b using Euclid's algorithm.
    /// </summary>
    public static int GCD(int a, int b)
    {
      int A = a;
      int B = b;
      int R;

      while (B != 0)
      {
        R = Remainder(A, B);
        A = B;
        B = R;
      }

      return Math.Abs(A);
    }

    /// <summary>
    /// Computes the GCD of the set of numbers in nums.
    /// </summary>
    /// <exception cref="ArgumentException">Cannot compute the GCD of an empty set.</exception>
    public static int SetGCD(List<int> nums)
    {
      int? currentGcd = null;
      foreach (var num in nums)
      {
        if (currentGcd == null)
          currentGcd = num;
        else
          currentGcd = GCD(currentGcd.Value, num);
      }

      //
      if (currentGcd == null)
        throw new ArgumentException("Cannot compute GCD of an empty set.");

      return currentGcd.Value;
    }

    /// <summary>
    /// Evaluates the product of two rationnal numbers.
    /// </summary>
    /// <returns>A rationnal number</returns>
    public static ASTNode EvaluateProductRationnal(ASTNode leftNode, ASTNode rightNode)
    {
      int[] leftFrac = GetNumAndDenum(leftNode);
      int[] rightFrac = GetNumAndDenum(rightNode);

      int resultNum = (leftFrac[0] * rightFrac[0]);
      int resultDenum = (leftFrac[1] * rightFrac[1]);

      if (resultDenum == 0)
        return ASTNode.NewUndefined();
      else if (resultDenum == 1)
        return new ASTNode(Token.Integer(resultNum.ToString()));

      return new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer(resultNum.ToString())),
        new ASTNode(Token.Integer(resultDenum.ToString()))
      });
    }

    /// <summary>
    /// Evaluates the quotient of two rationnal numbers.
    /// </summary>
    /// <returns>A rationnal number</returns>
    public static ASTNode EvaluateQuotientRationnal(ASTNode leftNode, ASTNode rightNode)
    {
      int[] leftFrac = GetNumAndDenum(leftNode);
      int[] rightFrac = GetNumAndDenum(rightNode);

      int resultNum = (leftFrac[0] * rightFrac[1]);
      int resultDenum = (leftFrac[1] * rightFrac[0]);

      if (rightFrac[1] == 0)
        return ASTNode.NewUndefined();
      else if (resultDenum == 0)
        return ASTNode.NewUndefined();
      else if (resultDenum == 1)
        return new ASTNode(Token.Integer(resultNum.ToString()));

      return new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer(resultNum.ToString())),
        new ASTNode(Token.Integer(resultDenum.ToString()))
      });
    }

    /// <summary>
    /// Evaluates the sum of two rationnal numbers.
    /// </summary>
    /// <returns>A rationnal number</returns>
    public static ASTNode EvaluateSumRationnal(ASTNode leftNode, ASTNode rightNode)
    {
      int[] leftFrac = GetNumAndDenum(leftNode);
      int[] rightFrac = GetNumAndDenum(rightNode);

      int leftNum = leftFrac[0] * rightFrac[1];
      int leftDenum = leftFrac[1] * rightFrac[1];
      int rightNum = rightFrac[0] * leftFrac[1];

      int resultNum = (leftNum + rightNum);
      int resultDenum = leftDenum;

      if (resultDenum == 0)
        return ASTNode.NewUndefined();
      else if (resultDenum == 1)
        return new ASTNode(Token.Integer(resultNum.ToString()));

      return new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer(resultNum.ToString())),
        new ASTNode(Token.Integer(resultDenum.ToString()))
      });
    }

    /// <summary>
    /// Evaluates the sum of two rationnal numbers. Left - Right.
    /// </summary>
    /// <returns>A rationnal number</returns>
    public static ASTNode EvaluateDiffRationnal(ASTNode leftNode, ASTNode rightNode)
    {
      int[] leftFrac = GetNumAndDenum(leftNode);
      int[] rightFrac = GetNumAndDenum(rightNode);

      int leftNum = leftFrac[0] * rightFrac[1];
      int leftDenum = leftFrac[1] * rightFrac[1];
      int rightNum = rightFrac[0] * leftFrac[1];

      int resultNum = (leftNum - rightNum);
      int resultDenum = leftDenum;

      if (resultDenum == 0)
        return new ASTNode(Token.Undefined());
      else if (resultDenum == 1)
        return new ASTNode(Token.Integer(resultNum.ToString()));

      return new ASTNode(Token.Fraction(), new List<ASTNode>
      {
        new ASTNode(Token.Integer(resultNum.ToString())),
        new ASTNode(Token.Integer(resultDenum.ToString()))
      });
    }

    /// <summary>
    /// Evaluates the power of a rationnal number to an integer exponent.
    /// </summary>
    /// <returns>A rationnal number</returns>
    public static ASTNode EvaluatePowerRationnal(ASTNode baseNum, ASTNode exponent)
    {
      int[] baseFrac = GetNumAndDenum(baseNum);
      int exponentInt = 0;

      if (baseFrac[1] == 0)
        return ASTNode.NewUndefined();

      if (exponent.Token.Type is IntegerNum expInt)
        exponentInt = expInt.intVal;
      else
        throw new ArgumentException("The 'exponent' should be an integer.");

      if (baseFrac[0] != 0)
      {
        if (exponentInt >= 0)
        {
          int[] recPow = EvaluatePowerRationnalRec(baseFrac, exponentInt - 1);
          int[] result = EvaluateProductRationnalRec(recPow, baseFrac);

          if (result[1] == 1)
            return new ASTNode(Token.Integer(result[0].ToString()));

          return new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer(result[0].ToString())),
            new ASTNode(Token.Integer(result[1].ToString()))
          });
        }
        else if (exponentInt == 0)
          return new ASTNode(Token.Integer("1"));
        else if (exponentInt == -1)
          return new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer(baseFrac[1].ToString())),
            new ASTNode(Token.Integer(baseFrac[0].ToString()))
          });
        else
        {
          int[] inverse = new int[] { baseFrac[1], baseFrac[0] };
          int[] result = EvaluatePowerRationnalRec(inverse, -exponentInt);

          if (result[1] == 1)
            return new ASTNode(Token.Integer(result[0].ToString()));

          return new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer(result[0].ToString())),
            new ASTNode(Token.Integer(result[1].ToString()))
          });
        }
      }
      else
      {
        if (exponentInt >= 1)
          return new ASTNode(Token.Integer("0"));

        return new ASTNode(Token.Undefined());
      }

    }

    /// <summary>
    /// Converts a rational number node to an array numerator and denumerator.
    /// </summary>
    /// <returns>Int array of [numerator, denumerator]</returns>
    /// <exception cref="ArgumentException">The given node is not a rational number</exception>
    public static int[] GetNumAndDenum(ASTNode node)
    {
      int[] frac = new int[2];
      if (node.Token.Type is IntegerNum nodeInt)
      {
        frac[0] = nodeInt.intVal;
        frac[1] = 1;
      }
      else if (node.Token.Type is Fraction)
      {
        frac[0] = ((IntegerNum)node.OperandAt(0).Token.Type).intVal;
        frac[1] = ((IntegerNum)node.OperandAt(1).Token.Type).intVal;
      }
      else
        throw new ArgumentException("The 'node' should be a rationnal number");

      return frac;
    }

    #region Private methods

    private static List<double> EvaluateArgs(List<ASTNode> args, Dictionary<string, double> symbolTable, Dictionary<string, Func<List<double>, double>> customFunctionTable)
    {

      List<double> evalArgs = new List<double>();
      foreach (ASTNode node in args)
        evalArgs.Add(Evaluate(node, symbolTable, customFunctionTable));

      return evalArgs;
    }

    private static double ComputeAddition(List<double> resultValues)
    {
      double result = resultValues[0];

      for (int i = 1; i < resultValues.Count(); i++)
        result += resultValues[i];
      return result;
    }

    private static double ComputeSubstraction(List<double> resultValues)
    {
      double result = resultValues[0];

      for (int i = 1; i < resultValues.Count(); i++)
        result -= resultValues[i];
      return result;
    }

    private static double ComputeMultiplication(List<double> resultValues)
    {
      double result = resultValues[0];

      for (int i = 1; i < resultValues.Count(); i++)
        result *= resultValues[i];
      return result;
    }

    private static double ComputeDivision(List<double> resultValues)
    {
      double result = resultValues[0];

      for (int i = 1; i < resultValues.Count(); i++)
        result /= resultValues[i];
      return result;
    }

    private static double ComputePower(List<double> resultValues)
    {
      double result = resultValues[resultValues.Count() - 1];

      for (int i = resultValues.Count() - 2; i >= 0; i--)
        result = Math.Pow(resultValues[i], result);
      return result;
    }

    private static int[] EvaluatePowerRationnalRec(int[] baseFrac, int exponent)
    {
      if (exponent >= 0)
      {
        int[] recPow = EvaluatePowerRationnalRec(baseFrac, exponent - 1);
        return EvaluateProductRationnalRec(recPow, baseFrac);
      }
      else if (exponent == 0)
        return new int[] { 1, 1 }; // Base case 1
      else if (exponent == -1)
        return new int[] { baseFrac[1], baseFrac[0] }; // Base case 2
      else
      {
        int[] inverse = new int[] { baseFrac[1], baseFrac[0] };
        return EvaluatePowerRationnalRec(inverse, -exponent);
      }
    }

    private static int[] EvaluateProductRationnalRec(int[] leftFrac, int[] rightFrac)
    {
      return new int[] { leftFrac[0] * rightFrac[0], leftFrac[1] * rightFrac[1] };
    }

    #endregion
  }
}

