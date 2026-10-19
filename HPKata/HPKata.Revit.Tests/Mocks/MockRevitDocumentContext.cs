using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Tests.Mocks;

/// <summary>
/// In-memory mock document context for headless unit testing of runners and commands.
/// </summary>
public sealed class MockRevitDocumentContext : IRevitDocumentContext
{
    public string DocumentTitle { get; set; } = "MockRevitDoc";
    public string DocumentPath { get; set; } = string.Empty;
    public string ActiveViewName { get; set; } = "3D View 1";

    public List<RevitRebarDto> AllRebars { get; } = [];
    public List<RevitRebarDto> SelectedRebars { get; } = [];

    public List<string> Notifications { get; } = [];
    public List<string> Errors { get; } = [];
    public List<RevitScheduleTableDto> CreatedTables { get; } = [];

    public IReadOnlyList<RevitRebarDto> GetAllRebars() => AllRebars;

    public IReadOnlyList<RevitRebarDto> GetSelectedRebars() => SelectedRebars;

    public void WriteNotification(string message) => Notifications.Add(message);

    public void ShowError(string message) => Errors.Add(message);

    public void CreateOrUpdateScheduleTable(RevitScheduleTableDto table) => CreatedTables.Add(table);
}
