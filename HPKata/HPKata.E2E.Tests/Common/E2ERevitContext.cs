using System.Collections.Generic;
using HPKata.Revit.Abstractions;

namespace HPKata.E2E.Tests.Common;

/// <summary>
/// Headless in-memory Revit document context for end-to-end integration testing.
/// </summary>
public sealed class E2ERevitContext : IRevitDocumentContext
{
    public string DocumentTitle { get; set; } = "E2E_StructuralModel";
    public string DocumentPath { get; set; } = @"C:\BimModels\E2E_Building_Rebar.rvt";
    public string ActiveViewName { get; set; } = "{3D - Rebar View}";

    public List<RevitRebarDto> AllRebars { get; } = [];
    public List<RevitRebarDto> SelectedRebars { get; } = [];
    public List<string> Notifications { get; } = [];
    public List<string> Errors { get; } = [];
    public List<RevitScheduleTableDto> CreatedTables { get; } = [];

    public IReadOnlyList<RevitRebarDto> GetAllRebars() => AllRebars;

    public IReadOnlyList<RevitRebarDto> GetSelectedRebars() =>
        SelectedRebars.Count > 0 ? SelectedRebars : AllRebars;

    public void WriteNotification(string message) => Notifications.Add(message);

    public void ShowError(string message) => Errors.Add(message);

    public void CreateOrUpdateScheduleTable(RevitScheduleTableDto table) =>
        CreatedTables.Add(table);
}
