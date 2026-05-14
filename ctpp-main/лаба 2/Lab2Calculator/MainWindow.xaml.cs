using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lab2Calculator;

public partial class MainWindow : Window
{
    private const string DefaultStatus = "Введите выражение кнопками или с клавиатуры.";

    private readonly CalculatorEngine _calculator = new();
    private string _expression = "0";
    private bool _isResultShown;

    public MainWindow()
    {
        InitializeComponent();
        UpdateDisplay();
        SetStatus(DefaultStatus, Brushes.DimGray);
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            AddInput(button.Content.ToString() ?? string.Empty);
        }
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearExpression("Выражение очищено.");
    }

    private void BackspaceButton_OnClick(object sender, RoutedEventArgs e)
    {
        RemoveLastSymbol();
    }

    private void EqualsButton_OnClick(object sender, RoutedEventArgs e)
    {
        CalculateResult();
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        var value = e.Key switch
        {
            Key.D9 when Keyboard.Modifiers == ModifierKeys.Shift => "(",
            Key.D0 when Keyboard.Modifiers == ModifierKeys.Shift => ")",
            >= Key.D0 and <= Key.D9 => ((int)(e.Key - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => ((int)(e.Key - Key.NumPad0)).ToString(),
            Key.Add => "+",
            Key.OemPlus when Keyboard.Modifiers == ModifierKeys.Shift => "+",
            Key.Subtract or Key.OemMinus => "-",
            Key.Multiply => "*",
            Key.Divide or Key.OemQuestion => "/",
            Key.Decimal or Key.OemPeriod or Key.OemComma => ".",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(value))
        {
            AddInput(value);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Enter or Key.Return)
        {
            CalculateResult();
            e.Handled = true;
        }
        else if (e.Key == Key.Back)
        {
            RemoveLastSymbol();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ClearExpression("Выражение очищено.");
            e.Handled = true;
        }
    }

    private void AddInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (_isResultShown && char.IsDigit(value[0]))
        {
            _expression = "0";
            _isResultShown = false;
        }
        else if (_isResultShown && (value == "." || value == "("))
        {
            _expression = value == "." ? "0" : string.Empty;
            _isResultShown = false;
        }
        else if (_isResultShown && IsOperator(value))
        {
            _isResultShown = false;
        }

        if (!TryAppendValue(value))
        {
            UpdateDisplay();
            return;
        }

        UpdateDisplay();
        ShowSmartPreview();
    }

    private bool TryAppendValue(string value)
    {
        if (_expression == "0" && char.IsDigit(value[0]))
        {
            _expression = value;
            SetStatus(DefaultStatus, Brushes.DimGray);
            return true;
        }

        if (value == "." && !CanAppendDecimalPoint())
        {
            SetStatus("В одном числе можно использовать только одну десятичную точку.", Brushes.Firebrick);
            return false;
        }

        if (IsOperator(value) && EndsWithOperator() && !CanUseUnaryMinus(value))
        {
            SetStatus("Нельзя вводить два оператора подряд.", Brushes.Firebrick);
            return false;
        }

        if (value == ")" && CountOpenBrackets() <= CountCloseBrackets())
        {
            SetStatus("Закрывающая скобка не имеет пары.", Brushes.Firebrick);
            return false;
        }

        if (value == "(" && CanInsertMultiplicationBeforeBracket())
        {
            _expression += "*";
        }

        if (value == "." && (_expression == "0" || EndsWithOperator() || _expression.EndsWith("(")))
        {
            _expression += "0";
        }

        _expression = _expression == "0" && (value == "(" || value == "-")
            ? value
            : _expression + value;

        SetStatus(DefaultStatus, Brushes.DimGray);
        return true;
    }

    private void CalculateResult()
    {
        try
        {
            var result = _calculator.Evaluate(_expression);
            var resultText = _calculator.Format(result);

            HistoryListBox.Items.Insert(0, $"{_expression} = {resultText}");
            _expression = resultText;
            _isResultShown = true;
            SetStatus("Вычисление выполнено успешно.", Brushes.DarkGreen);
            UpdateDisplay();
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, Brushes.Firebrick);
        }
    }

    private void RemoveLastSymbol()
    {
        if (_isResultShown || _expression.Length <= 1)
        {
            _expression = "0";
            _isResultShown = false;
        }
        else
        {
            _expression = _expression[..^1];
        }

        UpdateDisplay();
        ShowSmartPreview();
    }

    private void ClearExpression(string status)
    {
        _expression = "0";
        _isResultShown = false;
        SetStatus(status, Brushes.DimGray);
        UpdateDisplay();
    }

    private void ShowSmartPreview()
    {
        if (!CanPreviewExpression())
        {
            SetStatus(DefaultStatus, Brushes.DimGray);
            return;
        }

        try
        {
            var preview = _calculator.Format(_calculator.Evaluate(_expression));
            SetStatus($"Подсказка: сейчас получится {preview}", Brushes.SteelBlue);
        }
        catch
        {
            SetStatus(DefaultStatus, Brushes.DimGray);
        }
    }

    private bool CanPreviewExpression()
    {
        return _expression != "0" &&
               !EndsWithOperator() &&
               !_expression.EndsWith("(") &&
               CountOpenBrackets() == CountCloseBrackets();
    }

    private void UpdateDisplay()
    {
        DisplayTextBox.Text = _expression;
    }

    private void SetStatus(string message, Brush color)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = color;
    }

    private static bool IsOperator(string value)
    {
        return value is "+" or "-" or "*" or "/";
    }

    private bool CanAppendDecimalPoint()
    {
        for (var index = _expression.Length - 1; index >= 0; index--)
        {
            var current = _expression[index];

            if (IsOperator(current.ToString()) || current is '(' or ')')
            {
                break;
            }

            if (current == '.')
            {
                return false;
            }
        }

        return true;
    }

    private bool EndsWithOperator()
    {
        return _expression.Length > 0 && IsOperator(_expression[^1].ToString());
    }

    private bool CanUseUnaryMinus(string value)
    {
        return value == "-" && !_expression.EndsWith("-");
    }

    private bool CanInsertMultiplicationBeforeBracket()
    {
        return _expression != "0" && (char.IsDigit(_expression[^1]) || _expression.EndsWith(")"));
    }

    private int CountOpenBrackets()
    {
        return _expression.Count(character => character == '(');
    }

    private int CountCloseBrackets()
    {
        return _expression.Count(character => character == ')');
    }
}
