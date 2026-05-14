using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Lab9WizardForm;

public partial class ContactPage : Page
{
    private readonly FormData _formData;

    public ContactPage(FormData formData)
    {
        InitializeComponent();
        _formData = formData;
        EmailTextBox.Text = _formData.Email;
        PhoneTextBox.Text = _formData.Phone;
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        SaveValues();
        NavigationService?.Navigate(new PersonalPage(_formData));
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!Regex.IsMatch(EmailTextBox.Text.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ErrorTextBlock.Text = "Введите корректный email.";
            return;
        }

        if (!Regex.IsMatch(PhoneTextBox.Text.Trim(), @"^\+?\d[\d\s\-()]{6,}$"))
        {
            ErrorTextBlock.Text = "Введите корректный номер телефона.";
            return;
        }

        SaveValues();
        NavigationService?.Navigate(new AddressPage(_formData));
    }

    private void SaveValues()
    {
        _formData.Email = EmailTextBox.Text.Trim();
        _formData.Phone = PhoneTextBox.Text.Trim();
    }
}
