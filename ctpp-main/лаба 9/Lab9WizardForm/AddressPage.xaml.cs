using System.Windows;
using System.Windows.Controls;

namespace Lab9WizardForm;

public partial class AddressPage : Page
{
    private readonly FormData _formData;

    public AddressPage(FormData formData)
    {
        InitializeComponent();
        _formData = formData;
        CityTextBox.Text = _formData.City;
        StreetTextBox.Text = _formData.Street;
        HouseTextBox.Text = _formData.House;
        ApartmentTextBox.Text = _formData.Apartment;
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        SaveValues();
        NavigationService?.Navigate(new ContactPage(_formData));
    }

    private void SubmitButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CityTextBox.Text) ||
            string.IsNullOrWhiteSpace(StreetTextBox.Text) ||
            string.IsNullOrWhiteSpace(HouseTextBox.Text))
        {
            ErrorTextBlock.Text = "Город, улица и номер дома обязательны.";
            return;
        }

        SaveValues();
        var apartment = string.IsNullOrWhiteSpace(_formData.Apartment) ? "не указана" : _formData.Apartment;
        var message =
            $"Имя: {_formData.FirstName}\n" +
            $"Фамилия: {_formData.LastName}\n" +
            $"Дата рождения: {_formData.BirthDate:dd.MM.yyyy}\n\n" +
            $"Email: {_formData.Email}\n" +
            $"Телефон: {_formData.Phone}\n\n" +
            $"Город: {_formData.City}\n" +
            $"Улица: {_formData.Street}\n" +
            $"Дом: {_formData.House}\n" +
            $"Квартира: {apartment}";

        MessageBox.Show(message, "Введенные данные", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveValues()
    {
        _formData.City = CityTextBox.Text.Trim();
        _formData.Street = StreetTextBox.Text.Trim();
        _formData.House = HouseTextBox.Text.Trim();
        _formData.Apartment = ApartmentTextBox.Text.Trim();
    }
}
