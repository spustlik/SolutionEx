using EnvDTE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace SolutionExtensions
{
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
    public class FixEncoding
    {
        [Description("Files to process")]
        [DefaultValue("*.cs;*.ts;*.html")]
        public string Argument { get; set; }

        [Description("Fix encoding")]
        public void Run(DTE dte)
        {
            var sol = dte.Solution;
            pattern = new PatternChecker(Argument);
            foreach (Project project in sol.Projects)
            {
                projectsCount++;
                ProcessItems(project.ProjectItems, item => CheckItem(item));
            }
            var msg = $"Checked {filesCount} files\n" +
                $" - {utf8Count} in UTF-8\n" +
                $" - {nonAsciiCount} is non ASCII\n" +
                String.Join("\n", nonAsciiFiles.Take(3).Select(f => Path.GetFileName(f))) +
                $"...\n" +
                $"Do you want to convert them to UTF-8?";
            if (MessageBox.Show(msg, "Check items", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;            
            foreach(var f in nonAsciiFiles)
            {
                var text = File.ReadAllText(f);
                File.WriteAllText(f, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            }
        }

        int projectsCount = 0;
        int itemsCount = 0;
        int allFilesCount = 0;
        PatternChecker pattern;
        int filesCount = 0;
        int utf8Count = 0;
        int nonAsciiCount = 0;
        List<string> nonAsciiFiles = new List<string>();
        private void CheckItem(ProjectItem item)
        {
            itemsCount++;
            if (item.Kind != Constants.vsProjectItemKindPhysicalFile)
            {
                var kind = DteExtensions.StringToConst(item.Kind);
                var name = item.Name;
                return;
            }
            if (item.FileCount == 0)
                return;
            var fn = item.FileNames[1];
            if (!File.Exists(fn))
                return;
            allFilesCount++;
            if (!pattern.Match(fn))
                return;
            filesCount++;
            var bom = Encoding.UTF8.GetPreamble();
            using (var fs = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var header = new byte[bom.Length];
                fs.Read(header, 0, header.Length);
                if (Enumerable.SequenceEqual(header, bom))
                {
                    utf8Count++;
                    return; //already is UTF8
                }
            }
            var text = File.ReadAllText(fn);
            if (text.All(c => c <= 0x127))
                return;
            nonAsciiCount++;
            nonAsciiFiles.Add(fn);
        }

        private void ProcessItems(ProjectItems list, Action<ProjectItem> callback)
        {
            foreach (ProjectItem item in list)
            {
                callback(item);
                if (item.ProjectItems?.Count > 0)
                    ProcessItems(item.ProjectItems, callback);
            }
        }
    }
#pragma warning restore VSTHRD010 

    public class PatternChecker
    {
        Regex regex;

        public PatternChecker(string filesPattern)
        {
            regex = CreatePattern(filesPattern);
        }

        private Regex CreatePattern(string s)
        {
            //s = s.Replace("\\", "/").Replace("/", Path.PathSeparator + "");
            var parts = s.Split(new[] { ';', ',' });
            var types = new List<string>();
            foreach (var part in parts)
            {
                types.Add(CreateTypePattern(part));
            }
            return new Regex(string.Join("|", types), RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }


        private string CreateTypePattern(string part)
        {
            string GetChar(char ch)
            {
                switch (ch)
                {
                    case '.': return "\\.";
                    case '*': return "[^\\.]*";
                    case '\\': return "/";
                    default: return Regex.Escape(ch + "");
                }
            }
            var t = part.Trim();
            var sb = new StringBuilder();
            foreach (var ch in t)
                sb.Append(GetChar(ch));
            sb.Append("$");
            return sb.ToString();

        }
        public bool Match(string fn)
        {
            return regex.Match(fn).Success;
        }
    }
}
