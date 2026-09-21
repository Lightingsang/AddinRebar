using HPAutoCad.HPGeoLink.Model;
using HPAutoCad.HPGeoLink.Support;
using HPAutoCad.HPGeoLink.ViewModel;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Settings;
using HPAutoCad.Core.HPGeoLink.Units;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>
/// The dialog's view model without a window: first-run defaults, the two-meridian province that must be
/// chosen, manual meridian entry, unknown units, colour validation and an export through a fake shell.
/// </summary>
public sealed class GeoExportViewModelTests
{
    private static readonly SurveyPoint[] Samples =
    {
        new(1, "1", new PlanePoint(600125.887, 1231608.428)),
        new(2, "2", new PlanePoint(600124.894, 1231587.765)),
        new(3, "3", new PlanePoint(600130.542, 1231543.79)),
    };

    private sealed class FakeShell : IGeoExportShell
    {
        public string? SavePath { get; set; }
        public List<string> Opened { get; } = new();
        public List<string> Urls { get; } = new();

        public string? AskSavePath(string? initialDirectory, string suggestedFileName) => SavePath;
        public void OpenPath(string path) => Opened.Add(path);
        public string OpenInGoogleEarth(string kmzPath) { Opened.Add(kmzPath); return "đã mở trong Google Earth (fake)"; }
        public void OpenUrl(string url) => Urls.Add(url);
    }

    private static GeoExportViewModel Create(FakeShell? shell = null, DrawingUnit unit = DrawingUnit.Meters) =>
        new(Samples, Array.Empty<BoundaryPolyline>(), "Site plan", null, unit, shell ?? new FakeShell());

    [Fact]
    public void First_run_defaults_are_the_reference_tools()
    {
        var vm = Create();
        Assert.True(vm.Crs.UseCurrentCatalog);
        Assert.Equal(34, vm.Crs.Provinces.Count);
        Assert.Equal(CrsSelectionViewModel.DefaultProvince, vm.Crs.SelectedProvince?.Name);
        Assert.Equal(105.75, vm.Crs.SelectedMeridian?.Degrees);
        Assert.Equal("105.75", vm.Crs.CentralMeridianText);
        Assert.Contains("Bình Dương cũ", vm.Crs.MeridianNote);
        Assert.Equal("0.9999", vm.Crs.K0Text);
        Assert.Equal("500000", vm.Crs.FalseEastingText);
        Assert.True(vm.OutputBoth);
        Assert.Equal(KmlColor.DefaultPoint, vm.PointColor);
        Assert.Equal(KmlColor.DefaultLine, vm.LineColor);
        Assert.Equal("Site plan", vm.FileName);
        Assert.True(vm.CanExport);
        Assert.Equal(3, vm.Preview.Count);
        Assert.Contains("105°45′", vm.Status);
        Assert.StartsWith("11.13558", vm.Preview[0].Lat);
    }

    [Fact]
    public void Two_meridian_province_blocks_export_until_one_is_chosen()
    {
        var vm = Create();
        vm.Crs.SelectedProvince = vm.Crs.Provinces.Single(p => p.Name == "TP. Cần Thơ");
        Assert.Equal(2, vm.Crs.Meridians.Count);
        Assert.Equal("105°00′ — TP. Cần Thơ cũ + Hậu Giang cũ", vm.Crs.Meridians[0].Label);
        Assert.Equal("105°30′ — Sóc Trăng cũ", vm.Crs.Meridians[1].Label);
        Assert.Null(vm.Crs.SelectedMeridian);
        Assert.True(vm.Crs.NeedsMeridianChoice);
        Assert.Equal("", vm.Crs.CentralMeridianText);
        Assert.False(vm.CanExport);
        Assert.Contains("KINH TUYẾN TRỤC", vm.IssuesText);

        vm.Crs.SelectedMeridian = vm.Crs.Meridians[1];
        Assert.False(vm.Crs.NeedsMeridianChoice);
        Assert.Equal("105.5", vm.Crs.CentralMeridianText);
        Assert.True(vm.CanExport);
    }

    [Fact]
    public void Single_meridian_province_selects_it_automatically()
    {
        var vm = Create();
        vm.Crs.SelectedProvince = vm.Crs.Provinces.Single(p => p.Name == "TP. Hà Nội");
        Assert.Equal(105.0, vm.Crs.SelectedMeridian?.Degrees);
        Assert.Equal("", vm.Crs.MeridianNote);
        Assert.True(vm.CanExport);
    }

    [Fact]
    public void Manual_meridian_text_overrides_the_selection()
    {
        var vm = Create();
        vm.Crs.CentralMeridianText = "105-45";
        Assert.True(vm.CanExport);
        vm.Crs.CentralMeridianText = "abc";
        Assert.False(vm.CanExport);
        Assert.Contains("KINH TUYẾN TRỤC", vm.IssuesText);
    }

    [Fact]
    public void Legacy_catalogue_lists_63_provinces()
    {
        var vm = Create();
        vm.Crs.UseCurrentCatalog = false;
        Assert.Equal(63, vm.Crs.Provinces.Count);
        vm.Crs.SelectedProvince = vm.Crs.Provinces.Single(p => p.Name == "Bình Dương");
        Assert.Equal(105.75, vm.Crs.SelectedMeridian?.Degrees);
        Assert.True(vm.CanExport);
    }

    [Fact]
    public void Unknown_drawing_unit_must_be_chosen_first()
    {
        var vm = Create(unit: DrawingUnit.Unknown);
        Assert.False(vm.Crs.UnitFromDrawing);
        Assert.Null(vm.Crs.SelectedUnit);
        Assert.False(vm.CanExport);
        Assert.Contains("đơn vị", vm.IssuesText);
        vm.Crs.SelectedUnit = vm.Crs.Units.Single(u => u.Unit == DrawingUnit.Meters);
        Assert.True(vm.CanExport);
    }

    [Fact]
    public void Millimetre_drawing_converts_to_the_same_place()
    {
        var mm = Samples.Select(p => p with { DrawingXY = new PlanePoint(p.DrawingXY.Easting * 1000, p.DrawingXY.Northing * 1000) }).ToList();
        var vm = new GeoExportViewModel(mm, Array.Empty<BoundaryPolyline>(), "x", null, DrawingUnit.Millimeters, new FakeShell());
        Assert.True(vm.Crs.SelectedUnit?.FromDrawing);
        Assert.True(vm.CanExport);
        Assert.StartsWith("11.13558", vm.Preview[0].Lat);
        Assert.Equal("600125.887", vm.Preview[0].Easting);
    }

    [Fact]
    public void Invalid_colour_blocks_export()
    {
        var vm = Create();
        vm.PointColor = "yellow";
        Assert.False(vm.CanExport);
        Assert.Contains("aabbggrr", vm.IssuesText);
        vm.PointColor = "FF00FFFF";
        Assert.True(vm.CanExport);
    }

    [Fact]
    public void Export_writes_the_kmz_and_google_earth_opens_it()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hpgeo-vm-{Guid.NewGuid():N}.kmz");
        var shell = new FakeShell { SavePath = path };
        var vm = Create(shell);
        try
        {
            vm.OutputPoints = true;
            vm.ExportKmzCommand.Execute(null);
            Assert.Equal(Path.GetFullPath(path), vm.LastExportPath);
            Assert.True(File.Exists(path));
            Assert.StartsWith("Đã ghi", vm.Status);
            Assert.EndsWith("đã mở trong Google Earth (fake)", vm.Status);
            Assert.Equal(new[] { Path.GetFullPath(path) }, shell.Opened);
            var kml = KmzWriter.ReadKml(path);
            Assert.Contains("<name>Site plan</name>", kml);
            Assert.DoesNotContain("<Polygon>", kml);

            vm.OpenGoogleEarthCommand.Execute(null);
            Assert.Equal(new[] { Path.GetFullPath(path), Path.GetFullPath(path) }, shell.Opened);
            vm.OpenGoogleMapsCommand.Execute(null);
            Assert.StartsWith("https://www.google.com/maps?q=11.135", shell.Urls.Single()); // centroid of the three samples
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Cancelled_save_dialog_writes_nothing()
    {
        var shell = new FakeShell { SavePath = null };
        var vm = Create(shell);
        vm.ExportKmzCommand.Execute(null);
        Assert.Null(vm.LastExportPath);
    }

    [Fact]
    public void Kml_preview_toggles_and_follows_the_options()
    {
        var vm = Create();
        Assert.False(vm.ShowKmlPreview);
        vm.ToggleKmlPreviewCommand.Execute(null);
        Assert.True(vm.ShowKmlPreview);
        Assert.Contains("<Style id=\"ptVectorStyle\">", vm.KmlPreview);
        vm.LineColor = "ff00ff00";
        Assert.Contains("<color>ff00ff00</color><width>1.6</width>", vm.KmlPreview);
        vm.CloseCommand.Execute(null); // no subscriber: must not throw
    }

    [Fact]
    public void Close_raises_the_event_once()
    {
        var vm = Create();
        var closed = 0;
        vm.CloseRequested += () => closed++;
        vm.CloseCommand.Execute(null);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void Insert_image_hands_back_the_dialogs_zone_unit_and_the_selections_extent_then_closes()
    {
        var ring = new BoundaryPolyline("Boundary 1", new[] { new PlanePoint(600102.308, 1231385.196), new PlanePoint(600185.982, 1231422.961), new PlanePoint(600138.509, 1231379.42) }, true, "267");
        var vm = new GeoExportViewModel(Samples, new[] { ring }, "Site plan", null, DrawingUnit.Meters, new FakeShell(),
            new GeoSettings { CentralMeridianDeg = 105.75, ImageryResolutionMPerPx = 0.6, ImageryAreaRatio = 4 });
        Assert.Equal("0.6", vm.ImageResolutionText); // prefilled from the stored record
        Assert.Equal("4", vm.ImageAreaRatioText);
        var closed = 0;
        vm.CloseRequested += () => closed++;

        vm.ImageAreaRatioText = "10";
        vm.InsertImageCommand.Execute(null);
        Assert.Equal(1, closed);
        var choice = vm.ImageChoice!;
        Assert.Equal((105.75, 0.9999, DrawingUnit.Meters, 1.0, 0.6, 10.0, 3, 1), (choice.Tm.CentralMeridianDeg, choice.Tm.ScaleFactor, choice.Unit, choice.MetersPerUnit, choice.ResolutionMPerPx, choice.AreaRatio, choice.PointCount, choice.BoundaryCount));
        // The extent covers the points AND the ring's vertices (a survey of points alone still gets its imagery).
        Assert.Equal((600102.308, 1231379.42, 600185.982, 1231608.428), (choice.ExtentDrawingUnits.MinE, choice.ExtentDrawingUnits.MinN, choice.ExtentDrawingUnits.MaxE, choice.ExtentDrawingUnits.MaxN));
        // The margin makes the image ten times the extent's area: (W + 2m)(H + 2m) = 10·W·H.
        var w = 600185.982 - 600102.308; var h = 1231608.428 - 1231379.42; var m = choice.MarginM;
        Assert.Equal(10.0, (w + 2 * m) * (h + 2 * m) / (w * h), 6);
        var settings = vm.ToSettings();
        Assert.Equal(("esri", 0.6, 10.0, m), (settings.ImageryProvider, settings.ImageryResolutionMPerPx, settings.ImageryAreaRatio, settings.ImageryMarginM));
    }

    [Fact]
    public void Insert_image_refuses_a_bad_resolution_or_an_invalid_zone_without_closing()
    {
        var vm = Create();
        var closed = 0;
        vm.CloseRequested += () => closed++;
        vm.ImageResolutionText = "0";
        vm.InsertImageCommand.Execute(null);
        Assert.Null(vm.ImageChoice);
        Assert.Contains("độ phân giải", vm.Status);
        vm.ImageResolutionText = "0.3";
        vm.ImageAreaRatioText = "0.5"; // below 1 would cut into the plot
        vm.InsertImageCommand.Execute(null);
        Assert.Null(vm.ImageChoice);
        vm.ImageAreaRatioText = "10";

        vm.ImageResolutionText = "0.3";
        vm.Crs.CentralMeridianText = "abc"; // no zone → the conversion fails → no imagery either
        vm.InsertImageCommand.Execute(null);
        Assert.Null(vm.ImageChoice);
        Assert.Contains("hệ toạ độ", vm.Status);
        Assert.Equal(0, closed);
    }
}
