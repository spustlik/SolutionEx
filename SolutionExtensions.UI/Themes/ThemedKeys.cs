using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SolutionExtensions.UI.Themes
{
    //this is allowing to override keys for some colors
    //keys are named how it is used
    //map to VS colors is stringified, vs names are strange, https://learn.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/shared-colors-for-visual-studio?view=vs-2022
    //ui="Microsoft.VisualStudio.PlatformUI"
    //WARNING: sometime StaticResource is cached by StaticResourceHolder
    //and DynamicResource must be used instead
    public class ThemeKeys
    {
        private static readonly Dictionary<string, string> _originals = new Dictionary<string, string>();
        static ThemeKeys()
        {
            //string values moved to _originals and set to unique values using prop name
            //but x:Static is probably compiled to baml?
            foreach (var (property, current) in GetKeys())
            {
                _originals.Add(property.Name, current as string);
                property.SetValue(null, property.Name);
            }
        }
        public static void ReplaceWithOriginals(
            Func<(string name, string ns, string typePath), object> getNewValue)
        {
            foreach (var (property, _) in GetKeys())
            {
                if (!_originals.TryGetValue(property.Name, out var original))
                    continue;
                var parts = original.Split(new[] { ':' }, 2);
                var ns = parts.Length == 2 ? parts[0] : null;
                var typePath = parts.Last();
                var replace = getNewValue((name: property.Name, ns, typePath));
                if (replace == null) continue;
                property.SetValue(null, replace);
            }
        }
        public static string DumpCurrentValues(FrameworkElement resources)
        {
            var sb = new StringBuilder();
            foreach (var (property, value) in GetKeys())
            {
                var r = resources.TryFindResource(value);
                if (r is SolidColorBrush brush)
                {
                    sb.AppendLine($"<SolidColorBrush x:Key=\"{{x:Static themes:ThemeKeys.{property.Name}}}\" Color=\"{brush.Color}\"/>");
                }
                else if (r is Color color)
                {
                    sb.AppendLine($"<Color x:Key=\"{{x:Static themes:ThemeKeys.{property.Name}}}\">{color}</Color>");
                }
                else
                {
                    sb.AppendLine($"<!-- {property.Name}={value} : {(r ?? "(null)")} -->");
                }
            }
            return sb.ToString();
        }
        private static IEnumerable<(PropertyInfo property, object current)> GetKeys()
        {
            var props = typeof(ThemeKeys).GetProperties();
            foreach (var property in props)
            {
                var current = property.GetValue(null);
                yield return (property, current);
            }
        }

        public static object TreeViewItem_TreeArrow_Static_Stroke { get; set; } = "ui:TreeViewColors.GlyphBrushKey";
        public static object TreeViewItem_TreeArrow_Static_Fill { get; set; } = "ui:TreeViewColors.BackgroundBrushKey";
        public static object TreeViewItem_TreeArrow_MouseOver_Stroke { get; set; } = "ui:TreeViewColors.GlyphMouseOverBrushKey";
        public static object TreeViewItem_TreeArrow_MouseOver_Fill { get; set; } = "ui:TreeViewColors.BackgroundBrushKey";
        public static object TreeViewItem_TreeArrow_Static_Checked_Stroke { get; set; } = "ui:TreeViewColors.DragOverItemGlyphBrushKey";
        public static object TreeViewItem_TreeArrow_Static_Checked_Fill { get; set; } = "ui:TreeViewColors.DragOverItemBrushKey";
        public static object TreeViewItem_TreeArrow_MouseOver_Checked_Stroke { get; set; } = "ui:TreeViewColors.SelectedItemActiveGlyphBrushKey";
        public static object TreeViewItem_TreeArrow_MouseOver_Checked_Fill { get; set; } = "ui:TreeViewColors.SelectedItemActiveBrushKey";
        public static object TreeViewItem_Stroke { get; set; } = "ui:TreeViewColors.BackgroundTextBrushKey";
        /**/public static object TreeViewItem_Fill { get; set; } = "ui:TreeViewColors.BackgroundBrushKey";
        public static object TreeViewItem_Selected_Stroke { get; set; } = "ui:TreeViewColors.HighlightedSpanTextBrushKey";
        public static object TreeViewItem_Selected_Fill { get; set; } = "ui:TreeViewColors.HighlightedSpanBrushKey";
        public static object TreeViewItem_Selected_Inactive_Stroke { get; set; } = "ui:TreeViewColors.SelectedItemInactiveTextBrushKey";
        /**/public static object TreeViewItem_Selected_Inactive_Fill { get; set; } = "ui:TreeViewColors.SelectedItemInactiveBrushKey";
        public static object TreeViewItem_Disabled_Stroke { get; set; } = "ui:ThemedDialogColors.ListItemDisabledTextBrushKey";
    }
}
