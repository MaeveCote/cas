using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CAS.Core;
using CAS.Core.EquationParsing;

namespace CAS.UT
{
  public class CalculatorTest
  {
    private ASTNode Int(int value) =>
      new ASTNode(Token.Integer(value.ToString()), new List<ASTNode>());

    private ASTNode Frac(int numerator, int denominator) =>
      new ASTNode(Token.Fraction(), new List<ASTNode>
      {
      Int(numerator),
      Int(denominator)
      });

    private ASTNode Invalid() =>
      new ASTNode(Token.Operator("+"), new List<ASTNode>());

    [Fact]
    public void BasicArithmetic()
    {
      var equation1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
        new ASTNode(Token.Operator("-"), new List<ASTNode>
        {
          new ASTNode(Token.Number("5"), new List<ASTNode>()),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
        }),
        new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      var equation2 = new ASTNode(Token.Operator("/"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("*"), new List<ASTNode>
          {
              new ASTNode(Token.Number("6"), new List<ASTNode>()),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
          }),
          new ASTNode(Token.Number("2"), new List<ASTNode>())
      });

      var equation3 = new ASTNode(Token.Operator("*"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Number("1"), new List<ASTNode>()),
              new ASTNode(Token.Number("2"), new List<ASTNode>())
          }),
          new ASTNode(Token.Number("3"), new List<ASTNode>())
      });

      var fractions = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Number("3"), new List<ASTNode>()),
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
              new ASTNode(Token.Number("6"), new List<ASTNode>()),
              new ASTNode(Token.Number("7"), new List<ASTNode>())
          }),
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
              new ASTNode(Token.Number("2"), new List<ASTNode>()),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
          }),
          new ASTNode(Token.Fraction(), new List<ASTNode>
          {
              new ASTNode(Token.Number("4"), new List<ASTNode>()),
              new ASTNode(Token.Number("5"), new List<ASTNode>())
          })
      });

      var result1 = Calculator.Evaluate(equation1);
      var result2 = Calculator.Evaluate(equation2);
      var result3 = Calculator.Evaluate(equation3);
      var resultFrac = Calculator.Evaluate(fractions);

      Assert.Equal(6, result1, 0.001);
      Assert.Equal(9, result2, 0.001);
      Assert.Equal(9, result3, 0.001);
      Assert.Equal(5.3238, resultFrac, 0.01);
    }

    [Fact]
    public void Variables()
    {
      var equation1 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("x"), new List<ASTNode>()),
                  new ASTNode(Token.Number("2"), new List<ASTNode>())
              }),
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("2"), new List<ASTNode>()),
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          }),
          new ASTNode(Token.Number("1"), new List<ASTNode>())
      });
      Dictionary<string, double> symbolTable1 = new Dictionary<string, double> { { "x", 2 } };

      var equation2 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("a"), new List<ASTNode>()),
                  new ASTNode(Token.Number("3"), new List<ASTNode>())
              }),
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("b"), new List<ASTNode>()),
                  new ASTNode(Token.Number("2"), new List<ASTNode>())
              })
          }),
          new ASTNode(Token.Variable("c"), new List<ASTNode>())
      });
      Dictionary<string, double> symbolTable2 = new Dictionary<string, double> { { "a", 2 }, { "b", 3 }, { "c", 4 } };

      var equation3 = new ASTNode(Token.Operator("+"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("-"), new List<ASTNode>
          {
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("3"), new List<ASTNode>()),
                  new ASTNode(Token.Operator("^"), new List<ASTNode>
                  {
                      new ASTNode(Token.Variable("x"), new List<ASTNode>()),
                      new ASTNode(Token.Number("2"), new List<ASTNode>())
                  })
              }),
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("4"), new List<ASTNode>()),
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          }),
          new ASTNode(Token.Number("5"), new List<ASTNode>())
      });
      Dictionary<string, double> symbolTable3 = new Dictionary<string, double> { { "x", 2 } };

      var result1 = Calculator.Evaluate(equation1, symbolTable1);
      var result2 = Calculator.Evaluate(equation2, symbolTable2);
      var result3 = Calculator.Evaluate(equation3, symbolTable3);

      Assert.Equal(9, result1, 0.001);
      Assert.Equal(21, result2, 0.001);
      Assert.Equal(9, result3, 0.001);
    }

    [Fact]
    public void Functions()
    {
      var equation1 = new ASTNode(Token.Function("sin"), new List<ASTNode>
      {
          new ASTNode(Token.Variable("x"), new List<ASTNode>())
      });
      Dictionary<string, double> symbolTable1 = new Dictionary<string, double> { { "x", 2 } };

      var equation2 = new ASTNode(Token.Function("log"), new List<ASTNode>
      {
          new ASTNode(Token.Number("100"), new List<ASTNode>()),
          new ASTNode(Token.Number("10"), new List<ASTNode>())
      });
      Dictionary<string, double> symbolTable2 = new Dictionary<string, double> { { "x", 2 } };

      var equation3 = new ASTNode(Token.Function("sqrt"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Number("4"), new List<ASTNode>()),
              new ASTNode(Token.Operator("*"), new List<ASTNode>
              {
                  new ASTNode(Token.Number("2"), new List<ASTNode>()),
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          })
      });
      Dictionary<string, double> symbolTable3 = new Dictionary<string, double> { { "x", 2 } };

      var result1 = Calculator.Evaluate(equation1, symbolTable1);
      var result2 = Calculator.Evaluate(equation2, symbolTable2);
      var result3 = Calculator.Evaluate(equation3, symbolTable3);

      Assert.Equal(0.909297, result1, 0.001);
      Assert.Equal(0.5, result2, 0.001);
      Assert.Equal(2.828427, result3, 0.001);
    }

    [Fact]
    public void CustomFunctions()
    {
      var customFunctionTable = new Dictionary<string, Func<List<double>, double>>
      {
        { "f", (args) => (Math.Pow(args[0], 2) + 2 * args[0] + 3)},
        { "g", (args) => (Math.Pow(Math.Sin(args[0]), 2) + Math.Pow(Math.Cos(args[0]), 2))},
        { "h", (args) => (Math.Sqrt(4 + 2 * args[0]))}
      };

      var equation1 = new ASTNode(Token.Function("h"), new List<ASTNode>
      {
          new ASTNode(Token.Function("f"), new List<ASTNode>
          {
              new ASTNode(Token.Function("g"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              })
          })
      });
      Dictionary<string, double> symbolTable1 = new Dictionary<string, double> { { "x", 2 } };

      var equation2 = new ASTNode(Token.Function("f"), new List<ASTNode>
      {
          new ASTNode(Token.Function("g"), new List<ASTNode>
          {
              new ASTNode(Token.Variable("x"), new List<ASTNode>())
          })
      });
      Dictionary<string, double> symbolTable2 = new Dictionary<string, double> { { "x", 2 } };

      var equation3 = new ASTNode(Token.Function("f"), new List<ASTNode>
      {
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
              new ASTNode(Token.Function("g"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("x"), new List<ASTNode>())
              }),
              new ASTNode(Token.Function("h"), new List<ASTNode>
              {
                  new ASTNode(Token.Variable("y"), new List<ASTNode>())
              })
          })
      });
      Dictionary<string, double> symbolTable3 = new Dictionary<string, double> { { "x", 2 }, { "y", 3 } };

      var result1 = Calculator.Evaluate(equation1, symbolTable1, customFunctionTable);
      var result2 = Calculator.Evaluate(equation2, symbolTable2, customFunctionTable);
      var result3 = Calculator.Evaluate(equation3, symbolTable3, customFunctionTable);

      Assert.Equal(4, result1, 0.001);
      Assert.Equal(6, result2, 0.001);
      Assert.Equal(28.64911, result3, 0.001);
    }

    [Fact]
    public void MixedConcepts()
    {
      var equation = new ASTNode(Token.Operator("-"), new List<ASTNode>
      {
        // Left: f(x^2 + 3) + g(sin(x) * ln(y))
        new ASTNode(Token.Operator("+"), new List<ASTNode>
        {
          // f(x^2 + 3)
          new ASTNode(Token.Function("f"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("+"), new List<ASTNode>
            {
              new ASTNode(Token.Operator("^"), new List<ASTNode>
              {
                new ASTNode(Token.Variable("x"), new List<ASTNode>()),
                new ASTNode(Token.Number("2"), new List<ASTNode>())
              }),
              new ASTNode(Token.Number("3"), new List<ASTNode>())
            })
          }),
          // g(sin(x) * ln(y))
          new ASTNode(Token.Function("g"), new List<ASTNode>
          {
            new ASTNode(Token.Operator("*"), new List<ASTNode>
            {
              new ASTNode(Token.Function("sin"), new List<ASTNode>
              {
                new ASTNode(Token.Variable("x"), new List<ASTNode>())
              }),
              new ASTNode(Token.Function("ln"), new List<ASTNode>
              {
                new ASTNode(Token.Variable("y"), new List<ASTNode>())
              })
            })
          })
        }),
        // Right: 4 / (x + 1)
        new ASTNode(Token.Operator("/"), new List<ASTNode>
        {
          new ASTNode(Token.Number("4"), new List<ASTNode>()),
          new ASTNode(Token.Operator("+"), new List<ASTNode>
          {
            new ASTNode(Token.Variable("x"), new List<ASTNode>()),
            new ASTNode(Token.Number("1"), new List<ASTNode>())
          })
        })
      });

      Dictionary<string, double> symbolTable = new Dictionary<string, double> { { "x", 1.75 }, { "y", 30 } };

      var customFunctionTable = new Dictionary<string, Func<List<double>, double>>
      {
        { "f", (args) => (Math.Pow(args[0], 2) + 2 * args[0] + 3)},
        { "g", (args) => (Math.Pow(Math.Sin(args[0]), 2) + Math.Cos(args[0]))},
      };

      var result = Calculator.Evaluate(equation, symbolTable, customFunctionTable);

      Assert.Equal(49.481, result, 0.01);
    }

    [Fact]
    public void RationalOperatorsEvaluateSumRationnal()
    {
      var left = Frac(1, 2);
      var right = Frac(1, 3);

      var result = Calculator.EvaluateSumRationnal(left, right);
      Assert.Equal("5", result.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("6", result.OperandAt(1).Token.Type.stringValue);
    }

    [Fact]
    public void RationalOperatorsEvaluateDiffRationnal()
    {
      var left = Frac(3, 4);
      var right = Frac(1, 4);

      var result = Calculator.EvaluateDiffRationnal(left, right);
      Assert.Equal("8", result.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("16", result.OperandAt(1).Token.Type.stringValue);
    }

    [Fact]
    public void RationalOperatorsEvaluateProductRationnal()
    {
      var left = Frac(2, 3);
      var right = Frac(3, 4);

      var result = Calculator.EvaluateProductRationnal(left, right);
      Assert.Equal("6", result.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("12", result.OperandAt(1).Token.Type.stringValue);
    }

    [Fact]
    public void RationalOperatorsEvaluateQuotientRationnal()
    {
      var left = Frac(4, 5);
      var right = Frac(2, 3);

      var result = Calculator.EvaluateQuotientRationnal(left, right);
      Assert.Equal("12", result.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("10", result.OperandAt(1).Token.Type.stringValue);
    }

    [Fact]
    public void RationalOperatorsEvaluatePowerRationnal()
    {
      var baseNode1 = Frac(2, 3);
      var exponent1 = Int(2);

      var result1 = Calculator.EvaluatePowerRationnal(baseNode1, exponent1);
      Assert.Equal("24", result1.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("54", result1.OperandAt(1).Token.Type.stringValue);

      var baseNode2 = Frac(1, 4);
      var exponent2 = Int(-3);

      var result2 = Calculator.EvaluatePowerRationnal(baseNode2, exponent2);
      Assert.Equal("256", result2.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("4", result2.OperandAt(1).Token.Type.stringValue);

      var baseNode3 = Int(5);
      var exponent3 = Int(0);

      var result3 = Calculator.EvaluatePowerRationnal(baseNode3, exponent3);
      Assert.Equal("5", result3.OperandAt(0).Token.Type.stringValue);
      Assert.Equal("5", result3.OperandAt(1).Token.Type.stringValue);
    }

    [Fact]
    public void RationalOperatorsInvalidArguments()
    {
      var valid = Int(1);
      var invalid = Invalid();

      Assert.Throws<ArgumentException>(() =>
      {
        Calculator.EvaluateSumRationnal(valid, invalid);
      });

      Assert.Throws<ArgumentException>(() =>
      {
        Calculator.EvaluateDiffRationnal(invalid, valid);
      });

      Assert.Throws<ArgumentException>(() =>
      {
        Calculator.EvaluateProductRationnal(invalid, invalid);
      });

      Assert.Throws<ArgumentException>(() =>
      {
        Calculator.EvaluateQuotientRationnal(invalid, valid);
      });

      Assert.Throws<ArgumentException>(() =>
      {
        Calculator.EvaluatePowerRationnal(invalid, valid);
      });
    }

    [Fact] void SetGCD()
    {
      var nums1 = new List<int> { 24, 36, 60 };
      int result1 = Calculator.SetGCD(nums1);

      Assert.Equal(12, result1);

      var nums2 = new List<int> { -42, 56, -70 };
      int result2 = Calculator.SetGCD(nums2);

      Assert.Equal(14, result2);

      var nums3 = new List<int> { 3, 5, 11, 35, 24 };
      int result3 = Calculator.SetGCD(nums3);

      Assert.Equal(1, result3);

      var nums4 = new List<int> { 4, 8, 24, 100, 16, 40 };
      int result4 = Calculator.SetGCD(nums4);

      Assert.Equal(4, result4);
    }
  }
}
