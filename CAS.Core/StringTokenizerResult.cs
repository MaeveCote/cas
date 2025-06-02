using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS.Core
{
  /// <summary>
  /// Holds the result of the <see cref="StringTokenizer"/> process.
  /// </summary>
  public struct StringTokenizerResult
  {
    /// <summary>
    /// The result of the process <see cref="StringTokenizer.Tokenize(string)"/>.
    /// </summary>
    public List<Token> TokenizedExpression { get; }

    /// <summary>
    /// A table holding the variables contained in the equation and their value to substitute with.
    /// </summary>
    public Dictionary<string, double?> SymbolTable { get; set; }

    public StringTokenizerResult(List<Token> tokenizedExpression, Dictionary<string, double?> symbolTable)
    {
      TokenizedExpression = tokenizedExpression;
      SymbolTable = symbolTable;
    }
  }
}
