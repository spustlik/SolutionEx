using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace SolutionExtensions._DesignData
{
    public class ColorWindowVM : SimpleDataObject
    {
        public ObservableCollection<ColorModel> Colors { get; } = new ObservableCollection<ColorModel>();
        public ObservableCollection<ColorModel> UIColors { get; } = new ObservableCollection<ColorModel>();
        public ObservableCollection<ColorGroupModel> ColorGroups { get; } = new ObservableCollection<ColorGroupModel>();
        public List<ColorReflectedModel> ReflectedColors { get; } = new List<ColorReflectedModel>();
    }

    public class ColorGroupModel
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string DeclaringTypeName { get; set; }
        public ColorTypesModel[] Items { get; set; }
    }
    public class ColorTypesModel
    {
        public string Name { get; set; }
        public string TypeNames { get; set; }
        public Brush FgColor { get; set; }
        public Brush BgColor { get; set; }
        public Brush FgBrush { get; set; }
        public Brush BgBrush { get; set; }
        public ColorModel[] Colors { get; set; }
    }
    public class ColorModel 
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
    public class ColorReflectedModel
    {
        public object Key { get; set; }
        public string Name { get; set; }
        public Type DeclaringType { get; set; }
        public Brush Brush { get; set; }
    }
}
