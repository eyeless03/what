using System.Windows;
using System.Windows.Controls;

namespace Lab9WizardForm;

public partial class PersonalPage : Page
{
    private readonly FormData _formData;

    public PersonalPage(FormData formData)
    {
        InitializeComponent();
        _formData = formData;
        FirstNameTextBox.Text = _formData.FirstName;
        LastNameTextBox.Text = _formData.LastName;
        BirthDatePicker.SelectedDate = _formData.BirthDate;
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) || string.IsNullOrWhiteSpace(LastNameTextBox.Text))
        {
            ErrorTextBlock.Text = "Имя и фамилия обязательны для заполнения.";
            return;
        }

        if (BirthDatePicker.SelectedDate is null)
        {
            ErrorTextBlock.Text = "Выберите дату рождения.";
            return;
        }

        _formData.FirstName = FirstNameTextBox.Text.Trim();
        _formData.LastName = LastNameTextBox.Text.Trim();
        _formData.BirthDate = BirthDatePicker.SelectedDate;
        NavigationService?.Navigate(new ContactPage(_formData));
    }
}
