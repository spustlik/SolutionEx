using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SolutionExtensions.UI
{
    public class EmptyConverter : IValueConverter
    {
        public IValueConverter Inner { get; set; }
        public bool Negate { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = IsTrue(value, parameter);
            if (Inner != null)
            {
                return Inner.Convert(v, targetType, null, culture);
            }
            return v;
        }

        private bool IsTrue(object value, object parameter)
        {
            var r = Equals(value, parameter);
            if (parameter == null)
            {
                if (value is string s && string.IsNullOrEmpty(s))
                    r = true;
                else if (value is IList list && list.Count == 0)
                    r = false;
            }
            if (Negate) r = !r;
            return r;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
