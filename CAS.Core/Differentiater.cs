using CAS.Core.EquationParsing;

namespace CAS.Core
{
  public class Differentiater
  {
    private Simplifier Simplifier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Differentiater"/>.
    /// </summary>
    /// <param name="simplifier">The simplifier of the application or a simplifier with the same settings.</param>
    /// <exception cref="ArgumentException"><paramref name="simplifier"/> is null.</exception>
    public Differentiater(Simplifier simplifier)
    {
      if (simplifier == null)
        throw new ArgumentException("'Simplifier' is null.");
      Simplifier = simplifier;
    }

    /// <summary>
    /// Differentiate the function represented by the <paramref name="input"/> in terms of the variable <paramref name="x"/>.
    /// </summary>
    /// <remarks>This can be treated as a partial differentiation as it will consider the other variables as constants.</remarks>
    /// <param name="input">A function</param>
    /// <param name="x">A variable</param>
    /// <returns>The differentiated <paramref name="input"/></returns>
    public ASTNode Differentiate(ASTNode input, ASTNode x)
    {
      if (!(x.Token.Type is Variable diffVar))
        throw new ArgumentException("'x' is not a variable.");

      if (input.IsConstant())
        return ConstantRule(input, x);
      if (input.Token.Type is Variable symbol)
      {
        if (symbol.stringValue == diffVar.stringValue)
          return ConstantRule(input, x);
        return PowerRule(input, x);
      }
      if (input.IsFunction())
        return FunctionRule(input, x);
      if (input.Kind() == "^")
        return PowerRule(input, x);
      if (input.Kind() == "+" || input.Kind() == "-")
        return SumDifferenceRule(input, x);
      if (input.Kind() == "*")
        return ProductRule(input, x);
      if (input.Kind() == "/")
        return QuotientRule(input, x);

      throw new ArgumentException("'input' contains a token not supported by differentiation.");
    }

    /// <summary>
    /// Differentiate the function represented by the <paramref name="input"/> in terms of the variable <paramref name="x"/> <paramref name="n"/> times.
    /// </summary>
    /// <remarks>This can be treated as a partial differentiation as it will consider the other variables as constants.</remarks>
    /// <param name="input">A function</param>
    /// <param name="x">A variable</param>
    /// <param name="n">The number of differentiations</param>
    /// <returns>The <paramref name="input"/> differentiated <paramref name="n"/> times.</returns>
    public ASTNode NDifferentiate(ASTNode input, ASTNode x, int n)
    {
      if (n < 1)
        throw new ArgumentException("You need to differentiate at least 1 time.");

      ASTNode tempTree = input;
      for (int i = 0; i < n; i++)
      {
        if (input.IsUndefined()) return ASTNode.NewUndefined();
        tempTree = Differentiate(tempTree, x);
      }

      return tempTree;
    }

    #region Private methods

    private ASTNode ConstantRule(ASTNode power, ASTNode x)
    {
      return null;
    }

    private ASTNode PowerRule(ASTNode power, ASTNode x)
    {
      return null;
    }

    private ASTNode SumDifferenceRule(ASTNode power, ASTNode x)
    {
      return null;
    }

    private ASTNode ProductRule(ASTNode power, ASTNode x)
    {
      return null;
    }

    private ASTNode QuotientRule(ASTNode power, ASTNode x)
    {
      return null;
    }

    private ASTNode FunctionRule(ASTNode power, ASTNode x)
    {
      return null;
    }

    #endregion
  }
}
