using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS.Core
{
  public enum TokenType
  {
    Number,
    Variable,
    Function,
    FunctionArgumentSeparator,
    Operator,
    LeftParenthesis,
    RightParenthesis,
  }

  /// <summary>
  /// Holds token value for elements of a mathematical equation.
  /// </summary>
  public readonly struct Token
  {
    public TokenType Type { get; }
    
    // Only one value can be held at a time.
    public double? NumberValue { get; }
    public string? StringValue { get; }

    private Token(TokenType type, double? numberValue, string? stringValue)
    {
      Type = type;
      NumberValue = numberValue;
      StringValue = stringValue;
    }

    public static Token Number(string value) => new(TokenType.Number, double.Parse(value), null);
    public static Token Variable(string value) => new(TokenType.Variable, null, value);
    public static Token Function(string value) => new(TokenType.Function, null, value);
    public static Token FunctionArgumentSeparator(string value) => new(TokenType.FunctionArgumentSeparator, null, value);
    public static Token Operator(string value) => new(TokenType.Operator, null, value);
    public static Token LeftParenthesis() => new(TokenType.LeftParenthesis, null, "(");
    public static Token RightParenthese() => new(TokenType.RightParenthesis, null, ")");

    public override string ToString()
    {
      return Type switch
      {
        TokenType.Number => $"Number({NumberValue})",
        _ => $"{Type}('{StringValue}')"
      };
    }
  }
}
