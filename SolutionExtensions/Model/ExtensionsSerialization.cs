using System;
using System.IO;

namespace SolutionExtensions
{
    public static class ExtensionsSerialization
    {
        public static void LoadFromFile(ExtensionsModel target, string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    target.Extensions.Clear();
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine().Trim();
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                            continue;
                        var item = LoadItem(line);
                        if (item != null)
                            target.Extensions.Add(item);
                    }
                }
            }
        }
        private static ExtensionItem LoadItem(string line)
        {
            var parts = line.Split('|');
            if (parts.Length < 4)
                return null;
            var item = new ExtensionItem()
            {
                Title = parts[0],
                ShortCutKey = parts[1],
                ClassName = parts[2],
                DllPath = parts[3]
            };
            if (parts.Length > 4)
                item.Argument = parts[4];
            //if (parts.Length >= 5)
            //    item.AutoRun = parts[5];
            return item;
        }
        public static void SaveToFile(ExtensionsModel source, string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine("# Solution extensions configuration file");
                    sw.WriteLine("# format: [Title]|[ShortCutKey]|[ClassName]|DllPath");
                    foreach (var ext in source.Extensions)
                    {
                        if (ext.Title == null && ext.DllPath == null && ext.ClassName == null)
                            continue;
                        SaveItem(sw, ext);
                    }
                }
            }
        }

        private static void SaveItem(StreamWriter sw, ExtensionItem item)
        {
            sw.WriteLine($"{item.Title}|{item.ShortCutKey}|{item.ClassName}|{item.DllPath}|{item.Argument}");
        }
    }
}