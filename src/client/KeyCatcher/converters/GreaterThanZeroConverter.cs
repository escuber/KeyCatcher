
using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace KeyCatcher.converters
{
    public sealed class LessThanConverter : IValueConverter
    {
        // Returns true if (int)value < (int)parameter
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var count = System.Convert.ToInt32(value);
                var limit = System.Convert.ToInt32(parameter);
                return count < limit;
            }
            catch { return false; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i && i > 0;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as string == parameter as string;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter as string;
            return Binding.DoNothing;
        }
    }
}