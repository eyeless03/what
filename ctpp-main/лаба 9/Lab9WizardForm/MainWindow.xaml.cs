using System.Windows;
using System.Windows.Navigation;

namespace Lab9WizardForm;

public partial class MainWindow : Window
{
    private readonly FormData _formData = new();

    public MainWindow()
    {
        InitializeComponent();
        MainFrame.Navigate(new PersonalPage(_formData));
    }

    private void MainFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        StepTextBlock.Text = e.Content switch
        {
            PersonalPage => "Шаг 1 из 3",
            ContactPage => "Шаг 2 из 3",
            AddressPage => "Шаг 3 из 3",
            _ => StepTextBlock.Text
        };
    }
}
