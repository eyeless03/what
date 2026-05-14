using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lab1WpfHello;

// Класс MainWindow описывает логику главного окна WPF-приложения.
public partial class MainWindow : Window
{
    // Стандартный текст, который отображается до ввода имени.
    private const string DefaultGreeting = "Hello, World!";

    public MainWindow()
    {
        // InitializeComponent загружает интерфейс из файла MainWindow.xaml.
        InitializeComponent();
        ResetInterface();
    }

    // Обработчик кнопки "Показать".
    private void ShowGreetingButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Перед выводом приветствия проверяем корректность введенного имени.
        if (!TryGetValidatedName(out var trimmedName, out var errorMessage))
        {
            ShowValidationError(errorMessage);
            return;
        }

        ValidationMessageTextBlock.Text = "Данные введены корректно.";
        ValidationMessageTextBlock.Foreground = Brushes.DarkGreen;
        GreetingTextBlock.Text = $"Hello, {trimmedName}!";
    }

    // Обработчик кнопки "Сброс".
    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        ResetInterface();
    }

    // Событие вызывается каждый раз, когда пользователь меняет текст в поле ввода.
    private void NameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        // Подсказываем пользователю возможную проблему еще до нажатия кнопки.
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ValidationMessageTextBlock.Text = "Введите имя, чтобы получить персональное приветствие.";
            ValidationMessageTextBlock.Foreground = Brushes.Firebrick;
            GreetingTextBlock.Text = DefaultGreeting;
            return;
        }

        if (NameTextBox.Text.Any(char.IsDigit))
        {
            ValidationMessageTextBlock.Text = "Имя не должно содержать цифры.";
            ValidationMessageTextBlock.Foreground = Brushes.Firebrick;
            return;
        }

        ValidationMessageTextBlock.Text = "Нажмите \"Показать\", чтобы обновить приветствие.";
        ValidationMessageTextBlock.Foreground = Brushes.SteelBlue;
    }

    // Метод проверяет корректность введенного имени и возвращает результат проверки.
    private bool TryGetValidatedName(out string trimmedName, out string errorMessage)
    {
        trimmedName = NameTextBox.Text.Trim();
        errorMessage = string.Empty;

        // Набор простых проверок реализует "защиту от дурака".
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            errorMessage = "Поле имени пустое. Сначала введите имя.";
            return false;
        }

        if (trimmedName.Length < 2)
        {
            errorMessage = "Имя слишком короткое. Введите хотя бы 2 символа.";
            return false;
        }

        if (trimmedName.Any(char.IsDigit))
        {
            errorMessage = "Имя не должно содержать цифры.";
            return false;
        }

        if (!trimmedName.Any(char.IsLetter))
        {
            errorMessage = "Имя должно содержать хотя бы одну букву.";
            return false;
        }

        return true;
    }

    // Метод выводит сообщение об ошибке и возвращает стандартный текст приветствия.
    private void ShowValidationError(string message)
    {
        ValidationMessageTextBlock.Text = message;
        ValidationMessageTextBlock.Foreground = Brushes.Firebrick;
        GreetingTextBlock.Text = DefaultGreeting;
    }

    // Метод используется для полной очистки формы.
    private void ResetInterface()
    {
        // Возвращаем интерфейс в исходное состояние.
        NameTextBox.Text = string.Empty;
        GreetingTextBlock.Text = DefaultGreeting;
        ValidationMessageTextBlock.Text = "Введите имя, затем нажмите кнопку.";
        ValidationMessageTextBlock.Foreground = Brushes.Firebrick;
    }
}
