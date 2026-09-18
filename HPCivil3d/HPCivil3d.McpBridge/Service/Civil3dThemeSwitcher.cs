using System.Windows;
using Serilog;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPCivil3d.McpBridge.Service;

/// <summary>
///     Follows AutoCAD's own colour scheme: the COLORTHEME system variable is 0 for dark and 1 for
///     light. The window's XAML merges the dark palette; light is an override dictionary layered on
///     top, so every brush stays a DynamicResource token and nothing is hard-coded per theme.
/// </summary>
public static class Civil3dThemeSwitcher
{
    private static readonly Uri LightPalette = new Uri("/HPCivil3d.McpBridge;component/Resources/Themes/Civil3dThemeLight.xaml", UriKind.Relative);

    /// <summary>Call inside the assembly's contextual reflection scope, right after InitializeComponent.</summary>
    public static void ApplyFromAutocad(Window window)
    {
        if (!IsLight()) return;

        try
        {
            window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = LightPalette });
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP bridge light palette could not be loaded; staying dark");
        }
    }

    private static bool IsLight()
    {
        try
        {
            return AcadApp.GetSystemVariable("COLORTHEME") is short and 1 or int and 1;
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "COLORTHEME unreadable; assuming dark");
            return false;
        }
    }
}
