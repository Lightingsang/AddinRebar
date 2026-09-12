using Autodesk.Revit.UI;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     TaskDialog wrappers replacing the WinForms MessageBox calls of the original tool,
///     so the add-in needs no System.Windows.Forms reference on any target framework.
/// </summary>
internal static class RevitDialogs
{
    public static void Error(string title, string message)
    {
        var dialog = new TaskDialog(title)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconError,
            MainInstruction = title,
            MainContent = message
        };

        dialog.Show();
    }

    public static void Warning(string title, string message)
    {
        var dialog = new TaskDialog(title)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconWarning,
            MainInstruction = title,
            MainContent = message
        };

        dialog.Show();
    }

    public static void Info(string title, string message)
    {
        var dialog = new TaskDialog(title)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconInformation,
            MainInstruction = title,
            MainContent = message
        };

        dialog.Show();
    }

    /// <summary>Yes/No prompt. Returns true only when the user picks Yes.</summary>
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
