﻿using Lsj.Util.Win32.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WindowDebugger.Converters
{
    public class WindowStylesToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is WindowStyles styles && Enum.TryParse<WindowStyles>((string)values[1], out var style))
            {
                return style == 0 || (styles & style) != 0;
            }
            return DependencyProperty.UnsetValue;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
