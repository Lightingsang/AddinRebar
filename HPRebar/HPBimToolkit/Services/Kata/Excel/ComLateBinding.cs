using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace HPBimToolkit.Services.Kata.Excel;

/// <summary>
///     Late-bound calls on Excel COM objects through <see cref="Type.InvokeMember" /> (no PIA, no <c>dynamic</c>).
///     Ported from <c>HPRebar/HPRebar/KataExport/Service</c> (ADR-0009).
/// </summary>
public static class ComLateBinding
{
    private const BindingFlags GetFlags = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public;
    private const BindingFlags SetFlags = BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public;
    private const BindingFlags MethodFlags = BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public;

    public static object? Get(object target, string propertyName, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Invoke(target, propertyName, GetFlags, args.Length == 0 ? null : args);
    }

    public static void Set(object target, string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(target);
        Invoke(target, propertyName, SetFlags, [value]);
    }

    public static object? Call(object target, string methodName, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Invoke(target, methodName, MethodFlags, args.Length == 0 ? null : args);
    }

    /// <summary>Releases a COM wrapper; a wrapper already released or not COM is left alone.</summary>
    public static void Release(object? comObject)
    {
        if (comObject is null || !Marshal.IsComObject(comObject))
        {
            return;
        }

        try
        {
            Marshal.ReleaseComObject(comObject);
        }
        catch (InvalidComObjectException)
        {
            // Already detached from its COM object (released twice): nothing left to free.
        }
    }

    /// <summary>
    ///     Reflection wraps a failing COM call in <see cref="TargetInvocationException" />; the inner
    ///     <see cref="COMException" /> is rethrown so callers can react to its HRESULT (Excel busy, call rejected).
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
}
