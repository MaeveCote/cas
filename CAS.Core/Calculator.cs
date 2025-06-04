using System;
using System.Collections.Generic;
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
        double left = Evaluate(root.Children[0], symbolTable, customFunctionTable);
        double right = Evaluate(root.Children[1], symbolTable, customFunctionTable);

        return root.Token.Type.stringValue switch
        {
          "+" => left + right,
          "-" => left - right,
          "*" => left * right,
          "/" => left / right,
          "^" => Math.Pow(left, right),
          _ => throw new InvalidOperationException("Unknown operator")
        };
      }

      throw new InvalidOperationException("Unsupported token type, the node is not evaluable.");
    }

    private static List<double> EvaluateArgs(List<ASTNode> args,  Dictionary<string, double> symbolTable, Dictionary<string, Func<List<double>, double>> customFunctionTable)
    {

      List<double> evalArgs = new List<double>();
      foreach (ASTNode node in args)
        evalArgs.Add(Evaluate(node, symbolTable, customFunctionTable));

      return evalArgs;
    }
  }
}
