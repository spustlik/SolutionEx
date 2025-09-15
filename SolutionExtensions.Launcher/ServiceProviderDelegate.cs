using System;
using System.Runtime.InteropServices;

namespace SolutionExtensions.Launcher
{
        
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
}
