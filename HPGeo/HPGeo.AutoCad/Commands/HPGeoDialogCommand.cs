using System.Diagnostics;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPGeo.AutoCad.Cad;
using HPGeo.AutoCad.Imagery;
using HPGeo.AutoCad.UI;
using HPGeo.Core.Catalog;
using HPGeo.Core.Imagery;
using HPGeo.Core.Settings;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPGeo.AutoCad.Commands;

/// <summary>
/// <c>HPGEO</c> (registered by the loader): pick the survey points and boundaries first — the pick-first set
/// counts, Enter with nothing picked takes all of model space, Escape cancels — then the modal dialog. The
/// dialog never touches the drawing: reading happens here inside the command's transaction, and the dialog
/// only converts and writes a KMZ. After a successful export the zone is stored in the drawing's named object
/// dictionary (one undoable change under the command's own undo marker). The dialog's "Chèn ảnh vệ tinh vào CAD"
/// button closes it with a <see cref="GeoImageChoice"/>; the imagery is then fetched, warped and inserted here,
/// on the command line, exactly as <c>-HPGEOIMAGE</c> does (same pipeline, same undo group).
/// </summary>
internal static class HPGeoDialogCommand
{
    private const string Name = "HPGEO";

    public static void Run()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        var ed = doc.Editor;
        try
        {
            var ctx = DrawingContext.Read(doc);
            DrawingReadResult read;
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var ids = Pick(ed, tr, doc.Database);
                if (ids is null) return;
                read = DrawingReader.Read(tr, ids, ctx.ChordToleranceDrawingUnits);
                tr.Commit();
            }
            if (read.Points.Count == 0 && read.Boundaries.Count == 0)
            {
                ed.WriteMessage("\nHPGeo: không có POINT hay LWPOLYLINE nào trong lựa chọn.\n");
                return;
            }
            if (read.SkippedByType.Count > 0)
                ed.WriteMessage($"\nHPGeo: bỏ qua {string.Join(", ", read.SkippedByType.Select(kv => $"{kv.Key} ×{kv.Value}"))} (chỉ POINT và LWPOLYLINE được chuyển).");

            var drawingSettings = DocumentSettingsStore.Read(doc.Database);
            var userSettings = UserSettingsStore.Load();
            var viewModel = new GeoExportViewModel(read.Points, read.Boundaries, ctx.BaseName, ctx.DirectoryPath, ctx.Unit, new Shell(), drawingSettings ?? userSettings);
            var window = new GeoExportWindow(viewModel) { MapEnabled = userSettings?.MapEnabled ?? true };
            // persistSizeAndPosition false: the XAML size fits the table + map; AutoCAD would otherwise replay the first run's size.
            AcadApp.ShowModalWindow(AcadApp.MainWindow.Handle, window, false);
            if (viewModel.LastExportPath is not null)
            {
                ed.WriteMessage($"\nHPGeo: đã ghi {viewModel.LastExportPath}\n");
                var settings = viewModel.ToSettings();
                DocumentSettingsStore.Write(doc.Database, settings);
                UserSettingsStore.Save(settings with { MapEnabled = window.MapEnabled });
            }
            if (viewModel.ImageChoice is { } choice)
                InsertImagery(doc, ed, ctx, viewModel, choice, drawingSettings, userSettings);
        }
        catch (System.Exception exception)
        {
            ed.WriteMessage($"\nHPGeo: lỗi — {exception.Message}\n");
            HPGeoLog.Error("HPGEO failed", exception);
        }
    }

    /// <summary>The dialog's imagery button, honoured after it closed: the selection's extent + margin, the dialog's zone and unit.</summary>
    private static void InsertImagery(Document doc, Editor ed, DrawingContext ctx, GeoExportViewModel viewModel, GeoImageChoice choice, GeoSettings? stored, GeoSettings? user)
    {
        var ci = CultureInfo.InvariantCulture;
        if (!(choice.MetersPerUnit > 0))
        {
            ImageryConsole.Refuse(ed, Name, "UNKNOWN_UNIT", "Chưa chọn đơn vị bản vẽ.");
            return;
        }
        if (RasterInserter.CheckLayer(doc.Database) is { } layerProblem)
        {
            ImageryConsole.Refuse(ed, Name, "LAYER_LOCKED", layerProblem);
            return;
        }
        var f = choice.MetersPerUnit;
        var e = choice.ExtentDrawingUnits;
        var box = new GridBoundingBox(e.MinE * f, e.MinN * f, e.MaxE * f, e.MaxN * f);
        var provider = ImageryProviders.Default;
        var (imagePath, unsaved) = RasterInserter.ImagePathFor(ctx, $"{provider.Id}-{DateTime.Now.ToString("yyyyMMdd-HHmmss", ci)}");
        ed.WriteMessage($"\nHPGeo: ảnh vệ tinh phủ {choice.PointCount} POINT + {choice.BoundaryCount} LWPOLYLINE, E {box.MinE.ToString("F3", ci)}–{box.MaxE.ToString("F3", ci)}, N {box.MinN.ToString("F3", ci)}–{box.MaxN.ToString("F3", ci)} m, vùng ×{choice.AreaRatio.ToString("0.#", ci)} → biên {choice.MarginM.ToString("0.#", ci)} m, KTT {CentralMeridian.Format(choice.Tm.CentralMeridianDeg)}");
        if (unsaved) ed.WriteMessage($"\nHPGeo Warning: bản vẽ chưa lưu — ảnh ghi vào {RasterInserter.UnsavedImageRoot} (đường dẫn tuyệt đối; lưu bản vẽ rồi chạy lại nếu muốn ảnh nằm cạnh DWG).");

        // The dialog's resolution is a target: a wide view that would overflow the caps is taken at the finest zoom that fits.
        var request = new ImageryRequest(box, choice.Tm, f, choice.MarginM, choice.ResolutionMPerPx, null, provider, imagePath, FitToCaps: true);
        var outcome = ImageryPipeline.Run(doc.Database, request, message => ed.WriteMessage($"\nHPGeo: {message}"), () => HostApplicationServices.Current.UserBreak());
        ImageryConsole.Report(ed, Name, outcome);
        if (outcome.Success)
        {
            var chosenUnit = viewModel.Crs.UnitFromDrawing ? null : viewModel.Crs.SelectedUnit?.Unit;
            ImageryConsole.Remember(doc, stored, user, choice.Tm, chosenUnit, provider.Id, choice.ResolutionMPerPx, choice.MarginM,
                viewModel.Crs.UseCurrentCatalog, viewModel.Crs.SelectedProvince?.Name, choice.AreaRatio);
        }
    }

    /// <summary>Pick-first set, else a prompt; Enter with nothing picked → all of model space; Escape → null (cancel).</summary>
    private static IEnumerable<ObjectId>? Pick(Editor ed, Transaction tr, Database db)
    {
        var filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "POINT,LWPOLYLINE") });
        var implied = ed.SelectImplied();
        if (implied.Status == PromptStatus.OK && implied.Value.Count > 0)
        {
            ed.SetImpliedSelection(Array.Empty<ObjectId>());
            return implied.Value.GetObjectIds().Where(id => Matches(tr, id));
        }
        var options = new PromptSelectionOptions
        {
            MessageForAdding = "\nChọn POINT / LWPOLYLINE VN-2000 (Enter = toàn bộ model space)",
            AllowDuplicates = false,
        };
        var picked = ed.GetSelection(options, filter);
        if (picked.Status == PromptStatus.OK && picked.Value.Count > 0) return picked.Value.GetObjectIds();
        if (picked.Status == PromptStatus.Cancel) return null; // Escape: the user backs out, nothing is converted
        ed.WriteMessage("\nHPGeo: không chọn gì — dùng toàn bộ model space.");
        return DrawingReader.ModelSpaceIds(tr, db, e => e is DBPoint or Polyline).ToList();
    }

    private static bool Matches(Transaction tr, ObjectId id) => tr.GetObject(id, OpenMode.ForRead, false) is DBPoint or Polyline;

    private sealed class Shell : IGeoExportShell
    {
        public string? AskSavePath(string? initialDirectory, string suggestedFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Xuất KMZ",
                Filter = "Google Earth KMZ (*.kmz)|*.kmz",
                DefaultExt = ".kmz",
                AddExtension = true,
                FileName = suggestedFileName,
                InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                OverwritePrompt = true,
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void OpenPath(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

        public string OpenInGoogleEarth(string kmzPath) => GoogleEarthLauncher.Open(kmzPath);

        public void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
