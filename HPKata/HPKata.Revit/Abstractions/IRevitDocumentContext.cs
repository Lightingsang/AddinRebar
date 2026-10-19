namespace HPKata.Revit.Abstractions;

/// <summary>
/// Abstraction for interacting with a Revit document without tying business logic
/// to native Revit API types. Enables 100% headless testing.
/// </summary>
public interface IRevitDocumentContext
{
    string DocumentTitle { get; }
    string DocumentPath { get; }
    string ActiveViewName { get; }

    IReadOnlyList<RevitRebarDto> GetAllRebars();
    IReadOnlyList<RevitRebarDto> GetSelectedRebars();

    void WriteNotification(string message);
    void ShowError(string message);
    void CreateOrUpdateScheduleTable(RevitScheduleTableDto table);
}
