using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
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

        public static ProjectItem FindProjectItem(this Project project, string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (string.Equals(item.Name, Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
                    return item;
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
            var proj = solution.AddFromTemplate(template, null,MISC_FILES_NAME);
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
            outputPane.OutputString($"{msg}\n");
        }
        public static async Task SwitchToUiThreadAsync(this AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        }

        public static async Task<OleMenuCommandService> GetMenuCommandServiceAsync(this AsyncPackage package)
        {
            var svc = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (svc == null) throw new Exception($"Cannot create IMenuCommandService");
            return svc;
        }

        public static void AddShortCutToCommand(this Command command, string shortCut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //var command = GetCommandByName(dte2, "SolutionExtensions.MyCommand");
            // Assign shortcut if not already present
            var bindings = ((object[])command.Bindings).Cast<string>().ToList();
            //what about "Text Editor::Ctrl+E, Ctrl+E" ?
            if (!shortCut.Contains("::"))
                shortCut = "Global::" + shortCut;
            if (!bindings.Contains(shortCut))
            {
                bindings.Add(shortCut);
                command.Bindings = bindings.ToArray();
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
    }
}
