using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Late-binding reflection wrapper for Excel COM objects using Type.InvokeMember.
/// Works cleanly on both .NET 8 (without dynamic/PIA) and .NET Framework 4.8.
/// </summary>
public static class ComLateBinding
{
    private const BindingFlags GetFlags = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public;
    private const BindingFlags SetFlags = BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public;
    private const BindingFlags MethodFlags = BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public;

    public static object? Get(object target, string propertyName, params object?[] args)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        return Invoke(target, propertyName, GetFlags, args.Length == 0 ? null : args);
    }

    public static void Set(object target, string propertyName, object? value)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        Invoke(target, propertyName, SetFlags, new[] { value });
    }

    public static object? Call(object target, string methodName, params object?[] args)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        return Invoke(target, methodName, MethodFlags, args.Length == 0 ? null : args);
    }

    /// <summary>
    /// Reflection wraps a failing COM call in <see cref="TargetInvocationException"/>; the inner
    /// <see cref="COMException"/> is rethrown so callers can react to its HRESULT (e.g. Excel busy).
    /// </summary>
    private static object? Invoke(object target, string name, BindingFlags flags, object?[]? args)
    {
        try
        {
            return target.GetType().InvokeMember(name, flags, null, target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable: Throw() never returns
        }
    }

    public static void Release(object? comObj)
    {
        if (comObj is not null && Marshal.IsComObject(comObj))
        {
            try
            {
                Marshal.ReleaseComObject(comObj);
            }
            catch
            {
                // ignored
            }
        }
    }
}
