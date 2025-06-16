using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    #region Operators

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
    /// <returns></returns>
    public ASTNode Term()
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
    /// Creates an Undefined ASTNode
    /// </summary>
    public static ASTNode NewUndefined()
    {
      return new ASTNode(Token.Undefined());
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
