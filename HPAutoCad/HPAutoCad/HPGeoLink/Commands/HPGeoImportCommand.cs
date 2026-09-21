using Autodesk.AutoCAD.ApplicationServices;
using HPAutoCad.Core.HPGeoLink.Import;
using HPAutoCad.HPGeoLink.Cad;
using HPAutoCad.HPGeoLink.Support;
using HPAutoCad.HPGeoLink.View;
using HPAutoCad.HPGeoLink.ViewModel;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.HPGeoLink.Commands;

/// <summary>
/// <c>HPGEOIMPORT</c> (registered by the loader): the reverse direction through the dialog — a KML/KMZ or
/// pasted coordinates become POINT and LWPOLYLINE entities on HPGEO-IMPORT, written in one transaction after
/// the user confirms, so one Undo removes them all.
/// </summary>
internal static class HPGeoImportCommand
{
    public static void Run()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        var ed = doc.Editor;
        try
        {
            var ctx = DrawingContext.Read(doc);
            var stored = DocumentSettingsStore.Read(doc.Database) ?? UserSettingsStore.Load();
            var viewModel = new GeoImportViewModel(ctx.Unit, new Shell(), stored);
            var window = new GeoImportWindow(viewModel);
            AcadApp.ShowModalWindow(AcadApp.MainWindow.Handle, window, false);
            if (viewModel.Result is null) return;
            var written = Write(doc, viewModel.Result);
            ed.WriteMessage($"\nHPGeo: đã vẽ {written.PointCount} POINT, {written.PolylineCount} LWPOLYLINE lên layer {written.Layer}.\n");
            var settings = viewModel.ToSettings(stored);
            DocumentSettingsStore.Write(doc.Database, settings);
            UserSettingsStore.Save(settings);
        }
        catch (System.Exception exception)
        {
            ed.WriteMessage($"\nHPGeo: lỗi — {exception.Message}\n");
            HPGeoLog.Error("HPGEOIMPORT failed", exception);
        }
    }

    internal static DrawingWriteResult Write(Document doc, ImportPlan plan)
    {
        using var docLock = doc.LockDocument();
        var written = DrawingWriter.Write(doc.Database, plan);
        HPGeoLog.Information($"HPGEOIMPORT wrote {written.PointCount} points, {written.PolylineCount} polylines on {written.Layer}, KTT {plan.Options.Tm.CentralMeridianDeg.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        return written;
    }

    private sealed class Shell : IGeoImportShell
    {
        public string? AskOpenPath()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn file KML/KMZ",
                Filter = "Google Earth (*.kml;*.kmz)|*.kml;*.kmz|Tất cả (*.*)|*.*",
                CheckFileExists = true,
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
