using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace CAS.Core
{
  public class TokenType
  {
    public string stringValue { get; }

    public TokenType(string _stringValue)
    {
      stringValue = _stringValue;
    }
    public override string ToString()
    {
      return $"Token('{stringValue}')";
    }
  }

  public class Number : TokenType
  {
    public double value { get; }

    public Number(string _stringValue) : base(_stringValue)
    {
      value = double.Parse(_stringValue);
    }

    public override string ToString()
    {
      return $"Number({value})";
    }
  }

  public class Variable : TokenType
  {
    public Variable(string _stringValue) : base(_stringValue) { }

    public override string ToString()
    {
      return $"Variable('{stringValue}')";
    }
  }

  public class Operator : TokenType
  {
    public int priority { get; protected set; }
    public bool isRightAssociative { get; }

    public Operator(string _stringValue) : base(_stringValue)
    {
      if (stringValue == "^")
      {
        isRightAssociative = true;
        priority = 3;
      }
      else if (stringValue == "*" || stringValue == "/")
      {
        isRightAssociative = false;
        priority = 2;
      }
      else if (stringValue == "+" || stringValue == "-")
      {
        isRightAssociative = false;
        priority = 1;
      }
    }

    public int ComparePriority(Operator other)
    {
      return priority - other.priority;
    }

    public override string ToString()
    {
      return $"Operator('{stringValue}')";
    }
  }

  public class Function : Operator
  {
    public int numberOfArguments { get; }

    public Function(string _stringValue) : base(_stringValue)
    {
      priority = 4;
      numberOfArguments = 1;

      // Parse the value to find if the function has more arguments
      if (stringValue == "mod" || stringValue == "nthroot" || stringValue == "gcd" || stringValue == "lcm" || 
        stringValue == "diff" || stringValue == "max" || stringValue == "min" || stringValue == "log")
        numberOfArguments = 2;
    }

    public Function(string _stringValue, int _numberOfArguments) : base(_stringValue)
    {
      priority = 4;
      numberOfArguments = _numberOfArguments;
    }

    public override string ToString()
    {
      return $"Function('{stringValue}')";
    }
  }

  public class FunctionArgumentSeparator : TokenType
  {
    public FunctionArgumentSeparator() : base(",") { }

    public override string ToString()
    {
      return $"FunctionArgumentSeparator('{stringValue}')";
    }
  }

  public class LeftParenthesis : TokenType
  {
    public LeftParenthesis() : base("(") { }
    
    public override string ToString()
    {
    return $"LeftParenthesis('{stringValue}')";
    }
  }

  public class RightParenthesis : TokenType
  {
    public RightParenthesis() : base(")") { }

    public override string ToString()
    {
      return $"RightParenthesis('{stringValue}')";
    }
  }
}
