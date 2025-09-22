using SolutionExtensions.UI.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public class VsThemeKeys
    {
        public static void Init()
        {
            //xmlns:shell="clr-namespace:Microsoft.VisualStudio.Shell;assembly=Microsoft.VisualStudio.Shell.15.0"             
            //xmlns:ui="clr-namespace:Microsoft.VisualStudio.PlatformUI;assembly=Microsoft.VisualStudio.Shell.15.0"
            var t = typeof(Microsoft.VisualStudio.PlatformUI.VSColorTheme);
            //Microsoft.VisualStudio.PlatformUI.TreeViewColors.
            var uiTypes = t.Assembly.GetTypes()
                .Where(x => x.Namespace == t.Namespace)
                .GroupBy(x => x.Name)
                .ToDictionary(g => g.Key, g => g.First());
            var namespaces = new Dictionary<string, Dictionary<string, Type>>()
            {
                { "ui", uiTypes }
            };
            ThemeKeys.ReplaceWithOriginals(key =>
            {
                if (!namespaces.TryGetValue(key.ns, out var types))
                    return null;
                var parts = key.typePath.Split('.');
                if (parts.Length != 2) return null;
                var typeName = parts[0];
                var propName = parts[1];
                if (!types.TryGetValue(typeName, out var type)) return null;
                var prop = type.GetProperty(propName);
                if (prop == null) return null;
                return prop.GetValue(null);
            });
        }
    }
}
