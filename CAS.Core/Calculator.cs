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
    public static double Evaluate(ASTNode root, Dictionary<string, double> symbolTable)
    {
      // A Dictionnary that converts string functions to evaluate it.
      var functionTable = new Dictionary<string, Func<List<ASTNode>, Dictionary<string, double>, double>>
      {
          { "abs", (args, symTable) => Math.Abs(Evaluate(args[0], symTable)) },
          { "sign", (args, symTable) => Math.Sign(Evaluate(args[0], symTable)) },
          { "sqrt", (args, symTable) => Math.Sqrt(Evaluate(args[0], symTable)) },
          { "ln", (args, symTable) => Math.Log(Evaluate(args[0], symTable)) },
          { "log", (args, symTable) =>
              {
                  if (args.Count == 2)
                      return Math.Log(Evaluate(args[1], symTable), Evaluate(args[0], symTable)); // log(base, value)
                  else
                      return Math.Log10(Evaluate(args[0], symTable)); // default to base 10
              }
          },
          { "exp", (args, symTable) => Math.Exp(Evaluate(args[0], symTable)) },
          { "sin", (args, symTable) => Math.Sin(Evaluate(args[0], symTable)) },
          { "cos", (args, symTable) => Math.Cos(Evaluate(args[0], symTable)) },
          { "tan", (args, symTable) => Math.Tan(Evaluate(args[0], symTable)) },
          { "asin", (args, symTable) => Math.Asin(Evaluate(args[0], symTable)) },
          { "acos", (args, symTable) => Math.Acos(Evaluate(args[0], symTable)) },
          { "atan", (args, symTable) => Math.Atan(Evaluate(args[0], symTable)) },
          { "floor", (args, symTable) => Math.Floor(Evaluate(args[0], symTable)) },
          { "ceil", (args, symTable) => Math.Ceiling(Evaluate(args[0], symTable)) },
          { "round", (args, symTable) => Math.Round(Evaluate(args[0], symTable)) },
          { "min", (args, symTable) => args.Select(arg => Evaluate(arg, symTable)).Min() },
          { "max", (args, symTable) => args.Select(arg => Evaluate(arg, symTable)).Max() }
      };

      if (root.Token.Type is Number)
        return double.Parse(root.Token.Type.stringValue);

      if (root.Token.Type is Variable)
        return symbolTable[root.Token.Type.stringValue];

      if (root.Token.Type is Function)
      {
        string funcName = root.Token.Type.stringValue;
        return functionTable[funcName](root.Children, symbolTable);
      }

      if (root.Token.Type is Operator)
      {
        double left = Evaluate(root.Children[0], symbolTable);
        double right = Evaluate(root.Children[1], symbolTable);

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
  }
}
