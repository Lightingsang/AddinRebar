using System.Collections.Generic;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Abstractions
{
    public readonly record struct CadPoint3D(double X, double Y, double Z)
    {
        public static CadPoint3D Origin => new(0.0, 0.0, 0.0);
        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
    }

    public enum CadPromptStatus
    {
        OK,
        Cancel,
        Error,
        None
    }

    public readonly record struct CadPromptResult(CadPromptStatus Status, string? StringResult, CadPoint3D PointResult)
    {
        public static CadPromptResult Ok(string str) => new(CadPromptStatus.OK, str, CadPoint3D.Origin);
        public static CadPromptResult Ok(CadPoint3D pt) => new(CadPromptStatus.OK, null, pt);
        public static CadPromptResult Cancelled => new(CadPromptStatus.Cancel, null, CadPoint3D.Origin);
        public static CadPromptResult Failed => new(CadPromptStatus.Error, null, CadPoint3D.Origin);
    }

    public class CadEntityDto
    {
        public string Handle { get; set; } = string.Empty;
        public string Layer { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public KataXDataRecord XDataRecord { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new();
        public List<CadPoint3D> Vertices { get; set; } = new();
        public double TotalLengthMm { get; set; }
        public double ConstantWidth { get; set; }
    }

    public class CadTableRowDto
    {
        public List<string> Cells { get; } = new();

        public CadTableRowDto() { }

        public CadTableRowDto(IEnumerable<string> cells)
        {
            Cells.AddRange(cells);
        }
    }

    public class CadTableDto
    {
        public string Title { get; set; } = string.Empty;
        public List<string> ColumnHeaders { get; } = new();
        public List<CadTableRowDto> Rows { get; } = new();

        public void AddRow(params string[] cells)
        {
            Rows.Add(new CadTableRowDto(cells));
        }
    }
}
