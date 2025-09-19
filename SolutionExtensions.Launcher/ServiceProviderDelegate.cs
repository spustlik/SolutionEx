using System;
using System.Runtime.InteropServices;
using IOleSvcProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace SolutionExtensions.Launcher
{

    [ComVisible(true)]
    [Guid("11634ad1-90c0-4c61-a339-102080056c54")]
    public class ServiceProviderDelegate : IServiceProvider
    {
        private readonly Func<Type, object> provide;
        
        public ServiceProviderDelegate(Func<Type, object> provide)
        {
            this.provide = provide;
        }
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(string))
                return GetType().FullName;
            return provide(serviceType);
        }

    }

    [ComVisible(true)]
    [Guid("51c1082f-85b8-496d-92f1-eb39bb396cff")]
    public class ServiceProviderOle : IServiceProvider, IOleSvcProvider
    {
        private readonly IOleSvcProvider oleSvcProvider;

        public ServiceProviderOle(IOleSvcProvider oleSvcProvider)
        {
            this.oleSvcProvider = oleSvcProvider;
        }
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(string))
                return GetType().FullName;
            return oleSvcProvider.QueryService(serviceType.GUID);
        }

        int IOleSvcProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            return oleSvcProvider.QueryService(ref guidService, ref riid, out ppvObject);
        }
    }
}
