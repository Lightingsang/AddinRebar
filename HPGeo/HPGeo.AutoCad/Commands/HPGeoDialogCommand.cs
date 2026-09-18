using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPGeo.AutoCad.Cad;
using HPGeo.AutoCad.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPGeo.AutoCad.Commands;

/// <summary>
/// <c>HPGEO</c> (registered by the loader): pick the survey points and boundaries first — the pick-first set
/// counts, Enter with nothing picked takes all of model space, Escape cancels — then the modal dialog. The
/// dialog never touches the drawing: reading happens here inside the command's transaction, and the dialog
/// only converts and writes a KMZ. After a successful export the zone is stored in the drawing's named object
/// dictionary (one undoable change under the command's own undo marker).
/// </summary>
internal static class HPGeoDialogCommand
{
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

            var stored = DocumentSettingsStore.Read(doc.Database) ?? UserSettingsStore.Load();
            var viewModel = new GeoExportViewModel(read.Points, read.Boundaries, ctx.BaseName, ctx.DirectoryPath, ctx.Unit, new Shell(), stored);
            var window = new GeoExportWindow(viewModel) { MapEnabled = UserSettingsStore.Load()?.MapEnabled ?? true };
            // persistSizeAndPosition false: the XAML size fits the table + map; AutoCAD would otherwise replay the first run's size.
            AcadApp.ShowModalWindow(AcadApp.MainWindow.Handle, window, false);
            if (viewModel.LastExportPath is null) return;
            ed.WriteMessage($"\nHPGeo: đã ghi {viewModel.LastExportPath}\n");
            var settings = viewModel.ToSettings();
            DocumentSettingsStore.Write(doc.Database, settings);
            UserSettingsStore.Save(settings with { MapEnabled = window.MapEnabled });
        }
        catch (System.Exception exception)
        {
            ed.WriteMessage($"\nHPGeo: lỗi — {exception.Message}\n");
            HPGeoLog.Error("HPGEO failed", exception);
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

        public void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
