using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS.Core
{
  /// <summary>
  /// A node to store data in an abstract syntax tree.
  /// </summary>
  public class ASTNode
  {
    public Token Token { get; }
    public ASTNode? Parent { get; set; }
    public List<ASTNode> Children { get; }

    public ASTNode(Token token, List<ASTNode> children)
    {
      Token = token;
      Parent = null;
      Children = children;
      foreach (ASTNode n in Children)
        n.Parent = this;
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
