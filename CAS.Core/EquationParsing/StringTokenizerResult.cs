namespace CAS.Core.EquationParsing
{
  /// <summary>
  /// Holds the result of the <see cref="StringTokenizer"/> process.
  /// </summary>
  public readonly struct StringTokenizerResult
  {
    /// <summary>
    /// The result of the process <see cref="StringTokenizer.Tokenize(string)"/>.
    /// </summary>
    public List<Token> TokenizedExpression { get; }

    /// <summary>
    /// A set of the variables found in the equation.
    /// </summary>
    public HashSet<string> Symbols { get; }

    public StringTokenizerResult(List<Token> tokenizedExpression, HashSet<string> symbols)
    {
      TokenizedExpression = tokenizedExpression;
      Symbols = symbols;
    }
  }
}
