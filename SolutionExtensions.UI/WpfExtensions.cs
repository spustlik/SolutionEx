using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SolutionExtensions.UI
{
    public static class WpfExtensions
    {
        public static void ShowException(this Control _, Exception ex, string suffix = null, string title = null)
        {
            if (suffix != null)
                suffix = "\n" + suffix;
            if (string.IsNullOrEmpty(title))
                title = "Error";
            MessageBox.Show(ex.Message + suffix, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        /// <summary>
        /// returns name of resource key, if property is set by DynamicResource
        /// </summary>
        public static string GetPropertyResourceKey(this DependencyObject obj, DependencyProperty prop)
        {
            //SetResourceReference(Control.StyleProperty, "FormLabelStyle")
            var local = obj.ReadLocalValue(prop);//internal class ResourceReferenceExpression
            if (local == null)
                return null;
            if (local.GetType().Name != "ResourceReferenceExpression")
                return null;
            var pi = local.GetType().GetProperty("ResourceKey",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.FlattenHierarchy |
                System.Reflection.BindingFlags.NonPublic);
            if (pi == null)
                return null;
            var resKey = pi.GetValue(local) as string;
            return resKey;
        }

        public static void OpenContextMenu(this FrameworkElement element)
        {
            var ctx = element.ContextMenu;
            if (ctx == null)
                return;
            ctx.DataContext = element.DataContext;
            ctx.PlacementTarget = element;
            ctx.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            ctx.IsOpen = true;
        }

        public static void DoZoomerClick(this FrameworkElement element)
        {
            if (element is Window wnd)
            {
                element = wnd.Content as FrameworkElement;
            }
            var scale = element.LayoutTransform as ScaleTransform;
            if (scale == null)
                scale = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };
            var s = scale.ScaleX;
            if (s == 1) s = 1.5; else if (s == 1.5) s = 2.0; else if (s == 2.0) s = 1.0;
            element.LayoutTransform = new ScaleTransform(s, s);
        }

        public static T FindAncestor<T>(this FrameworkElement element) where T : FrameworkElement
        {
            return element.FindAncestor(typeof(T)) as T;
        }
        public static FrameworkElement FindAncestor(this FrameworkElement element, Type ancestorType, int level = 1)
        {
            if (!(VisualTreeHelper.GetParent(element) is FrameworkElement p))
                return null;
            if (ancestorType.IsAssignableFrom(p.GetType()))
            {
                if (level <= 1)
                    return p;
                return p.FindAncestor(ancestorType, level - 1);
            }
            return p.FindAncestor(ancestorType, level);
        }
        public static FrameworkElement FindAncestorOrSelf(this FrameworkElement element, Func<FrameworkElement,bool> predicate)
        {
            if (predicate(element))
                return element;
            if (!(VisualTreeHelper.GetParent(element) is FrameworkElement p))
                return null;
            return p.FindAncestorOrSelf(predicate);
        }
        public static bool IsModifierKey(this Key key)
        {
            return new[] {
                Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift,
                Key.CapsLock, Key.NumLock, Key.LWin, Key.RWin,
                }.Contains(key);
        }

    }
}

