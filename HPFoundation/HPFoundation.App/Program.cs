using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using HPFoundation.App.Etabs;
using HPFoundation.App.Excel;
using Microsoft.CSharp.RuntimeBinder;

namespace HPFoundation.App;

/// <summary>Command line used until the window exists, and afterwards by the live checks.</summary>
internal static class Program
{
    private const string FourColumnMarker = "4COT";
    private const string RunsFolderName = @"HPFoundation\runs";
    private const int FailureExitCode = 1;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [STAThread]
    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        EtabsInstallation.InstallResolver();
        try
        {
            return args switch
            {
                ["inspect", var workbook, .. var sheets] => Inspect(workbook, sheets),
                ["dump-grid", var workbook, var sheet, var outputPath] => DumpGrid(workbook, sheet, outputPath),
                ["run-spec", var specPath, var outputFolder] => EtabsCommands.RunSpec(specPath, outputFolder),
                _ => ShowUsage(),
            };
        }
        catch (Exception exception) when (exception is COMException or RuntimeBinderException or InvalidOperationException or IOException)
        {
            Console.Error.WriteLine($"Failed: {exception.Message}");
            return FailureExitCode;
        }
    }

    /// <summary>Prepares each sheet on a private copy, writes one input file per sheet and prints what was found.</summary>
    private static int Inspect(string workbookPath, string[] requestedSheets)
    {
        var runFolder = NewRunFolder(Path.GetFileNameWithoutExtension(workbookPath));
        var copy = WorkbookCopy.Create(workbookPath, runFolder);
        Console.WriteLine($"Copy: {copy.CopyPath}");
        Console.WriteLine($"Source SHA-256: {copy.SourceSha256}");

        var failures = 0;
        using (var excel = ExcelSession.Open(copy.CopyPath))
        {
            var sheets = requestedSheets.Length > 0
                ? requestedSheets
                : excel.VisibleSheetNames()
                    .Where(name => name.Contains(FourColumnMarker, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            if (sheets.Length == 0)
            {
                Console.WriteLine("No matching sheet found.");
                failures++;
            }

            foreach (var sheet in sheets)
            {
                var prepared = SheetPreparation.Prepare(excel, sheet);
                failures += Report(prepared, copy, runFolder);
            }

            excel.Save();
        }

        var isSourceUnchanged = copy.IsSourceUnchanged();
        Console.WriteLine($"Source file: {(isSourceUnchanged ? "unchanged" : "CHANGED DURING THE RUN")}");
        return isSourceUnchanged ? failures : failures + 1;
    }

    private static int Report(PreparedSheet prepared, WorkbookCopy copy, string runFolder)
    {
        Console.WriteLine($"--- {prepared.SheetName}");
        foreach (var anchor in prepared.MissingAnchors)
        {
            Console.WriteLine($"  MISSING ANCHOR: {anchor}");
        }

        foreach (var issue in prepared.Issues)
        {
            Console.WriteLine($"  ISSUE {issue.Cell}: {issue.Message}");
        }

        foreach (var change in prepared.MomentChanges)
        {
            Console.WriteLine($"  {change.Cell}: {change.OldValueTm} -> {change.NewValueTm} T.m");
        }

        if (prepared.Input is { } input)
        {
            var axes = string.Join(", ", input.Columns.Select(column => $"{column.Axis}@{column.AxisPositionM:0.##}"));
            Console.WriteLine($"  L={input.FootingLengthM:0.##} m, axes {axes}");
            Console.WriteLine($"  b={input.Section.FootingWidthM} bd={input.Section.BeamWidthM} h={input.Section.BeamDepthM} h1={input.Section.FlangeDepthM}");
            Console.WriteLine($"  k={input.Springs.InteriorStiffnessTPerM:0.###} / {input.Springs.EndStiffnessTPerM:0.###} T/m, {input.ConcreteGrade}, {input.SteelGrade}");
        }

        if (!prepared.IsReady)
        {
            return 1;
        }

        var inputPath = Path.Combine(runFolder, $"{FileSafe(prepared.SheetName)}-input.json");
        var source = new { copy.SourcePath, copy.SourceSha256, copy.SourceLastWriteUtc };
        File.WriteAllText(inputPath, JsonSerializer.Serialize(new { source, prepared.Input }, Json));
        return 0;
    }

    private static int DumpGrid(string workbookPath, string sheetName, string outputPath)
    {
        if (!outputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The grid dump must go to a .json file.");
        }

        var runFolder = NewRunFolder(Path.GetFileNameWithoutExtension(workbookPath));
        var copy = WorkbookCopy.Create(workbookPath, runFolder);
        using var excel = ExcelSession.Open(copy.CopyPath);
        var cells = excel.ReadGrid(sheetName).Cells
            .OrderBy(entry => entry.Key.Row)
            .ThenBy(entry => entry.Key.Column)
            .Select(entry => new { address = entry.Key.ToString(), entry.Value.Number, entry.Value.Text, entry.Value.ErrorText, entry.Value.Formula });
        File.WriteAllText(outputPath, JsonSerializer.Serialize(new { sheetName, cells }, Json));
        Console.WriteLine($"Wrote {outputPath}");
        return 0;
    }

    private static string NewRunFolder(string workbookName)
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, RunsFolderName, $"{FileSafe(workbookName)}-{DateTime.Now:yyyyMMdd-HHmmss}");
    }

    private static string FileSafe(string name) =>
        string.Concat(name.Select(letter => Path.GetInvalidFileNameChars().Contains(letter) ? '_' : letter));

    private static int ShowUsage()
    {
        Console.WriteLine("HPFoundation.App inspect <workbook.xls> [sheet ...]");
        Console.WriteLine("HPFoundation.App dump-grid <workbook.xls> <sheet> <output.json>");
        Console.WriteLine("HPFoundation.App run-spec <model-spec.json> <output-folder>");
        return 2;
    }
}
