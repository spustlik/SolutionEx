using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SolutionExtensions
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
            if (value is string s && parameter == null && String.IsNullOrEmpty(s))
                r = true;
            if (Negate) r = !r;
            return r;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
