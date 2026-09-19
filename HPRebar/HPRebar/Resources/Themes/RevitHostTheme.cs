namespace HPRebar.Resources.Themes;

/// <summary>
///     Revit's UI theme as an <see cref="IHostTheme" />. <see cref="NotifyChanged" /> is called from the add-in's
///     <c>ThemeChanged</c> handler (Revit 2024+), so every open window follows Options ▸ Colors ▸ UI theme live.
/// </summary>
public sealed class RevitHostTheme : IHostTheme
{
    public static RevitHostTheme Instance { get; } = new();

    private RevitHostTheme()
    {
    }

    public bool IsDark
    {
        get
        {
            // Multi-version: UIThemeManager arrived in Revit 2024. Earlier versions have no theme API,
            // and their UI is dark, so that is the safe default there.
#if REVIT2024_OR_GREATER
            return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
#else
            return true;
#endif
        }
    }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
