using System.Windows;

namespace Lab8LocalizedNotepad;

public partial class MainWindow : Window
{
    private readonly EditorViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void Window_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = !_viewModel.ConfirmClose();
    }
}
