using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace HPKata.Core.Excel.Session
{
    /// <summary>
    /// Pure reflection-based late binding and Win32 AccessibleObjectFromWindow helpers for COM Excel automation.
    /// Enumerates all running desktop Excel windows (XLMAIN / EXCEL7) as well as the Running Object Table.
    /// </summary>
    public static class ComLateBindingHelper
    {
        private const uint OBJID_NATIVEOM = 0xFFFFFFF0;
        private static readonly Guid IID_IDispatch = new("00020400-0000-0000-C000-000000000046");

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string progId, out Guid clsid);

        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.Interface)] out object ppunk);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(
            IntPtr hwnd, uint dwObjectID, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);

        public static object? TryGetActiveExcelApplication()
        {
            var apps = GetRunningExcelApplications();
            if (apps.Count > 0)
            {
                for (int i = 1; i < apps.Count; i++)
                {
                    Release(apps[i]);
                }
                return apps[0];
            }
            return null;
        }

        /// <summary>
        /// Discovers all running Excel.Application instances via Win32 XLMAIN/EXCEL7 windows and ROT fallback.
        /// </summary>
        public static List<object> GetRunningExcelApplications()
        {
            var apps = new List<object>();
            try
            {
                var excel7Windows = new List<IntPtr>();
                EnumWindows((topHwnd, _) =>
                {
                    if (IsWindowClass(topHwnd, "XLMAIN"))
                    {
                        EnumChildWindows(topHwnd, (childHwnd, _) =>
                        {
                            if (IsWindowClass(childHwnd, "EXCEL7"))
                            {
                                excel7Windows.Add(childHwnd);
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                    return true;
                }, IntPtr.Zero);

                foreach (IntPtr hwnd in excel7Windows)
                {
                    Guid iid = IID_IDispatch;
                    if (AccessibleObjectFromWindow(hwnd, OBJID_NATIVEOM, ref iid, out object winObj) >= 0 && winObj != null)
                    {
                        try
                        {
                            object? app = GetProperty(winObj, "Application");
                            if (app != null && IsExcelAppVisible(app))
                            {
                                apps.Add(app);
                            }
                            else
                            {
                                Release(app);
                            }
                        }
                        catch
                        {
                            // Ignore inaccessible window object
                        }
                        finally
                        {
                            Release(winObj);
                        }
                    }
                }
            }
            catch
            {
                // Ignore Win32 enumeration failures on non-Windows or restricted desktops
            }

            try
            {
                int hr = CLSIDFromProgID("Excel.Application", out var clsid);
                if (hr >= 0)
                {
                    GetActiveObject(ref clsid, IntPtr.Zero, out var rotApp);
                    if (rotApp != null)
                    {
                        if (IsExcelAppVisible(rotApp))
                        {
                            apps.Add(rotApp);
                        }
                        else
                        {
                            Release(rotApp);
                        }
                    }
                }
            }
            catch
            {
                // No active ROT object
            }

            return apps;
        }

        private static bool IsExcelAppVisible(object app)
        {
            try
            {
                object? vis = GetProperty(app, "Visible");
                return vis is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWindowClass(IntPtr hWnd, string expectedClass)
        {
            var sb = new StringBuilder(64);
            if (GetClassName(hWnd, sb, sb.Capacity) <= 0) return false;
            return string.Equals(sb.ToString(), expectedClass, StringComparison.OrdinalIgnoreCase);
        }

        public static object? GetProperty(object target, string propertyName)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return target.GetType().InvokeMember(
                propertyName,
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public,
                null,
                target,
                null);
        }

        public static object? GetProperty(object target, string propertyName, params object?[] index)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return target.GetType().InvokeMember(
                propertyName,
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public,
                null,
                target,
                index);
        }

        public static void SetProperty(object target, string propertyName, params object?[] args)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.GetType().InvokeMember(
                propertyName,
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public,
                null,
                target,
                args);
        }

        public static object? InvokeMethod(object target, string methodName, params object?[] args)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return target.GetType().InvokeMember(
                methodName,
                BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                null,
                target,
                args);
        }

        public static void Release(object? comObject)
        {
            if (comObject == null) return;
            try
            {
#pragma warning disable CA1416
                if (Marshal.IsComObject(comObject))
                {
                    Marshal.ReleaseComObject(comObject);
                }
#pragma warning restore CA1416
            }
            catch
            {
                // Suppress exception on release of COM reference during cleanup
            }
        }
    }
}
