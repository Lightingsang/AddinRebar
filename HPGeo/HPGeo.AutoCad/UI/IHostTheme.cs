namespace HPGeo.AutoCad.UI;

/// <summary>
///     Where a window learns whether the host UI is dark and when that changes. One implementation per host
///     (Revit: UIThemeManager; AutoCAD: COLORTHEME; Windows apps: AppsUseLightTheme) keeps the theme bridge host-free.
/// </summary>
public interface IHostTheme
{
    bool IsDark { get; }

    /// <summary>Raised on any thread; the bridge marshals to the window's dispatcher.</summary>
    event Action? Changed;
}
