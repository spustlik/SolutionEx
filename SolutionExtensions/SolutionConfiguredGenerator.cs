using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SolutionExtensions
{

    [Guid(ProgId)]
    [ComVisible(true)]
    public class SolutionConfiguredGenerator : IVsSingleFileGenerator
    {
        public const string ProgId = "10E6A37F-F716-465F-9FA1-6F5C3E34F643";
        private string lastExtension = ".log";
        #region generator
        int IVsSingleFileGenerator.DefaultExtension(out string extension)
        {
            extension = lastExtension;
            return VSConstants.S_OK;
        }
        int test = 0;
        int IVsSingleFileGenerator.Generate(
            string filePath,
            string fileContent,
            string defaultNamespace,
            IntPtr[] rgbOutputFileContents,
            out uint pcbOutput,
            IVsGeneratorProgress pGenerateProgress)
        {
            if (!CallGenerator(filePath, defaultNamespace, out var genOutput))
            {
                pcbOutput = 0;
                return VSConstants.E_FAIL;
            }
            var result = Encoding.UTF8.GetBytes(genOutput);
            int outputLength = result.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
            Marshal.Copy(result, 0, rgbOutputFileContents[0], outputLength);
            pcbOutput = (uint)outputLength;
            return VSConstants.S_OK;
        }

        private bool CallGenerator(string filePath, string defaultNamespace, out string genOutput)
        {
            //todo: process config file
            //exec, result output return to genOutput
            //how to get output from dll? just log of calling...?
            //TODO: if output is something like multipart, use fileName to lastExtension

            genOutput = null;
            return true;
        }

        private bool CallGenerator(string filePath, string defaultNamespace)
        {
            File.WriteAllText(Path.ChangeExtension(filePath, ".txt"), "#just testing generated file and not return it");
            return true;
        }

        #endregion

    }

}
