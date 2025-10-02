using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace KeyCatcher.converters
{

    public class BothDownToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return false;
            bool isBleUp = values[0] is bool b1 && b1;
            bool isWifiUp = values[1] is bool b2 && b2;
            return !(isBleUp || isWifiUp);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public enum LinkState
    {
        Off,      // not connected and not trying
        Trying,   // attempting to connect
        On,       // connected and working
        Error     // failed after retries
    }



    public sealed class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || parameter is null) return false;
            var s = parameter.ToString();
            return string.Equals(value.ToString(), s, StringComparison.Ordinal);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter is string s)
            {
                // Convert the string parameter back to the enum type
                try
                {
                    if (targetType.IsEnum)
                        return Enum.Parse(targetType, s, ignoreCase: false);

                    // When bound to object, try to infer enum type from the current source property
                    return s;
                }
                catch { /* fall through */ }
            }
            return Binding.DoNothing;
        }
    }
    public sealed class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Colors.LimeGreen;
        public Color FalseColor { get; set; } = Colors.Gray;

        public object Convert(object value, Type t, object parameter, CultureInfo culture)
            => (value is bool b && b) ? TrueColor : FalseColor;

        public object ConvertBack(object value, Type t, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
    public class IconByStateConverter : IValueConverter
    {
        public Color Off { get; set; }
        public Color On { get; set; }
        public Color Trying { get; set; }
        public Color Error { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LinkState state)
            {
                return state switch
                {
                    LinkState.Off => Off,
                    LinkState.On => On,
                    LinkState.Trying => Trying,
                    LinkState.Error => Error,
                    _ => Off
                };
            }
            return Off;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    public class LinkStateToColorConverter : IValueConverter
    {
        public Color OffColor { get; set; }
        public Color TryingColor { get; set; }
        public Color OnColor { get; set; }
        public Color ErrorColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is LinkState s
                ? s switch
                {
                    LinkState.Off => OffColor,
                    LinkState.Trying => TryingColor,
                    LinkState.On => OnColor,
                    LinkState.Error => ErrorColor,
                    _ => OffColor
                }
                : OffColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public sealed class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? 1.0 : 0.3;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    //public class EnumToBoolConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //        => value?.ToString() == parameter?.ToString();

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //        => (value is bool b && b) ? Enum.Parse(targetType, parameter.ToString()) : Binding.DoNothing;
    //}

    public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = (parameter as string)?.Split(',');
        if (param?.Length == 2 && value is bool b)
            return b ? param[0] : param[1];
        return "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}

public class StringEqualsToBoolConverter : IValueConverter
{
    // VM → RadioButton-IsChecked
    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
        => Equals(value?.ToString(), parameter?.ToString());

    // RadioButton back → VM  (only when checked == true)
    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => (bool)value ? parameter?.ToString() : Binding.DoNothing;
}
}
//public class StringToBoolConverter : IValueConverter
//{
//    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//        => value as string == parameter as string;

//    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//        => (bool)value ? parameter as string : Binding.DoNothing;
//}

#if ANDROID
public class BluetoothScanPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new List<(string, bool)>
        {
            ("android.permission.BLUETOOTH_SCAN", true),
        }.ToArray();
}

public class BluetoothConnectPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new List<(string, bool)>
        {
            ("android.permission.BLUETOOTH_CONNECT", true),
        }.ToArray();
}
#endif



