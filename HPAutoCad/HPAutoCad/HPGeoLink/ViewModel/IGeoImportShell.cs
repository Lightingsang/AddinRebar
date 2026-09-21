namespace HPAutoCad.HPGeoLink.ViewModel;

/// <summary>The host side the import dialog needs: an open-file dialog.</summary>
public interface IGeoImportShell
{
    string? AskOpenPath();
}
