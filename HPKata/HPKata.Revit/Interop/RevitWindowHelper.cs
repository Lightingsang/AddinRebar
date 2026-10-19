using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using HPKata.Revit.Entry;
using HPKata.UI.Common;

namespace HPKata.Revit.Interop;

/// <summary>
/// Helper for safely hosting WPF windows inside Revit 2026 with correct Win32 ownership
/// and dynamic Dark/Light theme synchronization.
/// </summary>
public static class RevitWindowHelper
{
    /// <summary>
    /// Displays a WPF window modally with Revit MainWindowHandle as owner.
    /// </summary>
    public static bool? ShowModal(Window window, UIApplication uiApp)
    {
        var helper = new WindowInteropHelper(window)
        {
            Owner = uiApp.MainWindowHandle
        };

        ThemeManager.Attach(window, RevitHostTheme.Instance);
        return window.ShowDialog();
    }

    /// <summary>
    /// Displays a WPF window modelessly with Revit MainWindowHandle as owner.
    /// </summary>
    public static void ShowModeless(Window window, UIApplication uiApp)
    {
        var helper = new WindowInteropHelper(window)
        {
            Owner = uiApp.MainWindowHandle
        };

        ThemeManager.Attach(window, RevitHostTheme.Instance);
        window.Show();
    }
}
