using System.Collections.Generic;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Abstractions
{
    /// <summary>
    /// Decoupled interface representing active AutoCAD document state and operations.
    /// Enables 100% headless testing outside acad.exe and full integration inside AutoCAD 2026.
    /// </summary>
    public interface ICadDocumentContext
    {
        string DocumentPath { get; }
        string DocumentName { get; }

        IReadOnlyList<CadEntityDto> GetAllRebarEntities();
        IReadOnlyList<CadEntityDto> GetSelectedRebarEntities();

        void WriteMessage(string message);
        CadPromptResult PromptPoint(string message, out CadPoint3D point);
        CadPromptResult PromptString(string message, out string value);

        void InsertTable(CadTableDto table, CadPoint3D insertionPoint);
        void UpdateEntityXData(string handle, KataXDataRecord record);
        void UpdateBlockAttributes(string handle, IReadOnlyDictionary<string, string> attributes);

        void HighlightEntity(string handle);
        void ClearHighlights();
    }
}
