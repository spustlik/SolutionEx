using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SolutionExtensions
{
    // Helper for use of ROT (Running object table)
    public static class RunningComObjects
    {
        /*
        •	For .NET objects, use [ComVisible(true)] and a GUID.
        •	Both processes must agree on the moniker name (e.g., "MyComObject").
        •	The object must be COM-visible and registered for COM interop.
        •	You may need to register your assembly for COM interop (regasm).
        */
        public static void RegisterComObject(object comObject, string monikerName)
        {
            IRunningObjectTable rot = GetROT();
            IMoniker moniker = CreateMoniker(monikerName);
            var result = rot.Register(0, comObject, moniker);
            //Marshal.ThrowExceptionForHR(hr);
            var r1 = rot.IsRunning(moniker);
            //var r2 = rot.IsRunning(moniker.na)
        }

        public static IMoniker CreateMoniker(string monikerName)
        {
            var hr = CreateItemMoniker("!", monikerName, out var moniker);
            Marshal.ThrowExceptionForHR(hr);
            return moniker;
        }

        public static IRunningObjectTable GetROT()
        {
            var hr = GetRunningObjectTable(0, out var rot);
            Marshal.ThrowExceptionForHR(hr);
            return rot;
        }

        public static object GetRunningComObject(string monikerName)
        {
            var rot = GetROT();
            return EnumerateRunning(rot)
                .FirstOrDefault(m => m.GetMonikerDisplayName().EndsWith(monikerName))
                ?.GetMonikerObject(rot);
        }

        public static IEnumerable<IMoniker> EnumerateRunning(IRunningObjectTable rot)
        {
            rot.EnumRunning(out var enumMoniker);
            IMoniker[] monikers = new IMoniker[1];
            IntPtr fetched = IntPtr.Zero;
            while (enumMoniker.Next(1, monikers, fetched) == 0)
            {
                yield return monikers[0];
            }
        }

        public static object GetMonikerObject(this IMoniker moniker, IRunningObjectTable rot)
        {
            var hr = rot.GetObject(moniker, out var obj);
            Marshal.ThrowExceptionForHR(hr);
            return obj;
        }

        public static string GetMonikerDisplayName(this IMoniker moniker)
        {
            var hr = CreateBindCtx(0, out var ctx);
            Marshal.ThrowExceptionForHR(hr);
            moniker.GetDisplayName(ctx, null, out var displayName);
            return displayName;
        }

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable pprot);

        [DllImport("ole32.dll")]
        private static extern int CreateItemMoniker(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszDelim,
            [MarshalAs(UnmanagedType.LPWStr)] string lpszItem,
            out IMoniker ppmk);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    }
}
