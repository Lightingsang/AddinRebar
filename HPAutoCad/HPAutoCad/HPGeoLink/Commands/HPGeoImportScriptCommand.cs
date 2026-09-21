using System.IO;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Import;
using HPAutoCad.Core.HPGeoLink.Units;
using HPAutoCad.Core.HPGeoLink.Validation;
using HPAutoCad.HPGeoLink.Cad;
using HPAutoCad.HPGeoLink.Support;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.HPGeoLink.Commands;

/// <summary>
/// <c>-HPGEOIMPORT</c> (registered by the loader): no-dialog import of a KML/KMZ for scripts and the acceptance
/// harness. One prompt takes <c>file=&lt;path&gt; cm=&lt;KTT&gt; [k0= fe= fn= unit=]</c> — the same keys as
/// -HPGEOKMZ, with <c>file</c> instead of <c>out</c>.
/// </summary>
internal static class HPGeoImportScriptCommand
{
    public static void Run()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        var ed = doc.Editor;
        var input = ed.GetString(new PromptStringOptions("\nHPGeo import arguments (file=<file.kml|kmz> cm=<KTT> [unit=m|mm] [k0= fe= fn=]): ") { AllowSpaces = true });
        if (input.Status != PromptStatus.OK) return;
        try
        {
            var values = ExportArguments.Tokenize(input.StringResult);
            if (!values.TryGetValue("file", out var file) || string.IsNullOrWhiteSpace(file))
                throw new ArgumentException("Thiếu file=<đường dẫn .kml/.kmz>.");
            if (!File.Exists(file)) throw new ArgumentException($"Không tìm thấy file: {file}");
            // Reuse the export parser for cm/k0/fe/fn/unit by giving it the one key it insists on.
            var rest = values.Where(kv => !string.Equals(kv.Key, "file", StringComparison.OrdinalIgnoreCase)).ToList();
            if (rest.Any(kv => kv.Value.Contains('"'))) throw new ArgumentException("Giá trị tham số không được chứa dấu \".");
            var args = ExportArguments.Parse(string.Join(' ', rest.Select(kv => $"{kv.Key}=\"{kv.Value}\"")) + " out=unused.kmz");

            var ctx = DrawingContext.Read(doc);
            var unit = args.UnitOverride ?? ctx.Unit;
            var options = new ConversionOptions(args.Tm, DrawingUnitFactor.MetersPerUnit(unit) ?? 0);
            var features = KmlReader.ReadFile(file);
            var plan = new ImportPlanner().FromKml(features, options);
            foreach (var issue in plan.Issues) ed.WriteMessage($"\nHPGeo {issue.Severity}: {issue.Message}");
            if (!plan.Success)
            {
                ed.WriteMessage($"\nHPGeo: FAILED — {plan.Errors.Count()} error(s), nothing drawn.\n");
                HPGeoLog.Warning($"-HPGEOIMPORT failed: {string.Join(" | ", plan.Errors.Select(e => e.Code))}");
                return;
            }
            var written = HPGeoImportCommand.Write(doc, plan);
            ed.WriteMessage($"\nHPGeo: OK — {written.PointCount} POINT, {written.PolylineCount} LWPOLYLINE on {written.Layer} from {Path.GetFileName(file)}\n");
        }
        catch (ArgumentException exception)
        {
            ed.WriteMessage($"\nHPGeo: {exception.Message}\n");
            HPGeoLog.Warning($"-HPGEOIMPORT refused: {exception.Message}");
        }
        catch (System.Exception exception)
        {
            ed.WriteMessage($"\nHPGeo: lỗi — {exception.Message}\n");
            HPGeoLog.Error("-HPGEOIMPORT failed", exception);
        }
    }
}
