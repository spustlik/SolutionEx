using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public static class DteExtensions
    {
        private const string SOLUTION_ITEMS_FOLDER = "Solution Items";

        public static Project FindSolutionItemsProject(this Solution solution)
        {
            //return dte.GetProject(SOLUTION_ITEMS_FOLDER, createIfNotExists);
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project proj in solution.Projects)
            {
                if (proj.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
                    return proj;
            }
            return null;
        }
        public static Project AddSolutionItemsProject(this Solution solution, string folderName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const string solutionFolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
            var p = solution.AddFromTemplate(solutionFolderKind, folderName, folderName);
            return p;
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


        public static Task<TC> InitCommandAsync<TC>(this AsyncPackage package) where TC : CommandBase, new()
        {
            return CommandBase.InitializeAsync<TC>(package);
        }
        public static async Task<OleMenuCommandService> GetMenuCommandServiceAsync(this AsyncPackage package)
        {
            var svc = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (svc == null) throw new Exception($"Cannot create IMenuCommandService");
            return svc;
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

    }
}
