using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;

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
}