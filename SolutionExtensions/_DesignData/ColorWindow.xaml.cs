using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionExtensions.ToolWindows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            AddColors(vm);
            DataContext = vm;
        }

        private void AddColors(ColorWindowVM vm)
        {
            try
            {
                AddVsColors(vm);
                AddPlaformColors(vm);
            }
            catch (Exception ex)
            {
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
            //categories as fields public const string Printer = "{47724E70-AF55-48fb-A928-BB161C1D0C05}";
            var categories = typeof(Microsoft.VisualStudio.Shell.Interop.FontsAndColorsCategory).GetFields()
                .ToDictionary(fi => (fi.GetValue(null) +"").ToUpperInvariant(), fi => fi.Name);
            foreach (var pair in VsColors.GetCurrentThemedColorValues())
            {
                var color = new ColorModel()
                {
                    Key = pair.Key,
                    DeclaringType = typeof(VsColors)
                };
                if (categories.TryGetValue(color.Category.ToString("B").ToUpperInvariant(), out var cat))
                {
                    color.CategoryName = cat;
                }
                else color.CategoryName = color.Category.ToString("B");
                var res = TryFindResource(pair.Key);                
                if (res is Brush b)
                    color.Brush = b;
                else
                {
                    //var shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsUIShell5;
                    var shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell5;
                    var c = VsColors.GetThemedWPFColor(shell, pair.Key);
                    color.Brush = new SolidColorBrush(c);
                }
                vm.Colors.Add(color);
            }
        }
    }
    public class ColorWindowVM : SimpleDataObject
    {
        public ObservableCollection<ColorModel> Colors { get; } = new ObservableCollection<ColorModel>();
        public ObservableCollection<ColorModel> UIColors { get; } = new ObservableCollection<ColorModel>();
    }
    public class ColorModel : SimpleDataObject
    {
        public ThemeResourceKey Key { get; set; }
        public ThemeResourceKeyType KeyType => Key.KeyType;
        public string Name => Key.Name;
        public Guid Category => Key.Category;
        public string CategoryName { get; set; }
        public Type DeclaringType { get; set; }
        public string DeclaringTypeName => DeclaringType?.FullName;
        public Brush Brush { get; set; }
    }
}
