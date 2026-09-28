using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.ViewModel;

/// <summary>Turns the sheet about to be written into the preview shown in the window, cell for cell.</summary>
public static class KataPreviewBuilder
{
    private static readonly string[] HeaderLabels =
        { "B3 tên", "B4 SL", "B5 h", "B6 b", "B7 h sàn", "B8 trục", "B9 lệch trục", "B10 cao trình" };

    public static string HeaderLine(KataSheet sheet) =>
        string.Join("   ", HeaderLabels.Select((label, i) => $"{label}: {Shown(KataColumnLetters.CellText(sheet.HeaderColumn[i]))}"));

    public static IReadOnlyList<KataPreviewColumn> Columns(KataElevation elevation) => elevation.Columns
        .Select(c => new KataPreviewColumn(c.Letter, KindName(c, elevation), c.Row11, c.Row19, "-", c.Row21, c.Row22, c.Row23))
        .ToList();

    /// <summary>What a column stands for, in the words of the office: support kind, span number, joint or free end.</summary>
    public static string KindName(KataElevationColumn column, KataElevation elevation) => column.Kind switch
    {
        KataColumnKind.Span => $"Nhịp {column.SpanNumber}",
        KataColumnKind.Joint => "Nối",
        KataColumnKind.FreeEnd => "Đầu tự do",
        _ => elevation.Supports.FirstOrDefault(s => s.ColumnIndex == column.Index)?.Kind switch
        {
            KataSupportKind.Foundation => "Móng",
            KataSupportKind.Beam => "Dầm giao",
            _ => "Cột"
        }
    };

    /// <summary>One line naming the selected column, e.g. "Cột E · Nhịp 2 · 250x600 · dài 5600 mm".</summary>
    public static string Describe(KataElevation? elevation, int columnIndex)
    {
        if (elevation is null || columnIndex < 0 || columnIndex >= elevation.Columns.Count)
            return "Bấm vào hình hoặc bảng để chọn một cột Kata.";

        var column = elevation.Columns[columnIndex];
        string size = column.Kind == KataColumnKind.Span ? $"{column.SpanSection} · dài {column.Row11} mm" : $"hàng 11 = {Shown(column.Row11)}";
        return $"Cột {column.Letter} · {KindName(column, elevation)} · {size}";
    }

    private static string Shown(string cell) => cell.Length == 0 ? "·" : cell;
}
