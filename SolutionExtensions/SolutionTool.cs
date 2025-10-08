using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using VSLangProj80;

namespace SolutionExtensions
{
    /*
    [ComVisible(true)]
    [Guid("341d1b76-71d8-4839-8919-a0fc0041b0a6")]
    [ProvideObject(typeof(SolutionTool))]
    [CodeGeneratorRegistration(typeof(SolutionTool), "Solution configured custom tool generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    //or on package
    //[ProvideCodeGenerator(typeof(SolutionTool), nameof(SolutionTool), "Solution configured custom tool generator", true)]

    public class SolutionTool : IVsSingleFileGenerator
    {
        int IVsSingleFileGenerator.DefaultExtension(out string ext)
        {
            ext = ".xml";
            return VSConstants.S_OK;
        }

        int IVsSingleFileGenerator.Generate(
            string filePath, //can be empty in future versions of VS
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
            throw new NotImplementedException();
        }
    }

    public class Tool2 : BaseCodeGeneratorWithSite
    {
        public override string GetDefaultExtension()
        {
            throw new NotImplementedException();
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            throw new NotImplementedException();
        }
    }
    //TODO: attributes
    public class SolutionToolFactory : IVsSingleFileGeneratorFactory
    {
        /// <summary>
        /// Gets the default generator prog ID for a specified file.
        /// </summary>
        /// <param name="wszFilename">The file for which to get the generator prog ID.</param>
        /// <param name="pbstrGenProgID">The default generator prog ID.</param>
        /// <returns>S_OK</returns>
        int IVsSingleFileGeneratorFactory.GetDefaultGenerator(
            string wszFilename,
            out string pbstrGenProgID)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a generator instance.
        /// </summary>
        /// <param name="wszProgId">The prog ID of the generator factory.</param>
        /// <param name="pbGeneratesDesignTimeSource">Boolean value; true if the factory generates source at design time.</param>
        /// <param name="pbGeneratesSharedDesignTimeSource">Boolean value; true if the factory generates shared source at design time.</param>
        /// <param name="pbUseTempPEFlag">Boolean value; true if the factory uses temporary PE flags.</param>
        /// <param name="ppGenerate">instance of generator</param>
        /// <returns>S_OK</returns>
        int IVsSingleFileGeneratorFactory.CreateGeneratorInstance(
            string wszProgId,
            out int pbGeneratesDesignTimeSource,
            out int pbGeneratesSharedDesignTimeSource,
            out int pbUseTempPEFlag,
            out IVsSingleFileGenerator ppGenerate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets information about a generator factory.
        /// </summary>
        /// <param name="wszProgId">[in] The prog ID of the generator factory.</param>
        /// <param name="pbGeneratesDesignTimeSource">[out] Boolean value; true if the factory generates source at design time.</param>
        /// <param name="pbGeneratesSharedDesignTimeSource">[out] Boolean value; true if the factory generates shared source at design time.</param>
        /// <param name="pbUseTempPEFlag">Boolean value; true if the factory uses temporary PE flags.</param>
        /// <param name="pguidGenerator">The GUID of the factory.</param>
        /// <returns>S_OK</returns>
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
