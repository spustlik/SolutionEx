using EnvDTE;
using EnvDTE80;
using SolutionExtensions.Reflector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Packaging;
using System.Linq;
using System.Windows;

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
namespace SolutionExtensions.Extensions
{
    [Description("Nest file")]
    public class NestFileExtension
    {
        private DTE2 dte;

        public void Run(DTE dte, IServiceProvider package)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            this.dte = dte as DTE2;
            var selected = GetSelectedSolutionExplorerItems().ToArray();
            if (selected.Length == 0)
                return;
            if (selected.Length > 1)
            {
                MessageBox.Show($"More selected items are no supported for now", "Error", MessageBoxButton.OK);
                return;
            }
            var item = selected.First();
            var r = GetNestItemStatus(item, out var nestTo);
            var s = GetStatusMessage(r, item, nestTo);
            if (r == NestStatus.AlreadyNested)
            {
                if (MessageBox.Show(s + "\nDo you want to unnest them?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
                if (!UnNest(item))
                {
                    MessageBox.Show($"Error unnesting {item.Name}", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
                return;
            }
            if (r != NestStatus.IsPossible)
            {
                MessageBox.Show(s, "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            if (MessageBox.Show(s + "\nAre you sure?", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            Log($"nesting {item.Name} to {nestTo.Name}");
            NestTo(nestTo, item);
        }

        string GetStatusMessage(NestStatus s, UIHierarchyItem item, UIHierarchyItem nestTo)
        {
            switch (s)
            {
                case NestStatus.UnsupportedProjectItem:
                    return $"Unsupported project item type. Need to use classic project, not modern types.";
                case NestStatus.NoFileName:
                    return $"Select some file.";
                case NestStatus.Folder:
                    return $"Cannot apply to folder.";
                case NestStatus.AlreadyNested:
                    return $"Item has already {item.UIHierarchyItems.Count} nested item(s).";
                case NestStatus.NoSuchFile:
                    return $"Cannot find file for nesting like '{item.Name}'.";
                case NestStatus.IsPossible:
                    return $"{item.Name} will be nested to {nestTo.Name}.";
                default:
                    return null;
            }
        }

        enum NestStatus
        {
            NoFileName,
            Folder,
            AlreadyNested,
            NoSuchFile,
            IsPossible,
            UnsupportedProjectItem
        }
        private NestStatus GetNestItemStatus(UIHierarchyItem item, out UIHierarchyItem nestTo)
        {
            nestTo = null;
            if (item.Object == null || !ReflectionCOM.IsCOMObjectType(item.Object.GetType()))
                return NestStatus.UnsupportedProjectItem;
            var fpath = GetFileName(item);
            Log($"item: {item.Name} {fpath}");
            if (String.IsNullOrEmpty(fpath))
                return NestStatus.NoFileName;
            if (fpath.EndsWith("\\"))
                return NestStatus.Folder;
            if (item.UIHierarchyItems.Count > 0)
                return NestStatus.AlreadyNested;
            nestTo = GetNestTo(item, fpath);
            if (nestTo == null)
                return NestStatus.NoSuchFile;
            return NestStatus.IsPossible;
        }

        private static string GetFileName(UIHierarchyItem item)
        {
            return (item as dynamic).Object.FileNames(0) as string;
        }

        private static string GetFileName2(UIHierarchyItem item)
        {
            var o = item.Object; //OAProjectItem
            if (o.GetType().Name != "OAProjectItem")
                return null;
            //jenomze tohle jsou items, ktere neumi nesting
            if (!ReflectionHelper.TryGetProperty<Int16>(o, "FileCount", out var fileCount))
                return null;
            if (fileCount <= 0)
                return null;
            if (!ReflectionHelper.TryCallMethod<string>(o, out var fileName, "get_FileNames", (Int16)0))
                return null;
            return fileName;
        }

        private static bool NestTo(UIHierarchyItem nestTarget, UIHierarchyItem item)
        {
            //COM object
            var fn = GetFileName(item);
            (nestTarget.Object as dynamic).ProjectItems.AddFromFile(fn);
            return true;
        }

        private static bool NestTo2(UIHierarchyItem nestTarget, UIHierarchyItem item)
        {
            // OAProjectItem - not nesting
            if (!ReflectionHelper.TryCallMethod<string>(item.Object, out var fileName, "get_FileNames", (Int16)0))
                return false;
            if (!ReflectionHelper.TryGetProperty<object>(nestTarget.Object, "ProjectItems", out var projectItems))
                return false;
            if (!ReflectionHelper.TryCallMethod<object>(projectItems, out _, "AddFromFile", fileName))
                return false;
            //??? item.Remove(), item.Collection.Remove()? item.Object.Remove()?            
            return true;
        }

        private void Log(string msg)
        {
            dte.AddToOutputPane(msg, GetType().Name);
        }

        private bool UnNest(UIHierarchyItem item)
        {
            var parent = (item as dynamic).Collection.Parent.Object; //project or folder, COM
            Log($"item: {item.Name} {GetFileName(item)} parent: {parent.Name}"); 
            var items = item.UIHierarchyItems.Cast<UIHierarchyItem>().ToArray();
            foreach (var sub in items)
            {
                var subfn = GetFileName(sub);
                Log($"  subitem: {sub.Name} {subfn}"); ///sub.Object is COM
                //(sub.Object as dynamic).ProjectItems.Remove(); // neexistuje sub.Collection.Remove(); 
                (sub.Object as dynamic).Remove(); //tohle funguje, ale zustane hierarchy
                // item.Collection.Parent.Object.ProjectItems.AddFromFile(sub.Object.FileNames(0));
                parent.ProjectItems.AddFromFile(subfn); //pokud se neprida, odstrani se z projektu, ale porad je jako subitem
            }
            return true;
        }
        private UIHierarchyItem GetNestTo(UIHierarchyItem item, string fpath)
        {
            var fn = System.IO.Path.GetFileNameWithoutExtension(fpath);
            foreach (UIHierarchyItem i in (item.Collection.Parent as UIHierarchyItem).UIHierarchyItems)
            {
                //outputPane.OutputString("  parent item: " + i.Name + "\n");
                var s = System.IO.Path.GetFileNameWithoutExtension(i.Name);
                if (s == fn && System.IO.Path.GetFileName(fpath) != System.IO.Path.GetFileName(i.Name))
                    return i;
            }
            return null;
        }
        protected IEnumerable<UIHierarchyItem> GetSelectedSolutionExplorerItems()
        {
            UIHierarchy solutionExplorer = dte.ToolWindows.SolutionExplorer;
            object[] items = solutionExplorer.SelectedItems as object[];
            return items.Cast<UIHierarchyItem>();
        }

    }
}
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

