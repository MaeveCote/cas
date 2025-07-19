using CAS.Core.EquationParsing;
using System.DirectoryServices.ActiveDirectory;
using System.Net.Quic;
using System.Windows.Navigation;

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
    /// <param name="applySimplify">Wether you want to simplify or not. Default = true</param>
    /// <returns>The differentiated <paramref name="input"/></returns>
    public ASTNode Differentiate(ASTNode input, ASTNode x, bool applySimplify = true)
    {
      if (!(x.Token.Type is Variable diffVar))
        throw new ArgumentException("'x' is not a variable.");

      ASTNode? result = null;
      if (input.IsConstant())
        result = ConstantRule(input, x);
      if (input.Token.Type is Variable symbol)
      {
        result = ConstantRule(input, x);
      }
      if (input.IsFunction())
        result = FunctionRule(input, x);
      if (input.Kind() == "^")
        result = PowerRule(input, x);
      if (input.Kind() == "+" || input.Kind() == "-")
        result = SumDifferenceRule(input, x);
      if (input.Kind() == "*")
        result = ProductRule(input, x);
      if (input.Kind() == "/")
        result = QuotientRule(input, x);

      if (result == null)
        throw new ArgumentException("'input' contains a token not supported by differentiation.");

      return Simplifier.AutomaticSimplify(result);
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
        tempTree = Differentiate(tempTree, x, false);
      }

      return Simplifier.AutomaticSimplify(tempTree);
    }

    #region Private methods

    private ASTNode ConstantRule(ASTNode constant, ASTNode x)
    {
      // d/dx(x) = 1
      if (constant.Token.Type is Variable variable && variable.stringValue == x.Kind())
        return new ASTNode(Token.Integer("1"));

      // d/dx(a) = 0
      return new ASTNode(Token.Integer("0"));
    }

    private ASTNode PowerRule(ASTNode power, ASTNode x)
    {
      var powBase = power.Base();
      var powExp = power.Exponent();

      // d/dx(f(x)^a) = a * f(x)^(a - 1) * d/dx(f(x)) 
      if (powExp.IsConstant())
      {
        return new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(powExp),
          new ASTNode(Token.Operator("^"), new List<ASTNode> {
            new ASTNode(powBase), new ASTNode(Token.Operator("-"), new List<ASTNode> {
              new ASTNode(powExp), 
              new ASTNode(Token.Integer("1"))
            })
          }),
          Differentiate(powBase, x, false)
        });
      }

      // d/dx(a^f(x)) = ln(a) * a^(f(x)) * d/dx(f(x))
      if (powBase.IsConstant())
      {
        return new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(power),
          new ASTNode(Token.Function("ln"), new List<ASTNode> {
            new ASTNode(powBase)
          }),
          Differentiate(powExp, x, false)
        });
      }

      // Generic case
      // d/dx(g(x)^f(x)) = (ln(g(x)) * d/dx(f(x)) + ((d/dx(g(x)) * f(x))) / g(x)) * (g(x) ^ f(x))
      var leftProduct = new ASTNode(Token.Operator("*"), new List<ASTNode> { 
        new ASTNode(Token.Function("ln"), new List<ASTNode> { new ASTNode(powBase) }),
        Differentiate(powExp, x, false)
      });
      var rightQuotient = new ASTNode(Token.Operator("/"), new List<ASTNode> {
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          Differentiate(powBase, x, false),
          new ASTNode(powExp)
        }),
        new ASTNode(powBase)
      });
      var innerSum = new ASTNode(Token.Operator("+"), new List<ASTNode> { 
        leftProduct,
        rightQuotient
      });
      return new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
        innerSum,
        new ASTNode(power)
      });
    }

    private ASTNode SumDifferenceRule(ASTNode input, ASTNode x)
    {
      // d/dx(f(x) +- g(x)) = d/dx(f(x)) +- d/dx(g(x))
      var diffChildren = new List<ASTNode>();
      foreach (var child in input.Children)
        diffChildren.Add(Differentiate(child, x, false));

      return new ASTNode(Token.Operator(input.Token.Type.stringValue), diffChildren);
    }

    private ASTNode ProductRule(ASTNode product, ASTNode x)
    {
      if (product.Children.Count() == 0)
        return new ASTNode(product);
      if (product.Children.Count() == 1)
        return Differentiate(product.Children[0], x, false);

      if (product.Children.Count() != 2)
      {
        return Differentiate(new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Operator("*"), product.Children.GetRange(0, product.Children.Count() - 1)),
          product.Children[product.Children.Count() - 1]
        }), x, false);
      }

      // d/dx(f(x) * g(x)) = f(x) * d/dx(g(x)) + d/dx(f(x)) * g(x)
      return new ASTNode(Token.Operator("+"), new List<ASTNode> {
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(product.Children[0]),
          Differentiate(product.Children[1], x, false)
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          Differentiate(product.Children[0], x, false),
          new ASTNode(product.Children[1])
        }),
      });
    }

    private ASTNode QuotientRule(ASTNode quotient, ASTNode x)
    {
      // d/dx(f(x)/g(x)) = (d/dx(f(x)) * g(x) - f(x) * d/dx(g(x))) / (g(x)^2)
      var num = new ASTNode(Token.Operator("-"), new List<ASTNode> { 
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          Differentiate(quotient.Children[0], x, false),
          new ASTNode(quotient.Children[1])
        }),
        new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(quotient.Children[0]),
          Differentiate(quotient.Children[1], x, false)
        })
      });
      return new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
        num,
        new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(quotient.Children[1]),
          new ASTNode(Token.Integer("2"))
        })
      });
    }

    private ASTNode FunctionRule(ASTNode function, ASTNode x)
    {
      // ---------- Logarithms ----------
      // d/dx(ln(f(x))) = d/dx(f(x))/f(x)
      if (function.Kind().ToLower() == "ln")
      {
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          Differentiate(function.Children[0], x, false),
          new ASTNode(function.Children[0])
        });
      }
      
      // d/dx(log(f(x), a)) = d/dx(f(x))/(f(x) * ln(a))
      if (function.Kind().ToLower() == "log")
      {
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          Differentiate(function.Children[0], x, false),
          new ASTNode(Token.Operator("*"), new List<ASTNode> {
            new ASTNode(function.Children[0]),
            new ASTNode(Token.Function("ln"), new List<ASTNode> { new ASTNode(function.Children[1]) })
          })
        });
      }


      // ---------- Trigonometric functions ----------
      // d/dx(sin(f(x))) = cos(f(x)) * d/dx(f(x))
      if (function.Kind().ToLower() == "sin")
      {
        return new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(function.Children[0]) }),
          Differentiate(function.Children[0], x, false)
        });
      }

      // d/dx(cos(f(x))) = -sin(f(x)) * d/dx(f(x))
      if (function.Kind().ToLower() == "cos")
      {
        return new ASTNode(Token.Operator("*"), new List<ASTNode> {
          new ASTNode(Token.Operator("-"), new List<ASTNode> { 
            new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(function.Children[0]) }) 
          }),
          Differentiate(function.Children[0], x, false)
        });
      }

      // d/dx(tan(f(x))) = d/dx(f(x))/(cos(f(x))) ^ 2
      if (function.Kind().ToLower() == "tan")
      {
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          Differentiate(function.Children[0], x, false),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(function.Children[0]) }),
            new ASTNode(Token.Integer("2"))
          })
        });
      }
      
      // d/dx(sec(f(x))) = d/dx(1/cos(f(x)))
      if (function.Kind().ToLower() == "sec")
      {
        return Differentiate(new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(function.Children[0]) })
        }), x, false);
      }

      // d/dx(csc(f(x))) = d/dx(1/sin(f(x)))
      if (function.Kind().ToLower() == "csc")
      {
        return Differentiate(new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(function.Children[0]) })
        }), x, false);
      }

      // d/dx(cot(f(x))) = d/dx(cos(f(x))/sin(f(x)))
      if (function.Kind().ToLower() == "cot")
      {
        return Differentiate(new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Function("cos"), new List<ASTNode> { new ASTNode(function.Children[0]) }),
          new ASTNode(Token.Function("sin"), new List<ASTNode> { new ASTNode(function.Children[0]) })
        }), x, false);
      }

      // d/dx(Arcsin(f(x))) = d/dx(f(x))/(1 - f(x)^2)^1/2
      if (function.Kind().ToLower() == "arcsin")
      {
        var denum = new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")),
            new ASTNode(Token.Operator("^"), new List<ASTNode>
            {
              new ASTNode(function.Children[0]),
              new ASTNode(Token.Integer("2"))
            })
          }),
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2"))
          })
        });
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          Differentiate(function.Children[0], x, false),
          denum
        });
      }

      // d/dx(Arccos(f(x))) = -d/dx(f(x))/(1 - f(x)^2)^1/2
      if (function.Kind().ToLower() == "arccos")
      {
        var denum = new ASTNode(Token.Operator("^"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")),
            new ASTNode(Token.Operator("^"), new List<ASTNode>
            {
              new ASTNode(function.Children[0]),
              new ASTNode(Token.Integer("2"))
            })
          }),
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
            new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2"))
          })
        });
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode> { Differentiate(function.Children[0], x, false) }),
          denum
        });
      }

      // d/dx(Arctan(f(x))) = d/dx(f(x))/(1 + f(x)^2)
      if (function.Kind().ToLower() == "arctan")
      {
        var denum = new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(function.Children[0]),
            new ASTNode(Token.Integer("2"))
          })
        });
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          Differentiate(function.Children[0], x, false),
          denum
        });
      }

      // d/dx(Arcsec(f(x))) = (d/dx(f(x)) / (f(x) * (f(x)^2 - 1)^(1/2))
      if (function.Kind().ToLower() == "arcsec")
      {
        var denum = new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(function.Children[0]),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("-"), new List<ASTNode>
            {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                new ASTNode(function.Children[0]),
                new ASTNode(Token.Integer("2"))
              }),
              new ASTNode(Token.Integer("1"))
            }),
            new ASTNode(Token.Fraction(), new List<ASTNode>
            {
              new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2"))
            })
          })
        });
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          Differentiate(function.Children[0], x, false),
          denum
        });
      }

      // d/dx(Arccsc(f(x))) = (-d/dx(f(x)) / (f(x) * (f(x)^2 - 1)^(1/2))
      if (function.Kind().ToLower() == "arccsc")
      {
        var denum = new ASTNode(Token.Operator("*"), new List<ASTNode>
        {
          new ASTNode(function.Children[0]),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("-"), new List<ASTNode>
            {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                new ASTNode(function.Children[0]),
                new ASTNode(Token.Integer("2"))
              }),
              new ASTNode(Token.Integer("1"))
            }),
            new ASTNode(Token.Fraction(), new List<ASTNode>
            {
              new ASTNode(Token.Integer("1")), new ASTNode(Token.Integer("2"))
            })
          })
        });
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode> { Differentiate(function.Children[0], x, false) }),
          denum
        });
      }

      // d/dx(Arccot(f(x))) = (-d/dx(f(x)) / (f(x)^2 + 1)
      if (function.Kind().ToLower() == "arccot")
      {
        var denum = new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          new ASTNode(Token.Integer("1")),
          new ASTNode(Token.Operator("^"), new List<ASTNode>
          {
            new ASTNode(function.Children[0]),
            new ASTNode(Token.Integer("2"))
          })
        });
        return new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Operator("-"), new List<ASTNode> { Differentiate(function.Children[0], x, false) }),
          denum
        });
      }

      // Generic functions fallback (rename with a prime ('))
      // d/dx(f(x)) = f'(x)
      return new ASTNode(Token.Function(function.Kind() + "'"), function.Children);
    }

    #endregion
  }
}
