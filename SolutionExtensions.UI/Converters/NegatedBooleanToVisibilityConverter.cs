using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace SolutionExtensions.UI
{
    public class NegatedBooleanToVisibilityConverter : IValueConverter
    {
        BooleanToVisibilityConverter inner = new BooleanToVisibilityConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            value = NegateValue(value);
            return inner.Convert(value, targetType, parameter, culture);
        }

        private object NegateValue(object value)
        {
            if (Equals(value, true)) return false;
            if (Equals(value, false)) return true;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            value = NegateValue(value);
            return inner.ConvertBack(value, targetType, parameter, culture);
        }
    }
}
