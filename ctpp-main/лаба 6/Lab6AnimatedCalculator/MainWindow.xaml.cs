using System.Globalization;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Lab6AnimatedCalculator;

public partial class MainWindow : Window
{
    private double? _storedValue;
    private string? _pendingOperation;
    private bool _isNewInput = true;
    private bool _isResultShown;

    public MainWindow()
    {
        InitializeComponent();
        UpdateDisplay("0");
    }

    private string CurrentText => DisplayTextBlock.Text;

    private void DigitButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            AddDigit(button.Content.ToString() ?? string.Empty);
        }
    }

    private void DecimalButton_OnClick(object sender, RoutedEventArgs e)
    {
        AddDecimalPoint();
    }

    private void OperationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            SetOperation(button.Content.ToString() ?? string.Empty);
        }
    }

    private void EqualsButton_OnClick(object sender, RoutedEventArgs e)
    {
        CalculateResult();
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearCalculator();
    }

    private void BackspaceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isNewInput || _isResultShown || CurrentText.Length <= 1)
        {
            UpdateDisplay("0");
            _isNewInput = true;
            return;
        }

        UpdateDisplay(CurrentText[..^1]);
    }

    private void ToggleSignButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CurrentText == "0")
        {
            return;
        }

        UpdateDisplay(CurrentText.StartsWith('-') ? CurrentText[1..] : $"-{CurrentText}");
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        var keyText = e.Key switch
        {
            >= Key.D0 and <= Key.D9 => ((int)(e.Key - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => ((int)(e.Key - Key.NumPad0)).ToString(),
            Key.Add => "+",
            Key.OemPlus when Keyboard.Modifiers == ModifierKeys.Shift => "+",
            Key.Subtract or Key.OemMinus => "-",
            Key.Multiply => "*",
            Key.Divide or Key.OemQuestion => "/",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(keyText))
        {
            if (IsOperation(keyText))
            {
                SetOperation(keyText);
            }
            else
            {
                AddDigit(keyText);
            }

            e.Handled = true;
            return;
        }

        if (e.Key is Key.Decimal or Key.OemPeriod or Key.OemComma)
        {
            AddDecimalPoint();
            e.Handled = true;
        }
        else if (e.Key is Key.Enter or Key.Return)
        {
            CalculateResult();
            e.Handled = true;
        }
        else if (e.Key == Key.Back)
        {
            BackspaceButton_OnClick(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ClearCalculator();
            e.Handled = true;
        }
    }

    private void AddDigit(string digit)
    {
        if (string.IsNullOrWhiteSpace(digit))
        {
            return;
        }

        if (_isNewInput || CurrentText == "0" || _isResultShown)
        {
            UpdateDisplay(digit);
            _isNewInput = false;
            _isResultShown = false;
        }
        else
        {
            UpdateDisplay(CurrentText + digit);
        }

        SetStatus("Число введено. Выберите операцию или нажмите =.");
    }

    private void AddDecimalPoint()
    {
        if (_isNewInput || _isResultShown)
        {
            UpdateDisplay("0.");
            _isNewInput = false;
            _isResultShown = false;
            return;
        }

        if (!CurrentText.Contains('.'))
        {
            UpdateDisplay(CurrentText + ".");
        }
    }

    private void SetOperation(string operation)
    {
        if (!TryGetCurrentNumber(out var current))
        {
            return;
        }

        if (_storedValue is not null && _pendingOperation is not null && !_isNewInput)
        {
            current = ExecuteOperation(_storedValue.Value, current, _pendingOperation);
            UpdateDisplay(FormatNumber(current));
            RunDisplayResultAnimation();
        }

        _storedValue = current;
        _pendingOperation = operation;
        _isNewInput = true;
        _isResultShown = false;
        ExpressionTextBlock.Text = $"{FormatNumber(_storedValue.Value)} {operation}";
        SetStatus("Операция выбрана. Введите следующее число.");
    }

    private void CalculateResult()
    {
        if (_storedValue is null || _pendingOperation is null)
        {
            RunDisplayResultAnimation();
            return;
        }

        if (!TryGetCurrentNumber(out var current))
        {
            return;
        }

        try
        {
            var result = ExecuteOperation(_storedValue.Value, current, _pendingOperation);
            ExpressionTextBlock.Text = $"{FormatNumber(_storedValue.Value)} {_pendingOperation} {FormatNumber(current)} =";
            UpdateDisplay(FormatNumber(result));
            _storedValue = null;
            _pendingOperation = null;
            _isNewInput = true;
            _isResultShown = true;
            SetStatus("Результат вычислен. Дисплей сменился с анимацией.");
            RunDisplayResultAnimation();
            SystemSounds.Asterisk.Play();
        }
        catch (DivideByZeroException exception)
        {
            ShowError(exception.Message);
        }
    }

    private void ClearCalculator()
    {
        _storedValue = null;
        _pendingOperation = null;
        _isNewInput = true;
        _isResultShown = false;
        ExpressionTextBlock.Text = "Готов к вычислениям";
        UpdateDisplay("0");
        SetStatus("Дисплей очищен с анимацией.");
        RunDisplayClearAnimation();
    }

    private bool TryGetCurrentNumber(out double number)
    {
        if (double.TryParse(CurrentText, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return true;
        }

        ShowError("Не удалось распознать число на дисплее.");
        return false;
    }

    private static double ExecuteOperation(double left, double right, string operation)
    {
        return operation switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" when Math.Abs(right) < 0.0000001 => throw new DivideByZeroException("Деление на ноль запрещено."),
            "/" => left / right,
            _ => right
        };
    }

    private void UpdateDisplay(string text)
    {
        DisplayTextBlock.Text = text;
    }

    private void ShowError(string message)
    {
        SetStatus(message);
        ExpressionTextBlock.Text = "Ошибка";
        DisplayTextBlock.Text = "0";
        _storedValue = null;
        _pendingOperation = null;
        _isNewInput = true;
        _isResultShown = false;
        SystemSounds.Hand.Play();
        RunDisplayClearAnimation();
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void RunDisplayResultAnimation()
    {
        if (Resources["DisplayResultStoryboard"] is Storyboard storyboard)
        {
            storyboard.Begin(this, true);
        }
    }

    private void RunDisplayClearAnimation()
    {
        if (Resources["DisplayClearStoryboard"] is Storyboard storyboard)
        {
            storyboard.Begin(this, true);
        }
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##########", CultureInfo.InvariantCulture);
    }

    private static bool IsOperation(string value)
    {
        return value is "+" or "-" or "*" or "/";
    }
}
