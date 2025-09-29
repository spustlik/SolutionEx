using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace SolutionExtensions.UI.Themes
{
    //this is allowing to override keys for some colors
    //keys are named by usage, not source
    //map to VS colors is stringified, vs names are strange, https://learn.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/shared-colors-for-visual-studio?view=vs-2022
    //ui="Microsoft.VisualStudio.PlatformUI"
    //shell="Microsoft.VisualStudio.Shell"
    public class ThemeKeys
    {
        private static readonly Dictionary<string, string> _originals = new Dictionary<string, string>();
        static ThemeKeys()
        {
            //string values moved to _originals and set to unique values using prop name
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
            var d = new ThemeDumper(resources);
            foreach (var (property, value) in GetKeys())            
                d.Dump(property.Name, value);            
            return d.Result;
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
        public static object TreeViewItem_Fill { get; set; } = "ui:TreeViewColors.BackgroundBrushKey";
        public static object TreeViewItem_Selected_Stroke { get; set; } = "ui:TreeViewColors.HighlightedSpanTextBrushKey";
        public static object TreeViewItem_Selected_Fill { get; set; } = "ui:TreeViewColors.HighlightedSpanBrushKey";
        public static object TreeViewItem_Selected_Inactive_Stroke { get; set; } = "ui:TreeViewColors.SelectedItemInactiveTextBrushKey";
        public static object TreeViewItem_Selected_Inactive_Fill { get; set; } = "ui:TreeViewColors.SelectedItemInactiveBrushKey";
        public static object TreeViewItem_Disabled_Stroke { get; set; } = "ui:ThemedDialogColors.ListItemDisabledTextBrushKey";
        //**new:
        public static object Mover_Fill { get; set; } = "ui:CommonControlsColors.ButtonBrushKey";
        
        //this color is same as bg
        //public static object Validation_Stroke { get; set; } = "ui:ThemedDialogColors.ValidationErrorTextBrushKey";
        public static object Validation_Fill { get; set; } = "ui:ThemedDialogColors.ValidationErrorBrushKey";
        public static object Button_Style { get; set; } = "shell:VsResourceKeys.ButtonStyleKey";
        public static object Label_Style { get; set; } = "shell:VsResourceKeys.ThemedDialogLabelStyleKey";
        public static object TextBox_Style { get; set; } = "shell:VsResourceKeys.TextBoxStyleKey";
    }
}
