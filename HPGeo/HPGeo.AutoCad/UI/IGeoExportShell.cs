namespace HPGeo.AutoCad.UI;

/// <summary>
/// The few things the view model needs from the host that are not pure: a save dialog and launching a file
/// or URL. Injected so the view model stays testable without a window.
/// </summary>
public interface IGeoExportShell
{
    /// <summary>Asks for the KMZ path; null when the user cancels.</summary>
    string? AskSavePath(string? initialDirectory, string suggestedFileName);

    /// <summary>Opens a file with its associated application (a .kmz opens in Google Earth Pro).</summary>
    void OpenPath(string path);

    void OpenUrl(string url);
}
