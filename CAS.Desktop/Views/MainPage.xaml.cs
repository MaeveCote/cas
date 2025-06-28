using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CAS.Core;
using CAS.Core.EquationParsing;
using Windows.ApplicationModel.Chat;
using Windows.UI;

namespace CAS.Desktop.Views
{
  /// <summary>
  /// The main page of the app.
  /// </summary>
  public sealed partial class MainPage : Page
  {
    public const int POPUP_DELAY = 5000;

    private Dictionary<string, double> SymbolTable { get; set; }
    private Dictionary<string, Func<List<double>, double>> CustomFunctions { get; set; }
    private string CurrentAction { get; set; }
    private Simplifier simplifier { get; set; }

    private Timer debounceTimer;

    public MainPage()
    {
      InitializeComponent();
      SymbolTable = new Dictionary<string, double>();
      CustomFunctions = new Dictionary<string, Func<List<double>, double>>();
      CurrentAction = "NoAction";
      debounceTimer = new Timer();
      simplifier = new Simplifier();
      SetWebViews();
    }

    #region OnClick Events

    private void OnClickActionToggle(object sender, RoutedEventArgs e)
    {
      if (sender is ToggleButton button && button.Content != null)
        CurrentAction = button.Content.ToString();
      else
      {
        Logger.LogError("'ModeToggle_Click' should only be triggered on a 'ToggleButton' element.");
        return;
      }

      foreach (var child in ActionPanel.Children)
      {
        if (child is ToggleButton btn && btn != sender)
        {
          btn.IsChecked = false;
        }
      }
    }

    private void OnClickAddVariable(object sender, RoutedEventArgs e)
    {
      string name = VarNameBox.Text.Trim();
      string value = VarValueBox.Text.Trim();
      double numValue;

      if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
      {
        if (!double.TryParse(value, out numValue))
        {
          ShowErrorPopup("The new variable value should be a decimal number.");
          return;
        }

        if (!name.All(char.IsLetter))
        {
          ShowErrorPopup("The new variable name should be a symbol.");
          return;
        }

        foreach (var child in VariableList.Children)
        {
          if (child is Border bor && bor.Name == name)
          {
            // Already exists
            // Change the value in the UI and in the dictionnary
            bor.Child = new TextBlock
            {
              Text = $"{name} = {value}",
              Foreground = (Brush)Application.Current.Resources["TextBrush"]
            };

            SymbolTable[name] = numValue;

            // Clear inputs
            VarNameBox.Text = "";
            VarValueBox.Text = "";
            return;
          }
        }

        // Create a new element
        var border = new Border
        {
          Name = $"{name}",
          Background = (Brush)Application.Current.Resources["Surface1Brush"],
          CornerRadius = new CornerRadius(10),
          BorderThickness = new Thickness(1),
          BorderBrush = (Brush)Application.Current.Resources["Surface1Brush"],
          Padding = new Thickness(10),
        };

        // Inner stack to hold text and button
        var horizontalPanel = new StackPanel
        {
          Orientation = Orientation.Horizontal,
          VerticalAlignment = VerticalAlignment.Center
        };

        var textBlock = new TextBlock
        {
          Text = $"{name} = {value}",
          Foreground = (Brush)Application.Current.Resources["TextBrush"],
          VerticalAlignment = VerticalAlignment.Center
        };

        var deleteButton = new Button
        {
          Content = "✖",
          Background = (Brush)Application.Current.Resources["RedBrush"],
          Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)), // black
          Margin = new Thickness(10, 0, 0, 0),
          Width = 32,
          Height = 32,
          FontSize = 14,
          Padding = new Thickness(0)
        };

        deleteButton.Click += (s, args) =>
                {
                  VariableList.Children.Remove(border);
                  SymbolTable.Remove(name);
                };

        horizontalPanel.Children.Add(textBlock);
        horizontalPanel.Children.Add(deleteButton);
        border.Child = horizontalPanel;

        VariableList.Children.Add(border);
        SymbolTable.Add(name, numValue);

        // Clear inputs
        VarNameBox.Text = "";
        VarValueBox.Text = "";
      }
    }

    private void OnClickAddFunction(object sender, RoutedEventArgs e)
    {
      var name = FuncNameBox.Text.Trim();
      var value = FuncValueBox.Text.Trim();

      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
      {
        ShowErrorPopup("The name or expression is not valid, can't create a function.");
        return;
      }

      var match = Regex.Match(name, @"^([A-Za-z]+)\(([A-Za-z](,[A-Za-z])*)\)$");
      if (!match.Success)
      {
        ShowErrorPopup("Invalid function name, the function should be in letters and describe it's variables. E.G. f(x,y,z).");
        return;
      }

      string functionName = "";
      try
      {
        functionName = match.Groups[1].Value;
        string argsString = match.Groups[2].Value;
        var functionArgs = argsString.Split(',');

        // Convert the expression to a tree
        var tokenizerResult = StringTokenizer.Tokenize(value);

        foreach (var symbol in tokenizerResult.Symbols)
        {
          bool found = true;
          foreach (var arg in functionArgs)
          {
            if (arg == symbol)
              found = true;
          }
          if (found == false)
            throw new ArgumentException("A variable in the expression is not declared in the function name.");
        }

        var tree = ASTBuilder.ParseInfixToAST(tokenizerResult.TokenizedExpression);

        CustomFunctions.Add(functionName, (args) =>
        {
          Dictionary<string, double> functionSymbols = new Dictionary<string, double>();
          for (int i = 0; i < args.Count(); i++)
            functionSymbols.Add(functionArgs[i], args[i]);
          return Calculator.Evaluate(tree, functionSymbols, CustomFunctions);
        });
      }
      catch (Exception ex)
      {
        ShowErrorPopup(ex.Message);
        return;
      }

      // Create UI element for the function
      var border = new Border
      {
        Background = (Brush)Application.Current.Resources["Surface1Brush"],
        CornerRadius = new CornerRadius(10),
        BorderThickness = new Thickness(1),
        BorderBrush = (Brush)Application.Current.Resources["Surface1Brush"],
        Padding = new Thickness(5),
      };

      var horizontalPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center
      };

      var textBlock = new TextBlock
      {
        Text = $"{name} = {value}",
        Foreground = (Brush)Application.Current.Resources["TextBrush"],
        VerticalAlignment = VerticalAlignment.Center
      };

      var deleteButton = new Button
      {
        Content = "✖",
        Background = (Brush)Application.Current.Resources["AccentBrush"],
        Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
        Margin = new Thickness(10, 0, 0, 0),
        Width = 32,
        Height = 32,
        FontSize = 14,
        Padding = new Thickness(0)
      };

      deleteButton.Click += (s, e) =>
      {
        FunctionList.Children.Remove(border);
        CustomFunctions.Remove(functionName);
        };

      horizontalPanel.Children.Add(textBlock);
      horizontalPanel.Children.Add(deleteButton);

      border.Child = horizontalPanel;
      FunctionList.Children.Add(border);

      FuncNameBox.Text = "";
      FuncValueBox.Text = "";
    }

    private void OnClickSettingsToggle(object sender, RoutedEventArgs e)
    {
      if (SettingsSidebar.Visibility == Visibility.Visible)
      {
        SettingsSidebar.Visibility = Visibility.Collapsed;
      }
      else
      {
        SettingsSidebar.Visibility = Visibility.Visible;
      }
    }

    #endregion

    #region OnCheck/UnCheck Events

    private void OnCheckLightModeCheckBox(Object sender, RoutedEventArgs e) => this.RequestedTheme = ElementTheme.Light;
    private void OnUncheckLightModeCheckBox(Object sender, RoutedEventArgs e) => this.RequestedTheme = ElementTheme.Dark;

    private void OnCheckSimplifyFunctionEval(Object sender, RoutedEventArgs e) => simplifier.SetSimplifierEvalFunctions(true);
    private void OnUncheckSimplifyFunctionEval(Object sender, RoutedEventArgs e) => simplifier.SetSimplifierEvalFunctions(false);

    private void OnCheckUseRadians(Object sender, RoutedEventArgs e) => simplifier.SetUseRadians(true);
    private void OnUncheckUseRadians(Object sender, RoutedEventArgs e) => simplifier.SetUseRadians(false);

    private void OnCheckDecimal2Rational(Object sender, RoutedEventArgs e) => simplifier.SetApplyDecimal2RationnalConverstion(true);
    private void OnUncheckDecimal2rational(Object sender, RoutedEventArgs e) => simplifier.SetApplyDecimal2RationnalConverstion(false);

    #endregion

    #region Show Events

    private void ShowVariablesPanel(object sender, RoutedEventArgs e)
    {
      VariablesPanel.Visibility = Visibility.Visible;
      FunctionsPanel.Visibility = Visibility.Collapsed;
      SetPanelButtonStyles(true);
    }

    private void ShowFunctionsPanel(object sender, RoutedEventArgs e)
    {
      VariablesPanel.Visibility = Visibility.Collapsed;
      FunctionsPanel.Visibility = Visibility.Visible;
      SetPanelButtonStyles(false);
    }

    private void ShowErrorPopup(string message)
    {
      ErrorPopupText.Text = message;
      ErrorPopup.IsOpen = true;
      Logger.LogError(message);

      // Optionally auto-hide after a delay
      _ = HideErrorPopupAfterDelay();
    }

    private async Task HideErrorPopupAfterDelay()
    {
      await Task.Delay(POPUP_DELAY);
      ErrorPopup.IsOpen = false;
    }

    private void SetPanelButtonStyles(bool isVariablesActive)
    {
      if (isVariablesActive)
      {
        VariablesButton.Background = (Brush)Application.Current.Resources["AccentBrush"];
        VariablesButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

        FunctionsButton.Background = (Brush)Application.Current.Resources["Surface1Brush"];
        FunctionsButton.Foreground = (Brush)Application.Current.Resources["TextBrush"];
      }
      else
      {
        FunctionsButton.Background = (Brush)Application.Current.Resources["AccentBrush"];
        FunctionsButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

        VariablesButton.Background = (Brush)Application.Current.Resources["Surface1Brush"];
        VariablesButton.Foreground = (Brush)Application.Current.Resources["TextBrush"];
      }
    }
    #endregion

    private string GenerateMathJaxHtml(string latex)
    {
      return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <script>
        window.MathJax = {{
            tex: {{
                inlineMath: [['$', '$'], ['\\(', '\\)']],
                displayMath: [['$$', '$$'], ['\\[', '\\]']]
            }},
            svg: {{ fontCache: 'global' }}
        }};
    </script>
    <script src='https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-svg.js'></script>
    <style>
        html, body {{
            margin: 0;
            padding: 0;
            background-color: transparent;
            color: #eaeefa; /* Catppuccin Text */
            font-family: sans-serif;
            font-size: 24px;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div>
        $$ {latex} $$
    </div>
</body>
</html>";
    }

    private void RenderInputLaTeX(object sender, TextChangedEventArgs e)
    {
      debounceTimer?.Stop();

      debounceTimer = new System.Timers.Timer(250); // Debounce delay in milliseconds
      debounceTimer.AutoReset = false;
      debounceTimer.Elapsed += (_, __) =>
      {
        DispatcherQueue.TryEnqueue(() =>
        {
          try
          {
            var tokenizerResult = StringTokenizer.Tokenize(InputTextBox.Text);
            var tree = ASTBuilder.ParseInfixToAST(tokenizerResult.TokenizedExpression);
            InputWebView.NavigateToString(GenerateMathJaxHtml(tree.ToLatex()));
          }
          catch { /* Ignore the errors, just don't render */ }
        });
      };
      debounceTimer.Start();
    }

    private async void SetWebViews()
    {
      await InputWebView.EnsureCoreWebView2Async();
      await OutputWebView.EnsureCoreWebView2Async();
    }

    private async void RenderOutputLaTeX(string outputLatex)
    {
      await OutputWebView.EnsureCoreWebView2Async();
      OutputWebView.NavigateToString(GenerateMathJaxHtml(outputLatex));
    }

    private void RunAction(object sender, RoutedEventArgs e)
    {
      string resultLatex = "";
      var equation = InputTextBox.Text;

      // Select the action
      if (CurrentAction == "Eval")
      {
        try
        {
          Logger.LogInfo("Running 'Eval'");

          var tokenizerResult = StringTokenizer.Tokenize(equation);
          if (!tokenizerResult.Symbols.All(key => SymbolTable.ContainsKey(key)))
            throw new Exception("There are variables in the equation with no corresponding value in the varible dictionnary. Please add all the variables to the dictionnary.");

          var tree = ASTBuilder.ParseInfixToAST(tokenizerResult.TokenizedExpression);

          resultLatex = Calculator.Evaluate(tree, SymbolTable, CustomFunctions).ToString();
        }
        catch (Exception ex)
        {
          ShowErrorPopup(ex.Message);
          return;
        }
      }
      else if (CurrentAction == "Simplify")
      {
        try
        {
          Logger.LogInfo("Running 'Simplify'");

          var tokenizerResult = StringTokenizer.Tokenize(equation);
          var tree = ASTBuilder.ParseInfixToAST(tokenizerResult.TokenizedExpression);

          simplifier.FormatTree(tree);
          var simplifiedTree = simplifier.AutomaticSimplify(tree);
          resultLatex = simplifiedTree.ToLatex();
        }
        catch (Exception ex)
        {
          ShowErrorPopup(ex.Message);
          return;
        }
      }
      else if (CurrentAction == "Expand")
      {
        try
        {
          Logger.LogInfo("Running 'Expand'");

          var tokenizerResult = StringTokenizer.Tokenize(equation);
          var tree = ASTBuilder.ParseInfixToAST(tokenizerResult.TokenizedExpression);

          simplifier.FormatTree(tree);
          var simplifiedTree = simplifier.Expand(tree);
          resultLatex = simplifiedTree.ToLatex();
        }
        catch (Exception ex)
        {
          ShowErrorPopup(ex.Message);
          return;
        }
      }
      else
      {
        // Unknown action
        var message = "No action selected, skipping the run.";
        Logger.LogInfo(message);
        ShowErrorPopup(message);
        return;
      }

      RenderOutputLaTeX(resultLatex);
    }
  }
}
