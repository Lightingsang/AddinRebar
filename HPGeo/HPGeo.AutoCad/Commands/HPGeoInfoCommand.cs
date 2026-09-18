using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPGeo.AutoCad.Cad;
using HPGeo.Core.Catalog;
using HPGeo.Core.Model;
using HPGeo.Core.Projection;

namespace HPGeo.AutoCad.Commands;

/// <summary>
/// <c>HPGEOINFO</c> (registered by the loader): the diagnostic printout — version, product, drawing unit and
/// factor, what the drawing holds, a trial conversion of the first point, the log folder. Read-only; the
/// acceptance harness greps it.
/// </summary>
internal static class HPGeoInfoCommand
{
    private const double TrialCentralMeridian = 105.75; // TP. Hồ Chí Minh's default zone, the first-run default

    public static void Run()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        var ed = doc.Editor;
        var ci = CultureInfo.InvariantCulture;
        try
        {
            var ctx = DrawingContext.Read(doc);
            ed.WriteMessage($"\nHPGeo {Entry.Version} · {SystemVariable("PRODUCT")} {SystemVariable("ACADVER")}");
            ed.WriteMessage($"\n  Drawing: {ctx.DocumentName}  folder: {ctx.DirectoryPath ?? "(unsaved)"}");
            ed.WriteMessage($"\n  INSUNITS: {ctx.InsUnitsCode} = {ctx.UnitLabel}  → metres per unit: {(ctx.MetersPerUnit?.ToString(ci) ?? "unknown — the dialog will ask")}");
            var stored = DocumentSettingsStore.Read(doc.Database);
            ed.WriteMessage(stored is null
                ? "\n  Stored VN-2000 settings: (none in this drawing)"
                : $"\n  Stored VN-2000 settings: KTT {(stored.HasCentralMeridian ? stored.CentralMeridianDeg!.Value.ToString(ci) : "?")} ({(stored.HasCentralMeridian ? CentralMeridian.Format(stored.CentralMeridianDeg!.Value) : "-")}), province {stored.ProvinceName ?? "-"}, catalog {(stored.UseCurrentCatalog ? "current" : "legacy")}, k0 {stored.ScaleFactor?.ToString(ci) ?? "-"}, FE {stored.FalseEasting?.ToString(ci) ?? "-"}, FN {stored.FalseNorthing?.ToString(ci) ?? "-"}, unit {stored.Unit?.ToString() ?? "(drawing)"}, output {stored.Output}, saved by {stored.SavedBy ?? "-"}");
            var userSettings = UserSettingsStore.Load();
            ed.WriteMessage(userSettings is null ? "\n  User settings: (none)" : $"\n  User settings ({UserSettingsStore.Path}): KTT {userSettings.CentralMeridianDeg?.ToString(ci) ?? "-"}, province {userSettings.ProvinceName ?? "-"}, export dir {userSettings.ExportDirectory ?? "-"}");
            ed.WriteMessage($"\n  Catalogue: {ProvinceCatalog.Default.Current.Count} current + {ProvinceCatalog.Default.Legacy.Count} legacy provinces, {ProvinceCatalog.Default.CentralMeridians.Count} central meridians ({ProvinceCatalog.Default.Source})");

            using var tr = doc.Database.TransactionManager.StartTransaction();
            var read = DrawingReader.Read(tr, DrawingReader.ModelSpaceIds(tr, doc.Database), ctx.ChordToleranceDrawingUnits);
            var closed = read.Boundaries.Count(b => b.Closed);
            ed.WriteMessage($"\n  Model space: {read.Points.Count} POINT, {read.Boundaries.Count} LWPOLYLINE ({closed} closed)");
            if (read.SkippedByType.Count > 0)
                ed.WriteMessage($"\n  Other entities (ignored): {string.Join(", ", read.SkippedByType.Select(kv => $"{kv.Key} ×{kv.Value}"))}");

            if (read.Points.Count > 0 && ctx.MetersPerUnit is { } factor)
            {
                var first = read.Points[0];
                var grid = new PlanePoint(first.DrawingXY.Easting * factor, first.DrawingXY.Northing * factor);
                var wgs = Vn2000Wgs84Transform.Default.ToWgs84(grid, TmParameters.Tm3(TrialCentralMeridian));
                ed.WriteMessage($"\n  Trial (KTT {CentralMeridian.Format(TrialCentralMeridian)}, default): point 1 E={grid.Easting.ToString("F3", ci)} N={grid.Northing.ToString("F3", ci)} → Lat {wgs.LatDeg.ToString("F7", ci)}, Lon {wgs.LonDeg.ToString("F7", ci)}");
            }
            tr.Commit();

            ed.WriteMessage($"\n  Log: {HPGeoLog.LogDirectory}\n");
            HPGeoLog.Information($"HPGEOINFO on {ctx.DocumentName}: {read.Points.Count} points, {read.Boundaries.Count} polylines, INSUNITS {ctx.InsUnitsCode}");
        }
        catch (System.Exception exception)
        {
            ed.WriteMessage($"\nHPGEOINFO failed: {exception.Message}\n");
            HPGeoLog.Error("HPGEOINFO failed", exception);
        }
    }

    private static string SystemVariable(string name)
    {
        try { return Application.GetSystemVariable(name)?.ToString() ?? ""; }
        catch (System.Exception) { return ""; }
    }
}
