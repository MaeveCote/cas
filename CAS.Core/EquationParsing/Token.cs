using System.Diagnostics.CodeAnalysis;

namespace CAS.Core.EquationParsing
{
  /// <summary>
  /// Holds token value for elements of a mathematical equation.
  /// </summary>
  public struct Token
  {
    /// <summary>
    /// The type of this token.
    /// </summary>
    public TokenType Type { get; set; }

    private Token(TokenType type)
    {
      Type = type;
    }

    public static Token Number(string value) => new(new Number(value));
    public static Token Integer(string value) => new(new IntegerNum(value));
    public static Token Fraction() => new(new Fraction());
    public static Token Variable(string value) => new(new Variable(value));
    public static Token Function(string value) => new(new Function(value));
    public static Token Function(string value, int argsCount) => new(new Function(value, argsCount));
    public static Token FunctionArgumentSeparator() => new(new FunctionArgumentSeparator());
    public static Token Operator(string value) => new(new Operator(value));
    public static Token LeftParenthesis() => new(new LeftParenthesis()); 
    public static Token RightParenthesis() => new(new RightParenthesis());
    public static Token Undefined() => new(new Undefined());

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
      if (obj is Token tok)
      {
        if (tok.Type is Number && Type is Number)
          return true;
        if (tok.Type is Variable && Type is Variable)
          return true;
        if (tok.Type is Function && Type is Function)
          return true;
        // Applys for other operators, parentheses, undefined, fractions and functionArgumentSeparaors
        if (tok.Type.stringValue == tok.Type.stringValue)
          return true;
      }

      return false;
    }
    public override string ToString()
    {
      return Type.ToString();
    }
  }
}
