using HPGeo.AutoCad.UI;
using HPGeo.Core.Conversion;
using HPGeo.Core.Kml;
using HPGeo.Core.Model;
using HPGeo.Core.Projection;
using HPGeo.Core.Settings;
using HPGeo.Core.Units;
using Xunit;

namespace HPGeo.Tests;

/// <summary>The import dialog's view model and the stored-settings precedence of both dialogs.</summary>
public sealed class GeoImportViewModelTests
{
    private sealed class FakeShell : IGeoImportShell, IGeoExportShell
    {
        public string? OpenPath { get; set; }
        public string? AskOpenPath() => OpenPath;
        public string? AskSavePath(string? initialDirectory, string suggestedFileName) => null;
        void IGeoExportShell.OpenPath(string path) { }
        public void OpenUrl(string url) { }
    }

    private static string WriteSampleKmz()
    {
        var points = new[] { new SurveyPoint(1, "1", new PlanePoint(600125.887, 1231608.428)), new SurveyPoint(2, "2", new PlanePoint(600124.894, 1231587.765)), new SurveyPoint(3, "3", new PlanePoint(600130.542, 1231543.79)) };
        var result = new Vn2000Converter().Convert(points, Array.Empty<BoundaryPolyline>(), new ConversionOptions(TmParameters.Tm3(105.75), 1.0));
        var path = Path.Combine(Path.GetTempPath(), $"hpgeo-import-{Guid.NewGuid():N}.kmz");
        KmzWriter.WriteFile(KmlDocumentBuilder.Build(result, new KmlExportOptions("S")).Kml, path);
        return path;
    }

    [Fact]
    public void Kmz_file_plans_points_and_the_boundary_from_points()
    {
        var path = WriteSampleKmz();
        try
        {
            var vm = new GeoImportViewModel(DrawingUnit.Meters, new FakeShell { OpenPath = path });
            Assert.False(vm.CanDraw);
            Assert.Equal("Chưa có dữ liệu.", vm.Status);
            vm.BrowseFileCommand.Execute(null);
            Assert.True(vm.SourceIsFile);
            Assert.Equal(path, vm.FilePath);
            Assert.True(vm.CanDraw, vm.IssuesText);
            Assert.Equal(3, vm.CurrentPlan!.Points.Count);
            Assert.Single(vm.CurrentPlan.Polylines); // the "Boundary" ring the exporter built from the points
            Assert.Contains("3 điểm, 0 đường, 1 vùng", vm.IssuesText);
            Assert.InRange(vm.CurrentPlan.Points[0].DrawingXY.Easting, 600125.886, 600125.888);

            var closed = 0;
            vm.CloseRequested += () => closed++;
            vm.DrawCommand.Execute(null);
            Assert.Same(vm.CurrentPlan, vm.Result);
            Assert.Equal(1, closed);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Missing_file_and_wrong_zone_block_drawing()
    {
        var vm = new GeoImportViewModel(DrawingUnit.Meters, new FakeShell());
        vm.FilePath = Path.Combine(Path.GetTempPath(), "does-not-exist.kml");
        Assert.False(vm.CanDraw);
        Assert.Contains("Không tìm thấy file", vm.IssuesText);
    }

    [Fact]
    public void Pasted_wgs84_and_vn2000_text_plan_correctly()
    {
        var vm = new GeoImportViewModel(DrawingUnit.Millimeters, new FakeShell());
        vm.SourceIsFile = false;
        vm.PastedText = "11.1355893, 106.6684375\n11.1354026, 106.6684279";
        Assert.True(vm.CanDraw, vm.IssuesText);
        Assert.Equal(2, vm.Preview.Count);
        Assert.InRange(vm.CurrentPlan!.Points[0].DrawingXY.Easting, 600125887 - 10, 600125887 + 10); // mm drawing
        Assert.StartsWith("600125.88", vm.Preview[0].Easting); // 7-decimal lat/lon carries ~1 cm

        vm.JoinAsPolyline = true;
        vm.CloseRing = true;
        Assert.False(vm.CanDraw); // 2 points cannot close a ring
        vm.CloseRing = false;
        Assert.True(vm.CanDraw);
        Assert.Single(vm.CurrentPlan!.Polylines);

        vm.PastedIsWgs84 = false;
        vm.PairOrderIndex = 2; // cadastral X,Y
        vm.PastedText = "1231608.428 600125.887";
        Assert.Single(vm.CurrentPlan!.Points);
        Assert.Equal(600125.887, vm.CurrentPlan.Points[0].GridM.Easting, 6);
    }

    [Fact]
    public void Stored_settings_win_over_first_run_defaults_and_round_trip()
    {
        var stored = new GeoSettings
        {
            UseCurrentCatalog = false, ProvinceName = "Bình Dương", CentralMeridianDeg = 105.75, ScaleFactor = 0.9999, FalseEasting = 500000, FalseNorthing = 0,
            Unit = DrawingUnit.Meters, Output = KmlOutput.Points, PointColor = "ff00ff00", LineColor = "ffff0000",
        };
        var points = new[] { new SurveyPoint(1, "1", new PlanePoint(600125.887, 1231608.428)) };
        var vm = new GeoExportViewModel(points, Array.Empty<BoundaryPolyline>(), "d", null, DrawingUnit.Unknown, new FakeShell(), stored);
        Assert.False(vm.Crs.UseCurrentCatalog);
        Assert.Equal("Bình Dương", vm.Crs.SelectedProvince?.Name);
        Assert.Equal("105.75", vm.Crs.CentralMeridianText);
        Assert.Equal(DrawingUnit.Meters, vm.Crs.SelectedUnit?.Unit); // INSUNITS unknown → the stored unit applies
        Assert.True(vm.OutputPoints);
        Assert.Equal("ff00ff00", vm.PointColor);
        Assert.True(vm.CanExport);

        var back = vm.ToSettings();
        Assert.Equal(stored with { SavedBy = null, ExportDirectory = null }, back);

        var import = new GeoImportViewModel(DrawingUnit.Meters, new FakeShell(), stored);
        Assert.Equal("Bình Dương", import.Crs.SelectedProvince?.Name);
        Assert.Null(import.ToSettings(stored).Unit); // INSUNITS known → no unit override stored
        Assert.Equal(KmlOutput.Points, import.ToSettings(stored).Output); // merged, not reset
    }

    [Fact]
    public void Stored_unit_is_ignored_when_the_drawing_has_one()
    {
        var stored = new GeoSettings { Unit = DrawingUnit.Kilometers, CentralMeridianDeg = 105.5, ProvinceName = "Hưng Yên" };
        var vm = new GeoExportViewModel(Array.Empty<SurveyPoint>(), Array.Empty<BoundaryPolyline>(), "d", null, DrawingUnit.Meters, new FakeShell(), stored);
        Assert.Equal(DrawingUnit.Meters, vm.Crs.SelectedUnit?.Unit);
        Assert.Equal("Hưng Yên", vm.Crs.SelectedProvince?.Name);
        Assert.Equal(105.5, vm.Crs.SelectedMeridian?.Degrees);
    }
}
