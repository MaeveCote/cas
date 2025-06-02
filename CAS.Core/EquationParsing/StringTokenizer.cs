using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CAS.Core.EquationParsing
{
  // Interesting to Add:
  //  -Support for symbol notation using the '\' character
  //  -Support for complex operations like derivatives, integrals, summations, etc...
  //  -Find a way to support arbitrary number of arguments in function.

  /// <summary>
  /// Tokenizes a mathematical expression before converting it to an abstract synthax tree (AST).
  /// </summary>
  public class StringTokenizer
  {
    private const string DIGIT_PATTERN = @"\d";
    private const string LETTER_PATTERN = @"[a-z]";
    private const string OPERATOR_PATTERN = @"\+|-|\*|\/|\^";

    /// <summary>
    /// Creates an instance of StringTokenizer
    /// </summary>
    public StringTokenizer() { }

    /// <summary>
    /// Tokenizes the given mathematical expression.
    /// </summary>
    /// <param name="mathExpression">The expression to tokenize</param>
    /// <remarks>This tokenizer makes some assumptions:
    ///   1-The spaces are discarded, so the expression can contain 0 or more whitespaces between token before tokenizing.
    ///   2-Function arguments must be in parentheses.
    ///   3-Letters before a parenthesis will be assumed to be a function, not implicit multiplication.
    ///   4-Implicit multiplication will only happen between NUMBER - VARIABLE and NUMBER - FUNCTION
    /// </remarks>
    /// <exception cref="ArgumentException">The given mathematical expression is not formatted correctly. There is an undefined character.</exception>
    /// <exception cref="ArgumentException">The given mathematical expression is not formatted correctly. Missing matchin parenthesis.</exception>
    /// <exception cref="ArgumentException">The given mathematical expression is not formatted correctly. Missing function argument or useless parentheses.</exception>
    /// <returns>A list of tokens that can be used to create an AST</returns>
    public StringTokenizerResult Tokenize(string mathExpression)
    {
      List<Token> tokenizedExpression = new List<Token>();
      HashSet<string> symbols = new HashSet<string>();
      string numberBuffer = "";
      string letterBuffer = "";
      int parentheseCount = 0;  // Keeps track of closing and opening parenthesis match

      // To prevent useless parentheses and missing function arguments
      int countSinceLastLeftParenthesis = 0;
      bool lastParenthesis = false; 

      foreach (char c in mathExpression)
      {
        // Skip spaces
        if (c == ' ') continue;
        else if (IsDigit(c) || IsDot(c))
          numberBuffer += c;
        else if (IsLetter(c))
        {
          if (numberBuffer.Length != 0)
          {
            tokenizedExpression.Add(Token.Number(numberBuffer));
            numberBuffer = "";
            
            // Implicite multiplication
            tokenizedExpression.Add(Token.Operator("*"));
          }

          letterBuffer += c;
        }
        else if (IsOperator(c))
        {
          if (numberBuffer.Length != 0)
          {
            tokenizedExpression.Add(Token.Number(numberBuffer));
            numberBuffer = "";
          }

          foreach (char ch in letterBuffer)
          {
            tokenizedExpression.Add(Token.Variable(ch.ToString()));
            symbols.Add(ch.ToString());
          }

          letterBuffer = "";
          tokenizedExpression.Add(Token.Operator(c.ToString()));
        }
        else if (IsLeftParenthesis(c))
        {
          if (letterBuffer.Length != 0)
          {
            tokenizedExpression.Add(Token.Function(letterBuffer));
            letterBuffer = "";
          }
          else if (numberBuffer.Length != 0)
          {
            tokenizedExpression.Add(Token.Number(numberBuffer));
            numberBuffer = "";

            // Implicite multiplication
            tokenizedExpression.Add(Token.Operator("*"));
          }

          parentheseCount++;
          lastParenthesis = true;
          tokenizedExpression.Add(Token.LeftParenthesis());
        }
        else if (IsRightParenthesis(c))
        {
          if (lastParenthesis)
            throw new ArgumentException(
              "The given mathematical expression is not formatted correctly. Missing function argument or useless parentheses.");

          if (numberBuffer.Length != 0)
          {
            tokenizedExpression.Add(Token.Number(numberBuffer));
            numberBuffer = "";
          }

          foreach (char ch in letterBuffer)
          {
            tokenizedExpression.Add(Token.Variable(ch.ToString()));
            symbols.Add(ch.ToString());
          }

          letterBuffer = "";

          parentheseCount--;
          tokenizedExpression.Add(Token.RightParenthesis());
        }
        else if (IsComma(c))
        {
          if (numberBuffer.Length != 0)
          {
            tokenizedExpression.Add(Token.Number(numberBuffer));
            numberBuffer = "";
          }

          foreach (char ch in letterBuffer)
          {
            tokenizedExpression.Add(Token.Variable(ch.ToString()));
            symbols.Add(ch.ToString());
          }
          
          letterBuffer = "";
          tokenizedExpression.Add(Token.FunctionArgumentSeparator());
        }
        else
          throw new ArgumentException(
            "The given mathematical expression is not formatted correctly. There is an undefined character.");

        if (countSinceLastLeftParenthesis == 1)
        {
          lastParenthesis = false;
          countSinceLastLeftParenthesis = 0;
        }
        if (lastParenthesis == true)
          countSinceLastLeftParenthesis++;
      }

      // Dump out the rest of the buffers in result
      if (numberBuffer.Length != 0)
        tokenizedExpression.Add(Token.Number(numberBuffer));
      foreach (char ch in letterBuffer)
      {
        tokenizedExpression.Add(Token.Variable(ch.ToString()));
        symbols.Add(ch.ToString());
      }

      if (parentheseCount != 0)
          throw new ArgumentException(
            "The given mathematical expression is not formatted correctly. Missing matchin parenthesis.");

      return new StringTokenizerResult(tokenizedExpression, symbols);
    }

    #region Helper Functions

    private bool IsDigit(char c) { return Regex.IsMatch(c.ToString(), DIGIT_PATTERN); }
    private bool IsLetter(char c) { return Regex.IsMatch(c.ToString(), LETTER_PATTERN); }
    private bool IsOperator(char c) { return Regex.IsMatch(c.ToString(), OPERATOR_PATTERN); }
    private bool IsComma(char c) { return c == ','; }
    private bool IsDot(char c) { return c == '.'; }
    private bool IsLeftParenthesis(char c) { return c == '('; }
    private bool IsRightParenthesis(char c) { return c == ')'; }

    #endregion
  }
}
