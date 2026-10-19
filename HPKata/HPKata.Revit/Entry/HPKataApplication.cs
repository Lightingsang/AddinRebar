using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using HPKata.Revit.Services;

namespace HPKata.Revit.Entry;

/// <summary>
/// Revit 2026 External Application entry point for the HPKata rebar detailing add-in.
/// </summary>
public sealed class HPKataApplication : IExternalApplication
{
    private RibbonBuilder? _ribbonBuilder;
    private EventHandler<ThemeChangedEventArgs>? _onThemeChanged;

    public Result OnStartup(UIControlledApplication application)
    {
        KataRevitAssemblyResolver.Initialize();
        _ribbonBuilder = new RibbonBuilder();
        _ribbonBuilder.Build(application);

        _onThemeChanged = (_, _) =>
        {
            bool isDark = RevitHostTheme.Instance.IsDark;
            _ribbonBuilder.UpdateIcons(isDark);
            RevitHostTheme.Instance.NotifyThemeChanged();
        };

        application.ThemeChanged += _onThemeChanged;
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        if (_onThemeChanged != null)
        {
            application.ThemeChanged -= _onThemeChanged;
            _onThemeChanged = null;
        }

        _ribbonBuilder = null;
        return Result.Succeeded;
    }
}
