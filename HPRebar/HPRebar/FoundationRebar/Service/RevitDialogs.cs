using Autodesk.Revit.UI;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
/// Revit TaskDialog wrapper replacing WinForms dialogs.
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
