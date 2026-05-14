using System.Windows;
using System.Windows.Controls;

namespace Lab7CustomButtons;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void DemoButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            StatusTextBlock.Text = $"Нажата кнопка: {button.Content}";
        }
    }
}
