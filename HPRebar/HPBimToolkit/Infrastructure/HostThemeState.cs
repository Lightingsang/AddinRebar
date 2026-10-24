using Autodesk.Revit.UI;

namespace HPBimToolkit.Infrastructure;

/// <summary>
///     Revit's UI theme, read only where the API is legal (start-up and the ThemeChanged event) and cached for the
///     page's handshake, which runs from a WebView message outside any API context.
/// </summary>
public sealed class HostThemeState
{
    public bool IsDark { get; private set; }

    /// <remarks>Call from OnStartup or a Revit event handler only.</remarks>
    public void Refresh() => IsDark = UIThemeManager.CurrentTheme == UITheme.Dark;
}
