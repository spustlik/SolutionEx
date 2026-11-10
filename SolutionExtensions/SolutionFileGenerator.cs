using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SolutionExtensions
{

    [Guid(ProgId)]
    [ComVisible(true)]
    public class SolutionFileGenerator : IVsSingleFileGenerator//, IObjectWithSite
    {
        public const string ProgId = "9ecce53a-2356-4fb4-a3ca-80f7059fe812";
        private string lastExtension;

        #region generator
        int IVsSingleFileGenerator.DefaultExtension(out string extension)
        {
            extension = lastExtension;
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
            var package = SolutionExtensionsPackage.GetGlobal();
            try
            {
                var result = package.ExtensionManager.RunGeneratorOnFile(
                    inputPath, defaultNamespace, argument: null /*!!!*/);
                lastExtension = result.Extension;
                if (result.Content != null)
                    return result.Content;
                return File.ReadAllText(result.Files[0]);
            }
            catch (Exception ex)
            {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                progress.GeneratorError(0, 1, ex.Message, 0, 0);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                return null;
            }
        }

        #endregion

        /*
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
        */
    }

}
