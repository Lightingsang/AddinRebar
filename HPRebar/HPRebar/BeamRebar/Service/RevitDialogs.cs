using Autodesk.Revit.UI;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Revit TaskDialog wrappers replacing WinForms MessageBox.
/// </summary>
public static class RevitDialogs
{
    public static void Error(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconError, MainInstruction = title, MainContent = message }.Show();

    public static void Warning(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconWarning, MainInstruction = title, MainContent = message }.Show();

    public static void Info(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconInformation, MainInstruction = title, MainContent = message }.Show();

    public static bool Confirm(string title, string message)
    {
        var dialog = new TaskDialog(title)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconWarning,
            MainInstruction = title,
            MainContent = message,
            CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
            DefaultButton = TaskDialogResult.No
        };
        return dialog.Show() == TaskDialogResult.Yes;
    }
}
