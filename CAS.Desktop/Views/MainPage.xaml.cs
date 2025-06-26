using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CAS.Desktop.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainPage : Page
  {
    public MainPage()
    {
      InitializeComponent();
    }

    private void ModeToggle_Click(object sender, RoutedEventArgs e)
    {
      foreach (var child in ModePanel.Children)
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

      if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
      {
        var border = new Border
        {
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

        // Clear inputs
        VarNameBox.Text = "";
        VarValueBox.Text = "";
      }
    }
  }
}
