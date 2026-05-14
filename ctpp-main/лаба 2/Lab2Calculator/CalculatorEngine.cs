using System.Globalization;

namespace Lab2Calculator;

public sealed class CalculatorEngine
{
    private static readonly Dictionary<string, Operation> Operations = new()
    {
        ["+"] = new Operation(1, (left, right) => left + right),
        ["-"] = new Operation(1, (left, right) => left - right),
        ["*"] = new Operation(2, (left, right) => left * right),
        ["/"] = new Operation(2, Divide)
    };

    public double Evaluate(string expression)
    {
        var tokens = Tokenize(expression);
        var postfix = ConvertToPostfix(tokens);
        return CalculatePostfix(postfix);
    }

    public string Format(double value)
    {
        return value.ToString("0.##########", CultureInfo.InvariantCulture);
    }

    private static double Divide(double left, double right)
    {
        if (Math.Abs(right) < 0.0000001)
        {
            throw new DivideByZeroException("Деление на ноль запрещено.");
        }

        return left / right;
    }

    private static List<string> Tokenize(string expression)
    {
        var tokens = new List<string>();
        var number = string.Empty;
        var previousToken = string.Empty;

        foreach (var character in expression.Replace(',', '.'))
        {
            if (char.IsDigit(character) || character == '.')
            {
                number += character;
                continue;
            }

            if (!string.IsNullOrEmpty(number))
            {
                tokens.Add(number);
                previousToken = number;
                number = string.Empty;
            }

            if (character == ' ')
            {
                continue;
            }

            var token = character.ToString();

            if (token == "-" && (tokens.Count == 0 || IsOperator(previousToken) || previousToken == "("))
            {
                number = "-";
                continue;
            }

            if (token == "(" && IsValueToken(previousToken))
            {
                tokens.Add("*");
            }

            if (IsOperator(token) || token is "(" or ")")
            {
                tokens.Add(token);
                previousToken = token;
                continue;
            }

            throw new InvalidOperationException("Обнаружен недопустимый символ в выражении.");
        }

        if (number == "-")
        {
            throw new InvalidOperationException("Минус должен стоять перед числом.");
        }

        if (!string.IsNullOrEmpty(number))
        {
            tokens.Add(number);
        }

        return tokens;
    }

    private static List<string> ConvertToPostfix(List<string> tokens)
    {
        var output = new List<string>();
        var operators = new Stack<string>();

        foreach (var token in tokens)
        {
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                output.Add(token);
                continue;
            }

            if (token == "(")
            {
                operators.Push(token);
                continue;
            }

            if (token == ")")
            {
                while (operators.Count > 0 && operators.Peek() != "(")
                {
                    output.Add(operators.Pop());
                }

                if (operators.Count == 0 || operators.Pop() != "(")
                {
                    throw new InvalidOperationException("Скобки расставлены некорректно.");
                }

                continue;
            }

            while (operators.Count > 0 &&
                   operators.Peek() != "(" &&
                   Operations[operators.Peek()].Priority >= Operations[token].Priority)
            {
                output.Add(operators.Pop());
            }

            operators.Push(token);
        }

        while (operators.Count > 0)
        {
            var operation = operators.Pop();

            if (operation is "(" or ")")
            {
                throw new InvalidOperationException("Скобки расставлены некорректно.");
            }

            output.Add(operation);
        }

        return output;
    }

    private static double CalculatePostfix(List<string> postfix)
    {
        var values = new Stack<double>();

        foreach (var token in postfix)
        {
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
            {
                values.Push(number);
                continue;
            }

            if (values.Count < 2)
            {
                throw new InvalidOperationException("Выражение введено некорректно.");
            }

            var right = values.Pop();
            var left = values.Pop();
            values.Push(Operations[token].Calculate(left, right));
        }

        if (values.Count != 1)
        {
            throw new InvalidOperationException("Выражение введено некорректно.");
        }

        return values.Pop();
    }

    private static bool IsOperator(string value)
    {
        return Operations.ContainsKey(value);
    }

    private static bool IsValueToken(string token)
    {
        return double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out _) || token == ")";
    }

    private sealed record Operation(int Priority, Func<double, double, double> Calculate);
}
