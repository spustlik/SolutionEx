using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SolutionExtensions.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public static class DteExtensions
    {
        private const string SOLUTION_ITEMS_FOLDER_NAME = "Solution Items";
        private const string MISC_FILES_NAME = "Miscellaneous Files";

        public static Project FindSolutionFolder(this Solution solution, bool addIfNotExists = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project proj in solution.Projects)
            {
                if (proj.UniqueName == EnvDTE.Constants.vsSolutionItemsProjectUniqueName)
                    return proj;
                if (proj.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    return proj;
            }
            if (addIfNotExists)
                return AddSolutionFolder(solution, SOLUTION_ITEMS_FOLDER_NAME);
            return null;
        }
        public static Project AddSolutionFolder(this Solution solution, string folderName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var s2 = solution as Solution2;
            var folder = s2.AddSolutionFolder(SOLUTION_ITEMS_FOLDER_NAME);
            //folder.Kind= EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder
            return folder;
        }

        public static Project FindSolutionProject(this Solution solution, Func<Project, bool> predicate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project p in solution.Projects)
            {
                if (predicate(p)) return p;
                var f = p.ProjectItems.FindSolutionProject(predicate);
                if (f != null) return f;
            }
            return null;
        }

        private static Project FindSolutionProject(this ProjectItems items, Func<Project, bool> predicate)
        {
            //warn: projects are in projectItems
            if (items == null)
                return null;
            ThreadHelper.ThrowIfNotOnUIThread();
            //if (items.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems ||
            //    items.Kind == EnvDTE.Constants.vsProjectItemsKindMisc)
            //{
            foreach (ProjectItem projectItem in items)
            {
                if (projectItem.Object is Project pip && predicate(pip))
                    return pip;
                var f = FindSolutionProject(projectItem.ProjectItems, predicate);
                if (f != null) return f;
            }
            return null;
        }

        public static ProjectItem FindProjectItem(this Project project, string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string name = Path.GetFileName(filePath);
            return FindProjectItem(project.ProjectItems, item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public static ProjectItem FindProjectItem(this ProjectItems items, Func<ProjectItem, bool> predicate)
        {
            if (items == null)
                return null;
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem item in items)
            {
                if (predicate(item))
                    return item;
                var f = item.ProjectItems.FindProjectItem(predicate);
                if (f != null) return f;
            }
            return null;
        }

        public static Project FindProjectMiscItems(this Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project item in solution.Projects)
            {
                if (item.Kind == EnvDTE.Constants.vsProjectKindMisc || item.UniqueName == EnvDTE.Constants.vsMiscFilesProjectUniqueName)
                    return item;
            }
            return null;
        }

        public static Project AddProjectMiscItems(this Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var sol2 = solution as Solution2;
            var template = solution.ProjectItemsTemplatePath(EnvDTE.Constants.vsProjectKindMisc);
            var proj = solution.AddFromTemplate(template, null, MISC_FILES_NAME);
            return proj;
        }
        /*
                if (File.Exists(filePath))
                    return project.ProjectItems.AddFromFile(filePath);
                else
                    return project.ProjectItems.AddFromFileCopy(filePath);
        */

        public static OutputWindowPane GetOutputWindowPane(this DTE dte, string name, bool createIfNotExists = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.Window win = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            OutputWindow ow = win.Object as OutputWindow;
            var pane = FindOutputPaneByName(ow, name);
            if (createIfNotExists && pane == null)
                pane = ow.OutputWindowPanes.Add(name);
            return pane;
        }

        public static OutputWindowPane FindOutputPaneByName(this OutputWindow ow, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (OutputWindowPane outputPane in ow.OutputWindowPanes)
            {
                if (outputPane.Name == name)
                    return outputPane;
            }
            return null;
        }

        public static void AddToOutputPane(this DTE dte, string msg, string name, bool clear = false, bool activate = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var outputPane = dte.GetOutputWindowPane(name, true);
            if (clear)
                outputPane.Clear();
            if (activate)
                outputPane.Activate();
            if (!msg.EndsWith("\n"))
                msg += "\n";
            outputPane.OutputString(msg);
        }

        public static Task ShowStatusBarErrorAsync(this AsyncPackage package, string message)
        {
            return package.ShowStatusBarAsync(message, isError: true);
        }
        public static async Task ShowStatusBarAsync(
            this AsyncPackage package,
            string message,
            bool isError = false,
            bool isImportant = false,
            int waitMs = 0)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            IVsStatusbar statusBar = await package.GetServiceAsync<SVsStatusbar, IVsStatusbar>();
            if (statusBar == null) return;
            var flash = isError || isImportant;
            if (flash && waitMs == 0)
                waitMs = 3000;
            //nor coloring nor highlight is working in VS
            uint? fcolor = null;
            uint? bcolor = null;
            if (isError)
            {
                //fcolor = (uint?)VSColorTheme.GetThemedColor(CommonDocumentColors.StatusBannerErrorTextColorKey).ToArgb();
                //bcolor = (uint?)VSColorTheme.GetThemedColor(CommonDocumentColors.StatusBannerErrorColorKey).ToArgb();                
            }
            if (isImportant)
            {
                //fcolor = 0xFF8080FF;
                //bcolor = 0xFF0000;
            }
            if (fcolor == null && bcolor == null)
                statusBar.SetText(message);
            else
                statusBar.SetColorText(message, fcolor.GetValueOrDefault(), bcolor.GetValueOrDefault());

            if (waitMs > 0)
            {
                statusBar.FreezeOutput(0); // Unfreeze if previously frozen
                statusBar.FreezeOutput(1); // Freeze to highlight the message
                await Task.Delay(waitMs);  // Keep it frozen for time
                statusBar.FreezeOutput(0); // Unfreeze again
            }
        }
        public static async Task SwitchToUiThreadAsync(this AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        }

        public static CommandID GetCommandID(this Command cmd)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return new CommandID(new Guid(cmd.Guid), cmd.ID);
        }
        public static async Task<OleMenuCommandService> GetMenuCommandServiceAsync(this AsyncPackage package)
        {
            var svc = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (svc == null) throw new Exception($"Cannot create IMenuCommandService");
            return svc;
        }

        public static void AddShortCutToCommand(this Command command, string shortCut, bool clear = true)
        {
            if (String.IsNullOrEmpty(shortCut))
                return;
            ThreadHelper.ThrowIfNotOnUIThread();
            //var command = GetCommandByName(dte2, "SolutionExtensions.MyCommand");
            // Assign shortcut if not already present
            var bindings = ((object[])command.Bindings).Cast<string>().ToList();
            if (clear)
                bindings.Clear();
            //what about "Text Editor::Ctrl+E, Ctrl+E" ?
            if (!shortCut.Contains("::"))
                shortCut = "Global::" + shortCut;
            if (!bindings.Contains(shortCut))
            {
                bindings.Add(shortCut);
                try
                {
                    command.Bindings = bindings.Cast<object>().ToArray();
                }
                catch (Exception ex)
                {
                    //silent error, because of bad format etc...
                }
            }
        }

        public static Command GetCommandByName(this DTE dte, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var commands = dte.Commands;
            var command = commands.Item(name, 0);
            return command;
        }

        public static T CreateToolWindow<T>(this Package package) where T : ToolWindowPane
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ToolWindowPane pane = package.FindToolWindow(typeof(T), 0, true);
            if (pane == null || pane.Frame == null)
                throw new NotSupportedException($"Cannot create tool window {typeof(T).Name}");

            IVsWindowFrame windowFrame = (IVsWindowFrame)pane.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            return pane as T;
        }

        public static IEnumerable<string> GetFiles(this ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            for (var i = 1; i <= item.FileCount; i++)
            {
                yield return item.FileNames[(short)i];
            }
        }

        public static IDisposable WaitCursor(this Package package)
        {
            return new WpfWaitCursorScope();
        }

        
        [Obsolete("Use using package.WaitCursor")]
        /// <summary>
        /// use using (new Microsoft.VisualStudio.Modeling.Shell.WaitCursor())
        /// </summary>
        public static void SetWaitCursor(this DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var vsShell = ServiceProvider.GlobalProvider.GetService<SVsUIShell, IVsUIShell>();
            vsShell.SetWaitCursor();
        }
        public static IEnumerable<IVsPackage> GetPackages(this IVsShell shell)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var hr = shell.GetPackageEnum(out var packagesEnum);
            Marshal.ThrowExceptionForHR(hr);
            packagesEnum.Reset();
            while (true)
            {
                var list = new IVsPackage[1];
                var r = packagesEnum.Next((uint)list.Length, list, out var fetched);
                Marshal.ThrowExceptionForHR(hr);
                if (fetched == 0)
                    break;
                yield return list[0];
            }
        }
        public static string StringToConst(string s)
        {
            var types = new[] { typeof(EnvDTE.Constants) };
            foreach (var type in types)
            {
                var list = type.GetFields();
                foreach (var field in list)
                {
                    var v = field.GetValue(null);
                    if (Equals(v, s))
                        return field.DeclaringType.Name + "." + field.Name;
                }
            }
            return s;
        }

    }
}
