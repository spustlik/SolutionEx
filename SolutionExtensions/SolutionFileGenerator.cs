using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    [Guid(SolutionFileGenerator.ProgId)]
    [ComVisible(true)]
    public class SolutionFileGenerator : IVsSingleFileGenerator, IObjectWithSite
    {
        public const string ProgId = "9ecce53a-2356-4fb4-a3ca-80f7059fe812";
        #region generator
        int IVsSingleFileGenerator.DefaultExtension(
            out string extension)
        {
            extension = GetExtension();
            if (extension == null)
                extension = ".cs";
            return VSConstants.S_OK;
        }
        int IVsSingleFileGenerator.Generate(
            string filePath,
            string fileContent,
            string defaultNamespace,
            IntPtr[] rgbOutputFileContents,
            out uint pcbOutput,
            IVsGeneratorProgress pGenerateProgress)
        {
            if (String.IsNullOrEmpty(fileContent))
                fileContent = File.ReadAllText(filePath, Encoding.Default);


            var result = GenerateBytes(fileContent, filePath, defaultNamespace, pGenerateProgress);
            if (result == null)
            {
                pcbOutput = 0;
                return VSConstants.E_FAIL;
            }
            int outputLength = result.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
            Marshal.Copy(result, 0, rgbOutputFileContents[0], outputLength);
            pcbOutput = (uint)outputLength;
            return VSConstants.S_OK;
        }

        private byte[] GenerateBytes(string inputContent, string inputPath, string defaultNamespace, IVsGeneratorProgress progress)
        {
            var result = GenerateString(inputContent, inputPath, defaultNamespace, progress);
            if (result == null)
                return null;
            return Encoding.UTF8.GetBytes(result);
        }

        private string GenerateString(string inputContent, string inputPath, string defaultNamespace, IVsGeneratorProgress progress)
        {
            return $"Here can be generated string using {inputContent.Length} input bytes from {inputPath} and namespace {defaultNamespace}";
        }

        private string GetExtension()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.pUnkSite == null)
                return null;
            var oleSp = this.pUnkSite as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
            if (oleSp == null)
                return null;
            var serviceProvider = new ServiceProvider(oleSp);
            var dte = serviceProvider.GetService(typeof(SDTE)) as DTE;
            if (dte == null)
                return null;
            if (dte.SelectedItems.Count <= 0)
                return null;
            var projectItem = dte.SelectedItems?.Item(1)?.ProjectItem;
            if (projectItem == null)
                return null;
            var fn = projectItem.FileNames[1];
            var ext = Path.GetExtension(fn);
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            return "." + String.Join("", ext.Reverse());
        }

        #endregion

        #region Site
        private object pUnkSite;

        void IObjectWithSite.SetSite(object pUnkSite)
        {
            this.pUnkSite = pUnkSite;
        }

        void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            IntPtr pUnk = Marshal.GetIUnknownForObject(this.pUnkSite);
            IntPtr ppv = IntPtr.Zero;
            Marshal.QueryInterface(pUnk, ref riid, out ppv);
            ppvSite = ppv;
        }
        #endregion 
    }

    /*

    [Guid("f31d2baf-0eb1-4c27-afe4-0e2af2884c66")]
    [ComVisible(true)]
    public class SolutionFileGeneratorFactory : IVsSingleFileGeneratorFactory
    {
        /// <summary>
        /// Gets the default generator prog ID for a specified file.
        /// </summary>
        int IVsSingleFileGeneratorFactory.GetDefaultGenerator(
            string wszFilename, 
            out string pbstrGenProgID)
        {
            pbstrGenProgID = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSingleFileGeneratorFactory.CreateGeneratorInstance(
            string wszProgId, 
            out int pbGeneratesDesignTimeSource, 
            out int pbGeneratesSharedDesignTimeSource, 
            out int pbUseTempPEFlag, 
            out IVsSingleFileGenerator ppGenerate)
        {
            throw new NotImplementedException();
        }

        int IVsSingleFileGeneratorFactory.GetGeneratorInformation(
            string wszProgId, 
            out int pbGeneratesDesignTimeSource, 
            out int pbGeneratesSharedDesignTimeSource, 
            out int pbUseTempPEFlag, 
            out Guid pguidGenerator)
        {
            throw new NotImplementedException();
        }
    }
    */
}
