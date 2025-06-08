using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS.Core.EquationParsing
{
  /// <summary>
  /// Holds token value for elements of a mathematical equation.
  /// </summary>
  public struct Token
  {
    /// <summary>
    /// The type of thus token.
    /// </summary>
    public TokenType Type { get; set; }

    private Token(TokenType type)
    {
      Type = type;
    }

    public static Token Number(string value) => new(new Number(value));
    public static Token Fraction() => new(new Fraction());
    public static Token Variable(string value) => new(new Variable(value));
    public static Token Function(string value) => new(new Function(value));
    public static Token Function(string value, int argsCount) => new(new Function(value, argsCount));
    public static Token FunctionArgumentSeparator() => new(new FunctionArgumentSeparator());
    public static Token Operator(string value) => new(new Operator(value));
    public static Token LeftParenthesis() => new(new LeftParenthesis()); 
    public static Token RightParenthesis() => new(new RightParenthesis());

    public override string ToString()
    {
      return Type.ToString();
    }
  }
}
