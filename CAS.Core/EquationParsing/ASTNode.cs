using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace CAS.Core.EquationParsing
{
  /// <summary>
  /// A node to store data in an abstract syntax tree.
  /// </summary>
  public class ASTNode
  {
    public Token Token { get; set; }
    public ASTNode? Parent { get; set; }
    public List<ASTNode> Children { get; set; }

    /// <summary>
    /// Constructs a new ASTNode.
    /// </summary>
    public ASTNode(Token token, List<ASTNode> children)
    {
      Token = token;
      Parent = null;
      Children = children;
      foreach (ASTNode n in Children)
        n.Parent = this;
    }

    /// <summary>
    /// Constructs a new ASTNode with no children.
    /// </summary>
    public ASTNode(Token token)
    {
      Token = token;
      Parent = null;
      Children = new List<ASTNode>();
    }
    
    /// <summary>
    /// Copy constructor
    /// </summary>
    public ASTNode(ASTNode other)
    {
      Token = other.Token; // Assuming Token is immutable or copied correctly
      Children = other.Children.Select(child => new ASTNode(child)).ToList();
    }

    #region Custom Operators

    /// <summary>
    /// Returns the as a string the kind of operator this node is.
    /// </summary>
    public string Kind()
    {
      return Token.Type.stringValue;
    }

    /// <summary>
    /// Returns the number of operands of this node.
    /// </summary>
    public int NumOfOperands()
    {
      return Children.Count();
    }

    /// <summary>
    /// Returns the i'th operand of this node.
    /// </summary>
    public ASTNode OperandAt(int i)
    {
      if (i < 0 || i >= Children.Count())
        throw new IndexOutOfRangeException();
      return Children[i];
    }
    
    /// <summary>
    /// Finds if the target is contained in the tree rooted at this node.
    /// </summary>
    /// <remarks>
    /// This might change if between a formatted and unformatted tree. 
    /// We recommend to format the tree before applying and not search for patterns dependent on the structure of the nodes
    /// </remarks>
    public bool FreeOf(ASTNode target)
    {
      if (target == this)
        return false;

      foreach (ASTNode child in Children)
      {
        if (!child.FreeOf(target))
          return false;
      }

      return true;
    }

    /// <summary>
    /// Substitute the substitution in place of the target.
    /// </summary>
    public void Substitute(ASTNode target, ASTNode substitution)
    {
      if (target == this)
      {
        this.Replace(substitution);
        return;
      }

      foreach (ASTNode child in Children)
        child.Substitute(target, substitution);
    }

    /// <summary>
    /// Replaces this node by the given other node.
    /// </summary>
    public void Replace(ASTNode other)
    {
      Token = other.Token;
      // Deep copy of children
      Children = other.Children.Select(child => new ASTNode(child)).ToList();
    }

    /// <summary>
    /// Returns the base of the expression if this is a power, itself if it is an operator or Undefined otherwise.
    /// </summary>
    public ASTNode Base()
    {
      if (Token.Type is Operator op)
      {
        if (op.stringValue == "^")
          return this.OperandAt(0);
        return this;
      }

      if (Token.Type is Variable)
        return this;

      return NewUndefined();
    }

    /// <summary>
    /// Returns the exponent of the expression if this is a power, 1 if it is an operator or Undefined otherwise.
    /// </summary>
    public ASTNode Exponent()
    {
      if (Token.Type is Operator op)
      {
        if (op.stringValue == "^")
          return this.OperandAt(1);
        return new ASTNode(Token.Integer("1"));
      }

      if (Token.Type is Variable)
        return new ASTNode(Token.Integer("1"));

      return NewUndefined();
    }

    /// <summary>
    /// Returns the terms of a product, a unary product if it is an operator or Undefined otherwise.
    /// </summary>
    /// <returns>A product without the constant.</returns>
    public ASTNode Terms()
    {
      if (Token.Type is Operator op)
      {
        if (op.stringValue == "*")
        {
          var u1 = this.OperandAt(0);
          if (u1.Token.Type is Number || u1.Token.Type is Fraction)
            return new ASTNode(Token.Operator("*"), this.Children.GetRange(1, Children.Count - 1));
          return this;
        }
        return new ASTNode(Token.Operator("*"), new List<ASTNode> { this });
      }

      if (Token.Type is Variable)
        return new ASTNode(Token.Operator("*"), new List<ASTNode> { this });

      return NewUndefined();
    }

    /// <summary>
    /// Returns the constant of a product, 1 if it is an operator or Undefined otherwise.
    /// </summary>
    /// <remarks>To get a list of terms do : node.Terms().Children</remarks>
    /// <returns></returns>
    public ASTNode Const()
    {
      if (Token.Type is Operator op)
      {
        if (op.stringValue == "*")
        {
          var u1 = this.OperandAt(0);
          if (u1.Token.Type is Number || u1.Token.Type is Fraction)
            return u1;
          return new ASTNode(Token.Integer("1"));
        }
        return new ASTNode(Token.Integer("1"));
      }

      if (Token.Type is Variable)
        return new ASTNode(Token.Integer("1"));

      return NewUndefined();
    }

    /// <summary>
    /// Returns the value of this node as a double.
    /// </summary>
    /// <remarks>This operator works only on ASAEs. This is intended to be used in the simplifier.</remarks>
    /// <exception cref="ArgumentException">This operation should not be called on a non constant node.</exception>
    public double EvaluateAsDouble()
    {
      if (Token.Type is Number num)
        return num.value;
      if (Token.Type is Fraction frac)
        return Children[0].EvaluateAsDouble() / Children[1].EvaluateAsDouble();

      throw new ArgumentException("This operation should not be called on a non constant node.");
    }

    /// <summary>
    /// Verifies if two nodes are alike.
    /// </summary>
    public static bool AreLikeTerms(ASTNode input1, ASTNode input2)
    {
      ASTNode u1, u2;
      if (input1.IsProduct())
      {
        u1 = input1.Terms();
        if (u1.Children.Count() == 1)
          u1 = u1.Children[0];
      }
      else u1 = input1;
      if (input2.IsProduct())
      {
        u2 = input2.Terms();
        if (u2.Children.Count() == 1)
          u2 = u2.Children[0];
      }
      else u2 = input2;

      if (u1.IsConstant() && u2.IsConstant())
        return true;
      if (u1.IsSymbol() && u2.IsSymbol())
        return u1.Kind() == u2.Kind();
      if (u1.IsPower() && u2.IsPower())
        return (AreLikeTerms(u1.Base(), u2.Base())) && (u1.Exponent() == u2.Exponent());
      if (u1.IsProduct() && u2.IsProduct())
      {
        var u1Terms = u1.Terms().Children;
        var u2Terms = u2.Terms().Children;

        if (u1Terms.Count() != u2Terms.Count())
          return false;
        for (int i = 0; i < u1Terms.Count(); i++)
        {
          if (!AreLikeTerms(u1Terms[i], u2Terms[i]))
            return false;
        }

        return true;
      }
      if (u1.IsFunction() && u2.IsFunction())
      {
        if ((u1.Children.Count() != u2.Children.Count()) || (u1.Kind() != u2.Kind()))
          return false;
        for (int i = 0; i < u1.Children.Count(); i++)
        {
          if (u1.Children[i] != u2.Children[i])
            return false;
        }

        return true;
      }
      if (u1.IsSum() && u2.IsSum())
      {
        if (u1.Children.Count() != u2.Children.Count())
          return false;
        for (int i = 0; i < u1.Children.Count(); i++)
        {
          if (u1.Children[i] != u2.Children[i])
            return false;
        }

        return true;
      }

      return false;
    }

    /// <summary>
    /// Converts a rational number node to an array numerator and denumerator.
    /// </summary>
    /// <returns>Int array of [numerator, denumerator]</returns>
    /// <exception cref="ArgumentException">The given node is not a rational number</exception>
    public int[] GetNumAndDenum()
    {
      int[] frac = new int[2];
      if (this.Token.Type is IntegerNum nodeInt)
      {
        frac[0] = nodeInt.intVal;
        frac[1] = 1;
      }
      else if (this.Token.Type is Fraction)
      {
        frac[0] = ((IntegerNum)this.OperandAt(0).Token.Type).intVal;
        frac[1] = ((IntegerNum)this.OperandAt(1).Token.Type).intVal;
      }
      else
        throw new ArgumentException("The 'node' should be a rationnal number");

      return frac;
    }

    #endregion

    #region Standard operators

    public static bool operator ==(ASTNode left, ASTNode right)
    {
      if (ReferenceEquals(left, right))
        return true;

      if (left is null || right is null)
        return false;

      return left.Equals(right);
    }

    public static bool operator !=(ASTNode left, ASTNode right)
    {
      return !(left == right);
    }
    public static bool operator <(ASTNode u, ASTNode v) => Compare(u, v);
    public static bool operator >(ASTNode u, ASTNode v) => Compare(v, u);

    #endregion

    #region Type Checks

    public bool IsConstant() => Token.Type is Number || Token.Type is Fraction;
    public bool IsRational() => Token.Type is IntegerNum || Token.Type is Fraction;
    public bool IsNumber() => Token.Type is Number;
    public bool IsIntegerNum() => Token.Type is IntegerNum;
    public bool IsFraction() => Token.Type is Fraction;
    public bool IsSymbol() => Token.Type is Variable;
    public bool IsPower() => Token.Type.stringValue == "^";
    public bool IsProduct() => Token.Type.stringValue == "*";
    public bool IsSum() => Token.Type.stringValue == "+";
    public bool IsFunction() => Token.Type is Function;
    public bool IsAddOrMultiply() => Token.Type.stringValue == "+" || Token.Type.stringValue == "*";
    // public bool IsFactorial() => 
    public bool IsPositive()
    {
      if (Token.Type is Number num)
        return num.value > 0;
      return false;
    }
 
    #endregion

    public override bool Equals(object obj)
    {
      if (obj is not ASTNode other)
        return false;

      if (Token.Type.stringValue != other.Token.Type.stringValue)
        return false;

      if (Children.Count != other.Children.Count)
        return false;

      bool isCommutative = Token.Type.stringValue == "+" || Token.Type.stringValue == "*";

      if (isCommutative)
      {
        // Unordered comparison: every child must match one in the other node
        var matched = new bool[Children.Count];
        foreach (var child in Children)
        {
          bool foundMatch = false;
          for (int i = 0; i < other.Children.Count; i++)
          {
            if (!matched[i] && child.Equals(other.Children[i]))
            {
              matched[i] = true;
              foundMatch = true;
              break;
            }
          }
          if (!foundMatch)
            return false;
        }

        return true;
      }
      else
      {
        // Ordered comparison
        for (int i = 0; i < Children.Count; i++)
        {
          if (!Children[i].Equals(other.Children[i]))
            return false;
        }

        return true;
      }
    }

    public override int GetHashCode()
    {
      int hash = Token.Type.stringValue.GetHashCode();

      if (Token.Type.stringValue == "+" || Token.Type.stringValue == "*")
      {
        // Combine hashes in an unordered way
        foreach (var child in Children.OrderBy(c => c.GetHashCode()))
          hash = HashCode.Combine(hash, child.GetHashCode());
      }
      else
      {
        foreach (var child in Children)
          hash = HashCode.Combine(hash, child.GetHashCode());
      }

      return hash;
    }

    public static bool Compare(ASTNode u, ASTNode v)
    {
      // O-1: Both constants (integer or fraction)
      if (u.IsConstant() && v.IsConstant())
      {
        return u.EvaluateAsDouble() < v.EvaluateAsDouble();
      }

      // O-2: Both symbols (variables)
      if (u.IsSymbol() && v.IsSymbol())
      {
        return LexCompare(u.Token.Type.stringValue, v.Token.Type.stringValue) < 0;
      }

      // O-3: Both products or both sums
      if (u.IsAddOrMultiply() && v.IsAddOrMultiply() && u.Token.Equals(v.Token))
      {
        int m = u.Children.Count, n = v.Children.Count;
        for (int j = 1; j <= Math.Min(m, n); ++j)
        {
          var uj = u.Children[m - j];
          var vj = v.Children[n - j];
          if (!uj.Equals(vj))
            return Compare(uj, vj);
        }
        return m < n;
      }

      // O-4: Both powers
      if (u.IsPower() && v.IsPower())
      {
        var (ubase, uexp) = (u.Children[0], u.Children[1]);
        var (vbase, vexp) = (v.Children[0], v.Children[1]);
        return ubase != vbase ? Compare(ubase, vbase) : Compare(uexp, vexp);
      }

      // O-5: Both factorials
      /*
      if (u.IsFactorial() && v.IsFactorial())
      {
        return Compare(u.Children[0], v.Children[0]);
      }
      */

      // O-6: Both functions
      if (u.IsFunction() && v.IsFunction())
      {
        var kindU = u.Token.Type.stringValue;
        var kindV = v.Token.Type.stringValue;
        if (kindU != kindV)
          return LexCompare(kindU, kindV) < 0;

        int m = u.Children.Count, n = v.Children.Count;
        for (int j = 0; j < Math.Min(m, n); ++j)
        {
          if (!u.Children[j].Equals(v.Children[j]))
            return Compare(u.Children[j], v.Children[j]);
        }
        return m < n;
      }

      // O-7: constant < anything else
      if (u.IsConstant() && !v.IsConstant()) return true;
      if (!u.IsConstant() && v.IsConstant()) return false;

      // O-8: product vs anything else
      if (u.IsProduct() && !v.IsProduct()) return Compare(u, UnaryProduct(v));
      if (!u.IsProduct() && v.IsProduct()) return !Compare(v, u);

      // O-9: power vs other
      if (u.IsPower() && !v.IsPower()) return Compare(u, PromoteToPower(v));
      if (!u.IsPower() && v.IsPower()) return !Compare(v, u);

      // O-10: sum vs other
      if (u.IsSum() && !v.IsSum()) return Compare(u, UnarySum(v));
      if (!u.IsSum() && v.IsSum()) return !Compare(v, u);

      // O-11: factorial vs function or symbol
      /*
      if (u.IsFactorial() && (v.IsFunction() || v.IsSymbol()))
      {
        return u.Children[0].Equals(v) ? false : Compare(u, Factorial(v));
      }
      if ((u.IsFunction() || u.IsSymbol()) && v.IsFactorial())
      {
        return v.Children[0].Equals(u) ? true : !Compare(v, Factorial(u));
      }
      */

      // O-12: function vs symbol
      if (u.IsFunction() && v.IsSymbol())
      {
        return u.Token.Type.stringValue == v.Token.Type.stringValue ? false : LexCompare(u.Token.Type.stringValue, v.Token.Type.stringValue) < 0;
      }
      if (u.IsSymbol() && v.IsFunction())
      {
        return u.Token.Type.stringValue == v.Token.Type.stringValue ? true : LexCompare(u.Token.Type.stringValue, v.Token.Type.stringValue) < 0;
      }

      // O-13: default fallback
      return !Compare(v, u);
    }

    private static int LexCompare(string a, string b)
    {
      static int CharRank(char c)
      {
        if (char.IsDigit(c)) return c - '0';
        if (char.IsUpper(c)) return 10 + (c - 'A');
        if (char.IsLower(c)) return 36 + (c - 'a');
        return -1;
      }
      for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
      {
        int cmp = CharRank(a[i]) - CharRank(b[i]);
        if (cmp != 0) return cmp;
      }
      return a.Length - b.Length;
    }

    #region Builders

    private static ASTNode UnaryProduct(ASTNode v) => new(Token.Operator("*"), new() { v });
    private static ASTNode UnarySum(ASTNode v) => new(Token.Operator("+"), new() { v });
    // private static ASTNode Factorial(ASTNode v) => new(Token.Factorial(), new() { v });
    private static ASTNode PromoteToPower(ASTNode v) => new(Token.Operator("^"), new() { v, new(Token.Integer("1")) });
    public static ASTNode NewUndefined() => new ASTNode(Token.Undefined());

    #endregion

    public override string ToString()
    {
      return ToString(0);
    }

    private string ToString(int indentLevel)
    {
      var indent = new string(' ', indentLevel * 2); // 2 spaces per level
      string result = indent + Token.Type.stringValue + "\n";

      foreach (var child in Children)
      {
        result += child.ToString(indentLevel + 1);
      }

      return result;
    }
  }
}
