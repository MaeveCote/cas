using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CAS.Core;

namespace CAS.Desktop.Views
{
  /// <summary>
  /// The main page of the app.
  /// </summary>
  public sealed partial class MainPage : Page
  {
    private Dictionary<string, double> SymbolTable { get; set; }
    private string CurrentAction { get; set; }

    private System.Timers.Timer debounceTimer;

    public MainPage()
    {
      InitializeComponent();
      SymbolTable = new Dictionary<string, double>();
      CurrentAction = "NoAction";
      debounceTimer = new System.Timers.Timer();
      SetWebViews();
    }

    private async void SetWebViews()
    {
      await InputWebView.EnsureCoreWebView2Async();
      await OutputWebView.EnsureCoreWebView2Async();
    }

    #region OnClick events

    private void ModeToggle_Click(object sender, RoutedEventArgs e)
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

    private void AddVariable_Click(object sender, RoutedEventArgs e)
    {
      string name = VarNameBox.Text.Trim();
      string value = VarValueBox.Text.Trim();
      double numValue;

      if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
      {
        if (!double.TryParse(value, out numValue))
        {
          Logger.LogWarn("The new variable value should be a decimal number.");
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
          Child = new TextBlock
          {
            Text = $"{name} = {value}",
            Foreground = (Brush)Application.Current.Resources["TextBrush"]
          }
        };
        VariableList.Children.Add(border);
        SymbolTable.Add(name, numValue);

        // Clear inputs
        VarNameBox.Text = "";
        VarValueBox.Text = "";
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
            InputWebView.NavigateToString(GenerateMathJaxHtml(InputTextBox.Text));
          });
      };
      debounceTimer.Start();
    }

    private async void RunAction(object sender, RoutedEventArgs e)
    {
      // Select the action
      if (CurrentAction == "Eval")
      {
        Logger.LogInfo("Running 'Eval'.");

      }
      else if (CurrentAction == "Simplify")
      {
        Logger.LogInfo("Running 'Simplify'.");

      }
      else if (CurrentAction == "Expand")
      {
        Logger.LogInfo("Running 'Expand'.");

      }
      else
      {
        // Unknown action
        Logger.LogInfo("No action selected, skipping the run.");
        return;
      }





      string inputLatex = "\\frac{1}{x} + \\sqrt{2}";
      string outputLatex = "\\frac{1}{x^2} - \\sqrt{2}";

      await InputWebView.EnsureCoreWebView2Async();
      InputWebView.NavigateToString(GenerateMathJaxHtml(inputLatex));

      await OutputWebView.EnsureCoreWebView2Async();
      OutputWebView.NavigateToString(GenerateMathJaxHtml(outputLatex));
    }
  }
}
