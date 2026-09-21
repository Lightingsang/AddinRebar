using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using HPAutoCad.HPGeoLink.Support;

namespace HPAutoCad.Resources.Themes;

/// <summary>
///     AutoCAD's COLORTHEME (0 = dark, 1 = light; unreadable = dark) as an <see cref="IHostTheme" />. The change event
///     fires on the command thread; MaterialThemeBridge marshals the re-apply onto the window's dispatcher.
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
                return Convert.ToInt32(AcadApp.GetSystemVariable("COLORTHEME")) == 0;
            }
            catch (Exception exception)
            {
                HPGeoLog.Warning("COLORTHEME unreadable; assuming dark: " + exception.Message);
                return true;
            }
        }
    }

    public event Action? Changed;
}
