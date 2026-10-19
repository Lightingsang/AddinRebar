namespace HPDynamo.McpBridge.Resources.Themes;

using System;
using Autodesk.Revit.UI;
using Serilog;

/// <summary>
///     Synchronizes WPF UI styling with Revit 2026 / Dynamo host color theme.
/// </summary>
public sealed class DynamoHostTheme : IHostTheme
{
    public static DynamoHostTheme Instance { get; } = new();

    private DynamoHostTheme() { }

    public bool IsDark
    {
        get
        {
            try
            {
                return ReadRevitThemeIsDark();
            }
            catch (Exception ex)
            {
                // UIThemeManager may throw if accessed before Revit UI initializes; default to Dark
                Log.Debug(ex, "Failed to read UIThemeManager.CurrentTheme, falling back to Dark");
                return true;
            }
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static bool ReadRevitThemeIsDark()
    {
        return UIThemeManager.CurrentTheme == UITheme.Dark;
    }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
