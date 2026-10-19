using System;
using System.Collections.Generic;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Tests.Mocks
{
    /// <summary>
    /// In-memory mock implementing ICadDocumentContext for fast, headless unit tests.
    /// </summary>
    public class MockCadDocumentContext : ICadDocumentContext
    {
        public string DocumentPath { get; set; } = @"C:\TestDrawings\Floor2_RebarPlan.dwg";
        public string DocumentName { get; set; } = "Floor2_RebarPlan.dwg";

        public List<CadEntityDto> Entities { get; } = new();
        public List<CadEntityDto> SelectedEntities { get; } = new();
        public List<string> Messages { get; } = new();
        public List<(CadTableDto Table, CadPoint3D InsertionPoint)> InsertedTables { get; } = new();
        public Dictionary<string, KataXDataRecord> UpdatedXData { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Dictionary<string, string>> UpdatedAttributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> HighlightedHandles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<CadEntityDto> GetAllRebarEntities() => Entities;
        public IReadOnlyList<CadEntityDto> GetSelectedRebarEntities() => SelectedEntities.Count > 0 ? SelectedEntities : Entities;

        public void WriteMessage(string message)
        {
            Messages.Add(message);
        }

        public CadPromptResult PromptPoint(string message, out CadPoint3D point)
        {
            point = new CadPoint3D(100.0, 200.0, 0.0);
            return CadPromptResult.Ok(point);
        }

        public CadPromptResult PromptString(string message, out string value)
        {
            value = "TestString";
            return CadPromptResult.Ok(value);
        }

        public void InsertTable(CadTableDto table, CadPoint3D insertionPoint)
        {
            InsertedTables.Add((table, insertionPoint));
        }

        public void UpdateEntityXData(string handle, KataXDataRecord record)
        {
            UpdatedXData[handle] = record;
        }

        public void UpdateBlockAttributes(string handle, IReadOnlyDictionary<string, string> attributes)
        {
            if (!UpdatedAttributes.TryGetValue(handle, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                UpdatedAttributes[handle] = dict;
            }
            foreach (var kvp in attributes)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        public void HighlightEntity(string handle)
        {
            HighlightedHandles.Add(handle);
        }

        public void ClearHighlights()
        {
            HighlightedHandles.Clear();
        }
    }
}
