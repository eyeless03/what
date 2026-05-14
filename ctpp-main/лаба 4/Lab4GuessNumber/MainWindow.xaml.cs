using System.Windows;

namespace Lab4GuessNumber;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new GameViewModel();
    }
}
