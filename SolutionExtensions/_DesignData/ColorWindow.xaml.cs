using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SolutionExtensions._DesignData
{
    /// <summary>
    /// Interaction logic for ColorWindow.xaml
    /// </summary>
    public partial class ColorWindow : Window
    {
        public ColorWindow()
        {
            InitializeComponent();
            var vm = new ColorWindowVM();
            GetColors(vm);
            DataContext = vm;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetColors(DataContext as ColorWindowVM);
        }
        private void GetColors(ColorWindowVM vm)
        {
            try
            {
                vm.Colors.Clear();
                vm.UIColors.Clear();
                vm.ColorGroups.Clear();
                vm.ReflectedColors.Clear();
                AddVsColors(vm);
                AddPlaformColors(vm);
                AddColorGroups(vm);
                AddVsColorsReflected(vm);
            }
            catch (Exception)
            {
            }
        }

        private void AddColorGroups(ColorWindowVM vm)
        {
            var all = vm.Colors.Concat(vm.UIColors).ToList();
            foreach (var gc in all.GroupBy(c => c.CategoryName ?? c.Category.ToString("B")))
            {
                var gm = new ColorGroupModel()
                {
                    Name = gc.Key,
                    DeclaringTypeName = String.Join(",", gc.Select(c => c.DeclaringTypeName).Distinct()),
                    Id = gc.First().Category.ToString(),
                    //Items = g.OrderBy(c => c.Name).ThenBy(c => c.KeyType).ToArray()
                };
                gm.Items = gc
                    .GroupBy(c => c.Name)
                    .Select(gn => new ColorTypesModel()
                    {
                        Name = gn.Key,
                        TypeNames = String.Join(",", gn.Select(c => c.KeyType)),
                        FgColor = gn.FirstOrDefault(x => x.KeyType == ThemeResourceKeyType.ForegroundColor)?.Brush,
                        BgColor = gn.FirstOrDefault(x => x.KeyType == ThemeResourceKeyType.BackgroundColor)?.Brush,
                        FgBrush = gn.FirstOrDefault(x => x.KeyType == ThemeResourceKeyType.ForegroundBrush)?.Brush,
                        BgBrush = gn.FirstOrDefault(x => x.KeyType == ThemeResourceKeyType.BackgroundBrush)?.Brush,
                        Colors = gn.ToArray()
                    })
                    .ToArray();
                vm.ColorGroups.Add(gm);
            }
        }

        private void AddPlaformColors(ColorWindowVM vm)
        {
            //Microsoft.VisualStudio.PlatformUI.VSColorTheme.
            var ctype = typeof(Microsoft.VisualStudio.PlatformUI.DecorativeColors);
            //public static ThemeResourceKey LightBlueQuinaryColorKey =>
            foreach (var colorsType in ctype.Assembly.GetTypes().Where(t => t.Namespace == ctype.Namespace))
            {
                foreach (var pi in colorsType.GetProperties())
                {
                    if (pi.PropertyType != typeof(ThemeResourceKey))
                        continue;
                    var c = new ColorModel()
                    {
                        Key = pi.GetValue(null) as ThemeResourceKey,
                        DeclaringType = colorsType,
                        CategoryName = colorsType.Name,
                    };
                    var res = TryFindResource(c.Key);
                    if (res is Brush b)
                        c.Brush = b;
                    else if (res is Color color)
                        c.Brush = new SolidColorBrush(color);
                    vm.UIColors.Add(c);
                }
            }
        }

        private void AddVsColors(ColorWindowVM vm)
        {
            //Microsoft.VisualStudio.Shell.VsColors;
            //not working: guids are different
            //categories as fields public const string Printer = "{47724E70-AF55-48fb-A928-BB161C1D0C05}";
            //var categories = typeof(Microsoft.VisualStudio.Shell.Interop.FontsAndColorsCategory).GetFields()
            //    .ToDictionary(fi => (fi.GetValue(null) + "").ToUpperInvariant(), fi => fi.Name);

            var shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell5;
            foreach (var pair in VsColors.GetCurrentThemedColorValues())
            {
                var color = new ColorModel()
                {
                    Key = pair.Key,
                    DeclaringType = typeof(VsColors)
                };
                //color.CategoryName = color.Category.ToString("B");
                var res = TryFindResource(pair.Key) as Brush;
                color.Brush = res;
                if (res == null)
                {
                    var c = VsColors.GetThemedWPFColor(shell, pair.Key);
                    color.Brush = new SolidColorBrush(c);
                }
                vm.Colors.Add(color);
            }
        }
        public void AddVsColorsReflected(ColorWindowVM vm)
        {
            var colorProps = typeof(VsColors)
                .GetProperties(System.Reflection.BindingFlags.Static)
                .Where(pi => pi.PropertyType == typeof(object))
                .ToArray();
            foreach (var cp in colorProps)
            {
                var color = new ColorReflectedModel()
                {
                    Key = cp.GetValue(null),
                    Name = cp.Name,
                    DeclaringType = typeof(VsColors),
                };
                color.Brush = TryFindResource(color.Key) as Brush;
                vm.ReflectedColors.Add(color);
            }
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            this.DoZoomerClick();
        }

    }
}
