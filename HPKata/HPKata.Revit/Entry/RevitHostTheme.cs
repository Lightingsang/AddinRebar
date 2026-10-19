using Autodesk.Revit.UI;
using HPKata.UI.Common;

namespace HPKata.Revit.Entry;

/// <summary>
/// Revit 2026 host theme wrapper implementing HPKata.UI.Common.IHostTheme.
/// Bridges Autodesk.Revit.UI.UIThemeManager to the reactive ThemeManager in HPKata.UI.
/// </summary>
public sealed class RevitHostTheme : IHostTheme
{
    private static readonly Lazy<RevitHostTheme> LazyInstance = new(() => new RevitHostTheme());

    public static RevitHostTheme Instance => LazyInstance.Value;

    public bool IsDark => UIThemeManager.CurrentTheme == UITheme.Dark;

    public event Action? Changed;

    public void NotifyThemeChanged() => Changed?.Invoke();
}
