using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS.Core.EquationParsing
{
  /// <summary>
  /// Builds an Abstract Syntax Tree from a list of tokens in infix notation.
  /// </summary>
  public static class ASTBuilder
  {
    /// <summary>
    /// Uses the Shunting Yard algorithm to convert a mathematical expression int infix notation into an AST.
    /// </summary>
    /// <remarks>See the <a href="https://en.wikipedia.org/wiki/Shunting_yard_algorithm">Shunting Yard Algorithm</a>. 
    /// We can ignore validity checks for the math expression since we assume it was generated using the <see cref="StringTokenizer"/></remarks>
    /// <param name="expression">The tokenized expression.</param>
    /// <exception cref="ArgumentException">The output should only have one element left (the root).</exception>
    /// <returns>The root of the AST</returns>
    public static ASTNode ParseInfixToAST(List<Token> expression)
    {
      Stack<ASTNode> output = new Stack<ASTNode>();
      Stack<Token> operators = new Stack<Token>();

      foreach(Token tok in expression)
      {
        if (tok.Type is Number || tok.Type is Variable)
          output.Push(new ASTNode(tok, new List<ASTNode>()));
        else if (tok.Type is Function)
          operators.Push(tok);
        else if (tok.Type is Operator)
        {
          Operator op1 = (Operator)tok.Type;
          Token op2;
          while (operators.Count() != 0)
          {
            op2 = operators.Peek();
            // Prevent comparison with parenthesis
            if (op2.Type is LeftParenthesis)
              break;
            int comparison = op1.ComparePriority((Operator)op2.Type);
            if (comparison < 0 || !op1.isRightAssociative && comparison == 0)
            {
              operators.Pop();
              AddNode(output, op2);
            }
            else break;
          }

          operators.Push(tok);
        }
        else if (tok.Type is FunctionArgumentSeparator)
        {
          while (!(operators.Peek().Type is LeftParenthesis))
            AddNode(output, operators.Pop());
        }
        else if (tok.Type is LeftParenthesis)
          operators.Push(tok);
        else if (tok.Type is RightParenthesis)
        {
          while (!(operators.Peek().Type is LeftParenthesis))
            AddNode(output, operators.Pop());
          // Discard the left parenthesis
          operators.Pop();

          if (operators.Count != 0 && operators.Peek().Type is Function)
          {
            var func = operators.Pop();
            List<ASTNode> args = new List<ASTNode>();
            for (int i = 0; i < ((Function)func.Type).numberOfArguments; i++)
              args.Add(output.Pop());

            args.Reverse();

            output.Push(new ASTNode(func, args));
          }
        }
      }

      // Dump the rest of the operator stack.
      while (operators.Count != 0)
        AddNode(output, operators.Pop());

      if (output.Count != 1)
        throw new ArgumentException("The output should only have one element left (the root).");

      return output.Pop();
    }

    private static void AddNode(Stack<ASTNode> output, Token op)
    {
      var rightChild = output.Pop();
      var leftChild = output.Pop();
      List<ASTNode> children =  new List<ASTNode>();
      children.Add(leftChild);
      children.Add(rightChild);

      output.Push(new ASTNode(op, children));
    }
  }
}
