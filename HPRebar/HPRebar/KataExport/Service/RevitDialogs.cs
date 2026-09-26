using Autodesk.Revit.UI;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Revit TaskDialog wrappers for KataExport messages and alerts.
/// </summary>
public static class RevitDialogs
{
    public static void Error(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconError, MainInstruction = title, MainContent = message }.Show();

    public static void Warning(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconWarning, MainInstruction = title, MainContent = message }.Show();

    public static void Info(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconInformation, MainInstruction = title, MainContent = message }.Show();
}
