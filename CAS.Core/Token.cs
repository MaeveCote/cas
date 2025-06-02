using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS.Core
{
  /// <summary>
  /// Holds token value for elements of a mathematical equation.
  /// </summary>
  public readonly struct Token
  {
    public TokenType Type { get; }

    private Token(TokenType type)
    {
      Type = type;
    }

    public static Token Number(string value) => new(new Number(value));
    public static Token Variable(string value) => new(new Variable(value));
    public static Token Function(string value) => new(new Function(value));
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
