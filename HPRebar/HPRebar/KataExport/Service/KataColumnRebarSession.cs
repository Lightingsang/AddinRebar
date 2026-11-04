using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.KataRebar.Service;

namespace HPRebar.KataExport.Service;

/// <summary>What step 2 read from Excel: the plan (null while the sheet cannot be read) and the workbook it came from.</summary>
public sealed record KataColumnPlanReading(KataColumnRebarSummary? Summary, string WorkbookName, string WorkbookFullName, string Error);

/// <summary>
///     Step 2 of Kata Column Rebar: reads sheet Cot of the active workbook, plans the bars on the measured columns and
///     creates them. Kata Column Rebar's window is modal over its command, so these calls run on Revit's thread inside
///     the command: no queue is needed.
/// </summary>
internal sealed class KataColumnRebarSession
{
    private const string SheetName = "Cot";
    private const string SheetRange = "A1:V216";

    private readonly Document _doc;
    private readonly KataColumnStackReading _reading;
    private readonly IReadOnlyList<KataColumnStoreyGeometry> _storeys;
    private Planned? _planned;
    private string? _shownSummary;
    private Expectation _expected = new(null, null);

    public KataColumnRebarSession(Document doc, KataColumnStackReading reading)
    {
        _doc = doc;
        _reading = reading;
        _storeys = reading.Input!.Storeys
            .Select(s => new KataColumnStoreyGeometry(s.BaseElevationMm, s.TopElevationMm, s.Section.MinXMm, s.Section.MaxXMm, s.Section.MinYMm, s.Section.MaxYMm))
            .ToList();
    }

    /// <summary>
    ///     Reads sheet Cot and plans the bars; this summary is what the page shows and what <see cref="Generate" />
    ///     creates. After step 1 wrote a workbook, a sheet read from another workbook is refused and another C3 noted.
    /// </summary>
    /// <param name="writtenWorkbook">Full name of the workbook step 1 wrote in this window, if any.</param>
    /// <param name="writtenName">The column name (C3) step 1 wrote, if any.</param>
    public KataColumnPlanReading Plan(string? writtenWorkbook, string? writtenName)
    {
        _expected = new Expectation(writtenWorkbook, writtenName);
        var (reading, planned) = Read(_expected);
        _planned = planned;
        _shownSummary = Fingerprint(reading.Summary);
        return reading;
    }

    /// <summary>
    ///     Creates the plan the page shows, after reading the sheet once more: when the sheet changed since (the user
    ///     edited Excel without reading it again) nothing is created and the page is told to read again.
    /// </summary>
    public KataColumnRebarResult Generate()
    {
        var (reading, planned) = Read(_expected);
        if (Fingerprint(reading.Summary) != _shownSummary)
        {
            return Refused("Sheet Cot đã đổi từ lần đọc trước — đã đọc lại, xem phương án mới rồi bấm Tạo thép.");
        }

        if (planned is null || _planned is null)
        {
            return Refused("Chưa có phương án hợp lệ — sửa sheet Cot rồi đọc lại.");
        }

        var creator = new KataColumnRebarCreator(_doc, _reading, _storeys, planned.Spec, planned.Design, planned.BarTypes);
        return creator.Create();
    }

    private static KataColumnRebarResult Refused(string error) => new(false, 0, 0, Array.Empty<string>(), error);

    private (KataColumnPlanReading Reading, Planned? Planned) Read(Expectation expected)
    {
        var read = KataDamComReader.Read(SheetName, SheetRange);
        if (!read.IsSuccess)
        {
            return (new KataColumnPlanReading(null, read.WorkbookName, read.WorkbookFullName, read.Error), null);
        }

        var spec = KataColumnSpecReader.Read(read.Cells!);
        if (spec.Spec is null)
        {
            var refused = new KataColumnRebarSummary(string.Empty, 0, 0, Array.Empty<KataColumnRebarSummaryRow>(), Array.Empty<string>(), spec.Errors, Array.Empty<KataColumnSectionDrawing>(), null);
            return (new KataColumnPlanReading(refused, read.WorkbookName, read.WorkbookFullName, string.Empty), null);
        }

        var rules = new KataColumnBarRules(TopContinues: _reading.TopContinues, CrankSlope: KataSettingsStore.Load().CrankSlope);
        var design = KataColumnRebarDesigner.Design(spec.Spec, _storeys, rules);
        var (barTypes, missing) = BarTypes(spec.Spec, design);
        var summary = KataColumnRebarSummaryMapper.Map(spec.Spec, design, _storeys);
        var errors = summary.Errors.Concat(missing).Concat(IdentityErrors(read, spec.Spec, expected)).ToList();
        var notes = summary.Notes.Concat(NameNotes(spec.Spec, expected)).ToList();
        summary = summary with { Errors = errors, Notes = notes };

        var planned = errors.Count == 0 ? new Planned(spec.Spec, design, barTypes) : null;
        return (new KataColumnPlanReading(summary, read.WorkbookName, read.WorkbookFullName, string.Empty), planned);
    }

    /// <summary>Bars planned from another workbook than the one step 1 wrote, or without a column name, are refused.</summary>
    private static IEnumerable<string> IdentityErrors(KataDamReadResult read, KataColumnSpec spec, Expectation expected)
    {
        if (expected.Workbook is { Length: > 0 } written && !string.Equals(read.WorkbookFullName, written, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"Sheet Cot đang đọc từ '{read.WorkbookFullName}', không phải workbook vừa ghi '{written}' — kích hoạt lại workbook đó trong Excel.";
        }

        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            yield return "Ô C3 (tên cột) trống — số hiệu thép cần tên cột.";
        }
    }

    private static IEnumerable<string> NameNotes(KataColumnSpec spec, Expectation expected)
    {
        if (expected.Name is { Length: > 0 } written && !string.Equals(spec.Name.Trim(), written.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            yield return $"Sheet ghi tên cột '{spec.Name}', khác '{written}' vừa ghi ở bước 1.";
        }
    }

    private static string Fingerprint(KataColumnRebarSummary? summary) => JsonSerializer.Serialize(summary);

    /// <summary>One bar type per Ø the plan uses (main bars deformed first); every Ø the model lacks is an error.</summary>
    private (Dictionary<double, RebarBarType> Types, List<string> Missing) BarTypes(KataColumnSpec spec, KataColumnRebarDesign design)
    {
        var resolver = new KataRebarTypeResolver(_doc);
        var types = new Dictionary<double, RebarBarType>();
        var missing = new List<string>();
        var wanted = design.Bars.Select(b => (b.Run.DiameterMm, Longitudinal: true))
            .Append((spec.Hoops.DiameterMm, Longitudinal: false))
            .Append((spec.Ties.DiameterMm, Longitudinal: false))
            .Distinct();
        foreach (var (diameter, longitudinal) in wanted)
        {
            if (types.ContainsKey(diameter))
            {
                continue;
            }

            if (resolver.Resolve(string.Empty, diameter, longitudinal) is { } option && _doc.GetElement(option.Id) is RebarBarType type)
            {
                types[diameter] = type;
            }
            else
            {
                missing.Add($"Model chưa có kiểu thép (Rebar Bar Type) Ø{diameter}.");
            }
        }

        return (types, missing);
    }

    private sealed record Expectation(string? Workbook, string? Name);

    private sealed record Planned(KataColumnSpec Spec, KataColumnRebarDesign Design, Dictionary<double, RebarBarType> BarTypes);
}
