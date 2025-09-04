using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SolutionExtensions.Extensions
{

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
    public class DumpExtension
    {
        [Description("Dump DTE")]
        public void Run(EnvDTE.DTE dte, AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package = package;
            this.dte = dte;
            this.cmdSvc = (package as IServiceProvider).GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dumpRoot = new XElement("Dump");
            DumpDte(dumpRoot);
            var fn = Path.GetTempFileName() + ".xml";
            dumpRoot.Save(fn);
            //ok:var template1 = dte.Solution.ProjectItemsTemplatePath("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
            //err:var template = dte.Solution.ProjectItemsTemplatePath("{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}");// EnvDTE.Constants.vsProjectKindMisc);
            OpenFile(fn);
        }

        private void OpenFile(string fn)
        {
            try
            {
                var proj = dte.Solution.FindProjectMiscItems() ?? dte.Solution.AddProjectMiscItems();
                var pi = proj.FindProjectItem(fn);
                if (pi != null)
                    pi = proj.ProjectItems.AddFromFile(fn);
                dte.Documents.Open(fn);
                /*
      <Project Name="Miscellaneous Files" Kind="{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}" UniqueName="&lt;MiscFiles&gt;">
        <ProjectItem Name="TextFile1.txt" 
                Kind="{66A2671F-8FB5-11D2-AA7E-00C04F688DDE}" 
                Files="C:\Users\jstuc\AppData\Local\Temp\bso35zeh..txt" />
      </Project>
                */

                //var doc = dte.Documents.Add(null);// arg invalid
                //var doc = dte.Documents.Add(EnvDTE.Constants.vsDocumentKindText);//arg invalid
                //var doc = dte.Documents.Add(EnvDTE.Constants.vsDocumentKindText.Trim('{', '}'));//arg invalid
                var doc = dte.Documents.Add(fn);
                doc.Activate();
                //(doc.Selection as EnvDTE.TextSelection).Insert(dumpRoot.ToString());
            }
            catch (Exception ex)
            {
                dte.AddToOutputPane($"Error:" + ex, typeof(SolutionExtensionsPackage).Namespace);
            }

            //dte.Solution.AddFromFile(fn);//needs path to template
        }

        AsyncPackage package;
        DTE dte;
        OleMenuCommandService cmdSvc;
        private void DumpDte(XElement parent)
        {
            DumpCommands(parent);
            DumpMenuCommands(parent);
            DumpSolution(parent);
            DumpDocuments(parent);
        }

        private void DumpDocuments(XElement parent)
        {
            var e = new XElement("Documents", Attr(new { count = dte.Documents.Count }));
            parent.Add(e);
            foreach (Document doc in dte.Documents)
            {
                DumpDocument(e, doc);
            }
        }

        private void DumpDocument(XElement parent, Document doc)
        {
            var e = new XElement("Document", Attr(new { doc.Name, doc.Kind, doc.Language, doc.Type, doc.Path, doc.FullName }));
            parent.Add(e);
        }

        private void DumpSolution(XElement parent)
        {
            if (dte.Solution == null)
                return;
            var sol = dte.Solution;
            var e = new XElement("Solution",
                Attr(new
                {
                    sol.FullName,
                    sol.IsOpen,
                    //templatePath = (sol as dynamic).TemplatePath
                }));
            parent.Add(e);
            DumpExtenders(e, sol);
            DumpGlobals(e, sol.Globals);
            DumpProperties(e, sol.Properties);
            DumpProjects(e, sol);
            DumpSolutionBuild(e, sol);
        }

        private void DumpSolutionBuild(XElement parent, Solution sol)
        {
            var b = sol.SolutionBuild;
            if (b == null)
                return;
            var e = new XElement("SolutionBuild", Attr(new { b.BuildState }));
            if (b.BuildState != vsBuildState.vsBuildStateNotStarted)
            {
                e.Add(Attr(new { b.LastBuildInfo }));
            }

            parent.Add(e);
        }

        private object[] Attr(object o)
        {
            var result = new List<XAttribute>();
            var props = o.GetType().GetProperties();
            foreach (var prop in props)
            {
                var v = prop.GetValue(o);
                if (v != null && !String.Empty.Equals(v))
                    result.Add(new XAttribute(prop.Name, v));
            }
            return result.ToArray();
        }

        private void DumpExtenders(XElement e, /*Solution*/
                dynamic parent)
        {
            if (!String.IsNullOrEmpty(parent.ExtenderCATID))
                e.Add(new XAttribute("extenderCATID", parent.ExtenderCATID));
            if (parent.ExtenderNames != null)
            {
                var names = parent.ExtenderNames as string[];
                foreach (var n in names)
                {
                    var ee = new XElement("Extender", new XAttribute("name", n));
                    e.Add(ee);
                    var ex = parent.Extender[n];
                    ee.Add(new XAttribute("value", ex));
                }
            }
        }

        private void DumpProjects(XElement parent, Solution sol)
        {
            var e = new XElement("Projects");
            parent.Add(e);
            //dte.Documents.Add()
            foreach (Project proj in sol)
            {
                DumpProject(e, proj);
            }

        }

        private void DumpProject(XElement parent, Project proj)
        {
            var e = new XElement("Project",
                Attr(new
                {
                    proj.Name,
                    proj.FileName,
                    proj.FullName,
                    proj.Kind,
                    proj.UniqueName,
                    proj.CodeModel,
                    objectType = proj.Object
                }));
            parent.Add(e);
            //DumpExtenders(e, proj);
            DumpGlobals(e, proj.Globals); //possible same as sol?
            DumpProperties(e, proj.Properties);
            foreach (ProjectItem item in proj.ProjectItems)
            {
                DumpProjectItem(e, item);
            }
        }

        private void DumpProjectItem(XElement parent, ProjectItem item)
        {
            var files = String.Join(";", item.GetFiles());
            var e = new XElement("ProjectItem", Attr(new
            {
                item.Name,
                item.Kind,
                item.FileCodeModel,
                Files = files,
                //item.IsDirty,
                //item.Saved,
                objectType = item.Object
            }));
            parent.Add(e);
            //DumpExtenders(e, item);
            DumpProperties(e, item.Properties);
            if (item.SubProject != null)
            {
                DumpProject(e, item.SubProject);
            }
        }

        private void DumpGlobals(XElement e, Globals globals)
        {
            var variables = (globals.VariableNames as object[]).Cast<string>().ToArray();
            if (variables.Length == 0)
                return;
            var ge = new XElement("Globals");
            e.Add(ge);
            foreach (var variable in variables)
            {
                var ve = new XElement("Variable", new XAttribute("name", variable));
                ge.Add(ve);
                try
                {
                    var v = globals[variable];
                    ve.Add(new XAttribute("value", v));
                }
                catch (Exception ex)
                {
                    ve.Add(new XAttribute("error", ex.Message));
                }
            }
        }

        private void DumpMenuCommands(XElement parent)
        {
            var e = new XElement("Menu", new XAttribute("count", cmdSvc.Verbs.Count));
            parent.Add(e);
            foreach (MenuCommand verb in cmdSvc.Verbs)
            {
                DumpMenuCommand(e, verb);
            }
            //problem of menuteims is that service is mnu from package, but cmd is from dte
            
            foreach (Command c in dte.Commands)
            {
                var id = new CommandID(new Guid(c.Guid), c.ID);
                var mnu = cmdSvc.FindCommand(id);
                if (mnu != null)
                {
                    DumpMenuCommand(e, mnu);
                }
            }
        }

        private void DumpMenuCommand(XElement parent, MenuCommand verb)
        {
            var e = new XElement("Menu",
                       Attr(new
                       {
                           verb.CommandID,
                           verb.Visible
                       }));
            if (verb is DesignerVerb dv)
            {
                e.Add(new XAttribute("text", dv.Text));
                e.Add(new XAttribute("description", dv.Description));
            }
            DumpProperties(e, verb.Properties);
            parent.Add(e);
        }

        private void DumpProperties(XElement parent, Properties properties)
        {
            if (properties == null || properties.Count == 0)
                return;
            foreach (Property property in properties)
            {
                var pe = new XElement("Property", new XAttribute("name", property.Name));
                parent.Add(pe);
                try
                {
                    var value = property.Value;
                    if (value != null)
                        pe.Add(new XAttribute("value", value));
                }
                catch (Exception ex)
                {
                    pe.Add(new XAttribute("error", ex.Message));
                }
            }

        }
        private void DumpProperties(XElement parent, IDictionary properties)
        {
            if (properties.Count == 0)
                return;
            foreach (var property in properties.Keys)
            {
                var pe = new XElement("Property", new XAttribute("name", property));
                parent.Add(pe);
                try
                {
                    var value = properties[property];
                    if (value != null)
                        pe.Add(new XAttribute("value", value));
                }
                catch (Exception ex)
                {
                    pe.Add(new XAttribute("error", ex.Message));
                }
            }
        }

        private void DumpCommands(XElement parent)
        {
            var e = new XElement("Commands", Attr(new { dte.Commands.Count }));
            parent.Add(e);
            foreach (var g in dte.Commands.Cast<Command>().GroupBy(c => c.Guid))
            {
                var ge = new XElement("GroupSet", new XAttribute("guid", g.Key));
                e.Add(ge);
                foreach (var cmd in g)
                {
                    var ce = new XElement("Command", new XAttribute("id", "0x" + cmd.ID.ToString("X")));
                    ge.Add(ce);
                    if (!string.IsNullOrEmpty(cmd.Name))
                        ce.Add(new XAttribute("name", cmd.Name));
                    var b = cmd.Bindings as object[]; //array of strings in format like "VC Dialog Editor::F9"
                    if (b.Length > 0)
                        ce.Add(new XAttribute("bindings", string.Join("|", b)));
                }
            }
        }
    }
}
