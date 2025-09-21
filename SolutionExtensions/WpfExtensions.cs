using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SolutionExtensions
{
    public static class WpfExtensions
    {
        public static void ShowException(this Control _, Exception ex, string suffix = null, string title = null)
        {
            if (suffix != null)
                suffix = "\n" + suffix;
            if (String.IsNullOrEmpty(title))
                title = "Error";
            MessageBox.Show(ex.Message + suffix, title, MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
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
            if (p.GetType().IsAssignableFrom(ancestorType))
            {
                if (level <= 1)
                    return p;
                return FindAncestor(p, ancestorType, level - 1);
            }
            return FindAncestor(p, ancestorType, level);
        }
        public static FrameworkElement FindAncestorOrSelf(this FrameworkElement element, Func<FrameworkElement,bool> predicate)
        {
            if (predicate(element))
                return element;
            if (!(VisualTreeHelper.GetParent(element) is FrameworkElement p))
                return null;
            return FindAncestorOrSelf(p, predicate);
        }
        static void _Resources()
        {
            var _ = new[] {
                VsResourceKeys.ButtonStyleKey,
                VsResourceKeys.LabelEnvironment111PercentFontSizeStyleKey,
                VsResourceKeys.ThemedDialogTreeViewItemStyleKey,
                //...
                CommonControlsColors.ButtonBorderBrushKey,
                CommonControlsColors.ButtonBorderColorKey,
                CommonControlsColors.ComboBoxBackgroundBrushKey,
                //no treeview
                TreeViewColors.BackgroundTextColorKey,
                TreeViewColors.BackgroundColorKey,
            };
        }
    }

    /// <summary>
    /// Executes action after timeout. If previous call is waiting, it is disposed.
    /// </summary>
    public class ThrottleTimer
    {
        private Action action;
        private readonly DispatcherTimer timer;

        public ThrottleTimer(TimeSpan timeout)
        {
            timer = new DispatcherTimer(DispatcherPriority.Input);
            timer.Interval = timeout;
            timer.Tick += Timer_Tick;
        }
        public void Invoke(Action action)
        {
            timer.Stop();
            this.action = action;
            timer.Start();

        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            action();
        }
    }
}
