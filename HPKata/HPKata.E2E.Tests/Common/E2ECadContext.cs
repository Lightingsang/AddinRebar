using System;
using System.Collections.Generic;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Serialization;

namespace HPKata.E2E.Tests.Common;

/// <summary>
/// Headless in-memory CAD document context for end-to-end integration testing.
/// </summary>
public sealed class E2ECadContext : ICadDocumentContext
{
    public string DocumentPath { get; set; } = @"C:\Drawings\Structural_Plan_Beam.dwg";
    public string DocumentName { get; set; } = "Structural_Plan_Beam.dwg";

    public List<CadEntityDto> Entities { get; } = [];
    public List<CadEntityDto> SelectedEntities { get; } = [];
    public List<string> Messages { get; } = [];
    public List<(CadTableDto Table, CadPoint3D InsertionPoint)> InsertedTables { get; } = [];
    public Dictionary<string, KataXDataRecord> UpdatedXData { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Dictionary<string, string>> UpdatedAttributes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> HighlightedHandles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<CadEntityDto> GetAllRebarEntities() => Entities;

    public IReadOnlyList<CadEntityDto> GetSelectedRebarEntities() =>
        SelectedEntities.Count > 0 ? SelectedEntities : Entities;

    public void WriteMessage(string message) => Messages.Add(message);

    public CadPromptResult PromptPoint(string message, out CadPoint3D point)
    {
        point = new CadPoint3D(0.0, 0.0, 0.0);
        return CadPromptResult.Ok(point);
    }

    public CadPromptResult PromptString(string message, out string value)
    {
        value = "E2E_Prompt_Response";
        return CadPromptResult.Ok(value);
    }

    public void InsertTable(CadTableDto table, CadPoint3D insertionPoint) =>
        InsertedTables.Add((table, insertionPoint));

    public void UpdateEntityXData(string handle, KataXDataRecord record) =>
        UpdatedXData[handle] = record;

    public void UpdateBlockAttributes(string handle, IReadOnlyDictionary<string, string> attributes)
    {
        if (!UpdatedAttributes.TryGetValue(handle, out var dict))
        {
            dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            UpdatedAttributes[handle] = dict;
        }

        foreach (var pair in attributes)
        {
            dict[pair.Key] = pair.Value;
        }
    }

    public void HighlightEntity(string handle) => HighlightedHandles.Add(handle);

    public void ClearHighlights() => HighlightedHandles.Clear();
}
