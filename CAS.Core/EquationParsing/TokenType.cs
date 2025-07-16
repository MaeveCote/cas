namespace CAS.Core.EquationParsing
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

    public override bool Equals(object? obj)
    {
      return obj is Variable v && stringValue == v.stringValue;
    }

    public override int GetHashCode()
    {
      return stringValue.GetHashCode();
    }
  }

  public class Undefined : TokenType
  {
    public Undefined() : base("Undefined") { }
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

  /// <summary>
  /// Can safely use int type casting on the value without losing data.
  /// </summary>
  public class IntegerNum : Number
  {
    public int intVal { get; }
    public IntegerNum(string _stringValue) : base(_stringValue)
    {
      intVal = int.Parse(_stringValue);
    }
  }

  public class Fraction : TokenType
  {
    public Fraction() : base("Frac") { }

    public override string ToString()
    {
      return $"Fraction('Frac')";
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
    public int numberOfArguments { get; set; }

    public Function(string _stringValue) : base(_stringValue)
    {
      priority = 4;
      numberOfArguments = 1;
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
