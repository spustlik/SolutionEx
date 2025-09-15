using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SolutionExtensions
{
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
    public static class OleExtensions
    {
        public static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GetOLEServiceProvider(this DTE dte, bool throwIfNotFound = false)
        {
            var svcOle = dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
            if (svcOle == null && throwIfNotFound)
                throw new Exception($"DTE is not providing OLE service provider");
            return svcOle;
        }
        public static IEnumerable<IVsPackage> GetPackages(this IVsShell shell)
        {
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
        public static object QueryService<T>(this Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
        {
            return serviceProvider.QueryService(typeof(T).GUID);
        }
        public static object QueryService(this Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider, Guid serviceGuid)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");
            //Guid riid = VSConstants.IID_IUnknown;
            Guid riid = new Guid("{00000000-0000-0000-C000-000000000046}");
            //Guid riid = typeof(IInternalUnknown).GUID;
            var r = serviceProvider.QueryService(ref serviceGuid, ref riid, out var ppvObject);
            Marshal.ThrowExceptionForHR(r);
            if (ppvObject == IntPtr.Zero)
                return null;
            try
            {
                return Marshal.GetObjectForIUnknown(ppvObject);
            }
            finally
            {
                Marshal.Release(ppvObject);
            }
        }

    }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
}
