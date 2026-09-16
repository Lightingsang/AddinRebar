using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Structural;

namespace HPAutoCad.Aec.Cad;

/// <summary>The schedule as an AutoCAD Table: a title row, a header row, one row per schedule line. Two-phase like every write.</summary>
public static partial class StructuralWriteService
{
    public const double DefaultTableRowHeightMm = 400;
    public const double DefaultTableColumnWidthMm = 3000;

    /// <summary>Marks listed in a row's cell before "… (+n)": a cell is a label, not the register.</summary>
    public const int MaxMarksPerCell = 20;

    private static readonly string[] ScheduleHeader = ["Kind", "Section", "Count", "Total length (m)", "Marks"];

    public static EditResult WriteTable(EditContext cx, IReadOnlyList<ScheduleRow> rows, Point3d insertAt, string title, string? layerName, double rowHeightMm, double columnWidthMm, double textHeightMm, string? space)
    {
        // Phase 1: arguments, space and layer — nothing opened for write yet.
        if (rows.Count == 0) throw new ArgumentException("no schedule rows: no structural member matched the filter/kinds, there is nothing to draw.");
        if (rowHeightMm <= 0 || columnWidthMm <= 0 || textHeightMm <= 0) throw new ArgumentException("rowHeightMm, columnWidthMm and textHeightMm must be > 0.");
        var result = new EditResult();
        var spaceId = cx.SpaceId(space, out var spaceError);
        if (spaceId.IsNull) throw new ArgumentException(spaceError!.Message);
        var layer = cx.LayerForCreate(layerName, out var layerError, out var layerWarning);
        if (layer is null) return EditResult.Refused(layerError!, type: "ACAD_TABLE");
        if (layerWarning is not null) result.Warn(layerWarning);

        // Phase 2.
        var target = (BlockTableRecord)cx.Tr.GetObject(spaceId, OpenMode.ForWrite);
        var table = new Table();
        table.SetDatabaseDefaults(cx.Db);
        table.TableStyle = cx.Db.Tablestyle;
        table.SetSize(rows.Count + 2, ScheduleHeader.Length);
        table.SetRowHeight(cx.ToDrawing(rowHeightMm));
        table.SetColumnWidth(cx.ToDrawing(columnWidthMm));
        table.Position = insertAt;
        table.LayerId = layer.ObjectId;
        table.Cells[0, 0].TextString = title;
        for (var c = 0; c < ScheduleHeader.Length; c++) table.Cells[1, c].TextString = ScheduleHeader[c];
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            table.Cells[r + 2, 0].TextString = row.Kind;
            table.Cells[r + 2, 1].TextString = row.Section;
            table.Cells[r + 2, 2].TextString = row.Count.ToString();
            table.Cells[r + 2, 3].TextString = row.TotalLengthMm > 0 ? (row.TotalLengthMm / 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : "-";
            table.Cells[r + 2, 4].TextString = MarksCell(row.Marks);
        }

        for (var r = 0; r < rows.Count + 2; r++)
            for (var c = 0; c < ScheduleHeader.Length; c++)
                table.Cells[r, c].TextHeight = cx.ToDrawing(textHeightMm);
        table.GenerateLayout();
        target.AppendEntity(table);
        cx.Tr.AddNewlyCreatedDBObject(table, true);
        var handle = table.Handle.ToString();
        result.Created(handle);
        result.Items = [new ItemOutcome(0, true, handle, "ACAD_TABLE")];
        result.Summary = new { handle, rows = rows.Count, columns = ScheduleHeader.Length, layer = layer.Name, space = target.Name };
        return result;
    }

    private static string MarksCell(IReadOnlyList<string> marks) =>
        marks.Count <= MaxMarksPerCell ? string.Join(", ", marks) : string.Join(", ", marks.Take(MaxMarksPerCell)) + $" … (+{marks.Count - MaxMarksPerCell})";
}
