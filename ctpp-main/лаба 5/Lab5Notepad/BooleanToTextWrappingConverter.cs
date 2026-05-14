using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lab5Notepad;

public sealed class BooleanToTextWrappingConverter : IValueConverter
{
    public static BooleanToTextWrappingConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TextWrapping.Wrap;
    }
}
