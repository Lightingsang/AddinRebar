using HPAutoCad.McpBridge.Resources.Themes;
using Serilog;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.McpBridge.Service;

/// <summary>
///     AutoCAD's COLORTHEME (0 = dark, 1 = light) as an <see cref="IHostTheme" />. The change event fires on the
///     command thread; MaterialThemeBridge marshals the re-apply onto the window's dispatcher.
/// </summary>
public sealed class AutocadHostTheme : IHostTheme
{
    public static AutocadHostTheme Instance { get; } = new();

    private AutocadHostTheme()
    {
        AcadApp.SystemVariableChanged += (_, args) =>
        {
            if (string.Equals(args.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase)) Changed?.Invoke();
        };
    }

    public bool IsDark
    {
        get
        {
            try
            {
                return AcadApp.GetSystemVariable("COLORTHEME") is not (short and 1 or int and 1);
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "COLORTHEME unreadable; assuming dark");
                return true;
            }
        }
    }

    public event Action? Changed;
}
