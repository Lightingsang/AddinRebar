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
    public void Hex_colour_is_accepted_and_synced_to_map_data_and_settings()
    {
        var vm = Create();
        vm.PointColor = "#FF69B4"; // Hong
        vm.LineColor = "#00FF00";  // Xanh la
        Assert.True(vm.CanExport);
        Assert.Contains("\"pointColor\":\"#FF69B4\"", vm.MapDataJson);
        Assert.Contains("\"lineColor\":\"#00FF00\"", vm.MapDataJson);

        var settings = vm.ToSettings();
        Assert.Equal("ffb469ff", settings.PointColor);
        Assert.Equal("ff00ff00", settings.LineColor);
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
        Assert.Contains("<color>ff00ff00</color><width>3.5</width>", vm.KmlPreview);
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

    [Fact]
    public void Preview_rows_include_boundary_vertices_with_index_and_item_type()
    {
        var ring = new BoundaryPolyline("Ranh thửa", new[]
        {
            new PlanePoint(600102.308, 1231385.196),
            new PlanePoint(600185.982, 1231422.961),
            new PlanePoint(600138.509, 1231379.42),
        }, true, "101");

        var vm = new GeoExportViewModel(Samples, new[] { ring }, "Site plan", null, DrawingUnit.Meters, new FakeShell());

        // 3 survey points + 3 boundary vertices = 6 preview rows
        Assert.Equal(6, vm.Preview.Count);

        // Point checks
        Assert.Equal(1, vm.Preview[0].Index);
        Assert.Equal("1", vm.Preview[0].Label);
        Assert.Equal("Điểm", vm.Preview[0].ItemType);

        Assert.Equal(3, vm.Preview[2].Index);
        Assert.Equal("3", vm.Preview[2].Label);
        Assert.Equal("Điểm", vm.Preview[2].ItemType);

        // Boundary vertex checks (single boundary -> P1, P2, P3)
        Assert.Equal(4, vm.Preview[3].Index);
        Assert.Equal("P1", vm.Preview[3].Label);
        Assert.Equal("Đỉnh ranh", vm.Preview[3].ItemType);
        Assert.Equal("600102.308", vm.Preview[3].Easting);
        Assert.Contains("Ranh thửa", vm.Preview[3].Note);

        Assert.Equal(5, vm.Preview[4].Index);
        Assert.Equal("P2", vm.Preview[4].Label);
        Assert.Equal("Đỉnh ranh", vm.Preview[4].ItemType);

        Assert.Equal(6, vm.Preview[5].Index);
        Assert.Equal("P3", vm.Preview[5].Label);
        Assert.Equal("Đỉnh ranh", vm.Preview[5].ItemType);
    }

    [Fact]
    public void UpdateObjects_refreshes_summary_and_recomputes_preview()
    {
        var vm = Create();
        Assert.Equal(3, vm.Preview.Count);
        Assert.Contains("3 POINT", vm.SourceSummary);

        var newPoints = new[] { new SurveyPoint(1, "P1", new PlanePoint(600000, 1200000)) };
        var newRing = new BoundaryPolyline("Ranh A", new[]
        {
            new PlanePoint(600000, 1200000),
            new PlanePoint(600100, 1200000)
        }, false);

        vm.UpdateObjects(newPoints, new[] { newRing }, "NewPlan");
        Assert.Equal("NewPlan", vm.FileName);
        Assert.Contains("1 POINT", vm.SourceSummary);
        Assert.Contains("1 LWPOLYLINE", vm.SourceSummary);
        // 1 point + 2 vertices = 3 preview rows
        Assert.Equal(3, vm.Preview.Count);
        Assert.Equal("P1", vm.Preview[0].Label);
        Assert.Equal("P1", vm.Preview[1].Label);
        Assert.Equal("P2", vm.Preview[2].Label);
    }

    [Fact]
    public void Boundary_vertex_export_options_defaults_and_selection_work()
    {
        var vm = Create();
        Assert.True(vm.ExportBoundaryVertices);
        Assert.Equal(4, vm.MarkerStyles.Count);
        Assert.Equal(3, vm.PopupTemplates.Count);
        Assert.Equal(BoundaryMarkerStyle.Triangle, vm.SelectedMarkerStyleItem.Style);
        Assert.Equal(BoundaryPopupTemplate.Cadastral, vm.SelectedPopupTemplateItem.Template);

        vm.SelectedMarkerStyleItem = vm.MarkerStyles.Single(m => m.Style == BoundaryMarkerStyle.Pushpin);
        vm.SelectedPopupTemplateItem = vm.PopupTemplates.Single(t => t.Template == BoundaryPopupTemplate.Technical);
        vm.ExportBoundaryVertices = false;

        var settings = vm.ToSettings();
        Assert.False(settings.ExportBoundaryVertices);
        Assert.Equal(BoundaryMarkerStyle.Pushpin, settings.BoundaryMarkerStyle);
        Assert.Equal(BoundaryPopupTemplate.Technical, settings.BoundaryPopupTemplate);
    }

    [Fact]
    public void Boundary_vertex_export_options_roundtrip_through_settings()
    {
        var vm = Create();
        var customSettings = new GeoSettings
        {
            ExportBoundaryVertices = true,
            BoundaryMarkerStyle = BoundaryMarkerStyle.Circle,
            BoundaryPopupTemplate = BoundaryPopupTemplate.Simple,
        };

        vm.ApplySettings(customSettings);
        Assert.True(vm.ExportBoundaryVertices);
        Assert.Equal(BoundaryMarkerStyle.Circle, vm.SelectedMarkerStyleItem.Style);
        Assert.Equal(BoundaryPopupTemplate.Simple, vm.SelectedPopupTemplateItem.Template);

        var exportedSettings = vm.ToSettings();
        Assert.True(exportedSettings.ExportBoundaryVertices);
        Assert.Equal(BoundaryMarkerStyle.Circle, exportedSettings.BoundaryMarkerStyle);
        Assert.Equal(BoundaryPopupTemplate.Simple, exportedSettings.BoundaryPopupTemplate);
    }

    [Fact]
    public void Export_with_real_autocad_RanhDat_boundary_succeeds()
    {
        var vertices = new[]
        {
            new PlanePoint(598355.642, 1232218.801),
            new PlanePoint(598230.823, 1232219.709),
            new PlanePoint(598222.492, 1232216.017),
            new PlanePoint(598139.273, 1232122.880),
            new PlanePoint(598128.517, 1232095.101),
            new PlanePoint(598128.462, 1232089.451),
            new PlanePoint(598133.414, 1232084.415),
            new PlanePoint(598354.652, 1232082.805),
        };
        var ranhDat = new BoundaryPolyline("RanhDat", vertices, true, "2B61C8");
        var shell = new FakeShell();
        var vm = new GeoExportViewModel(
            Array.Empty<SurveyPoint>(),
            new[] { ranhDat },
            "THCPHCS2-HPC-TDVN2000-XX-XX-DR-0001",
            @"F:\1-CONG VIEC\03-HPCDE\12-TRUONG TIEU HOC CHANH PHU HOA CO SO 2\01_WIP\01_ARC\02_Consumed-Data",
            DrawingUnit.Meters,
            shell);

        Assert.True(vm.CanExport);
        Assert.Equal(8, vm.Preview.Count);
        Assert.Equal("P1", vm.Preview[0].Label);
        Assert.Equal("P8", vm.Preview[7].Label);
        Assert.Equal("598355.642", vm.Preview[0].Easting);
        Assert.Equal("1232218.801", vm.Preview[0].Northing);
        Assert.Equal(1, vm.Preview[0].Index);
        Assert.Equal(8, vm.Preview[7].Index);

        var tempKmz = Path.Combine(Path.GetTempPath(), $"ranhdat_test_{Guid.NewGuid():N}.kmz");
        shell.SavePath = tempKmz;
        vm.ExportKmzCommand.Execute(null);

        Assert.True(File.Exists(tempKmz));
        Assert.Contains(tempKmz, shell.Opened);
        File.Delete(tempKmz);
    }

    [Fact]
    public void MapDataJson_includes_boundary_vertices_marker_style_and_popup_html()
    {
        var vertices = new[]
        {
            new PlanePoint(598355.642, 1232218.801),
            new PlanePoint(598230.823, 1232219.709),
            new PlanePoint(598222.492, 1232216.017),
        };
        var ranhDat = new BoundaryPolyline("RanhDat", vertices, true, "2B61C8");
        var vm = new GeoExportViewModel(
            Array.Empty<SurveyPoint>(),
            new[] { ranhDat },
            "BoundaryTest",
            null,
            DrawingUnit.Meters,
            new FakeShell());

        Assert.True(vm.ExportBoundaryVertices);
        Assert.Contains("\"exportBoundaryVertices\":true", vm.MapDataJson);
        Assert.Contains("\"markerStyle\":\"triangle\"", vm.MapDataJson);
        Assert.Contains("\"label\":\"P1\"", vm.MapDataJson);
        Assert.Contains("\"easting\":598355.642", vm.MapDataJson);
        Assert.Contains("\"northing\":1232218.801", vm.MapDataJson);
        Assert.Contains("\"segmentLength\":", vm.MapDataJson);
        Assert.Contains("\"nextLabel\":\"P2\"", vm.MapDataJson);
        Assert.Contains("\"popupHtml\":", vm.MapDataJson);
        // Default cadastral template includes table and headers
        Assert.Contains("THÔNG TIN ĐỈNH RANH: P1", vm.MapDataJson);

        // Toggle marker styles
        vm.SelectedMarkerStyleItem = vm.MarkerStyles.Single(m => m.Style == BoundaryMarkerStyle.Pushpin);
        Assert.Contains("\"markerStyle\":\"pushpin\"", vm.MapDataJson);

        vm.SelectedMarkerStyleItem = vm.MarkerStyles.Single(m => m.Style == BoundaryMarkerStyle.Circle);
        Assert.Contains("\"markerStyle\":\"circle\"", vm.MapDataJson);

        vm.SelectedMarkerStyleItem = vm.MarkerStyles.Single(m => m.Style == BoundaryMarkerStyle.LabelOnly);
        Assert.Contains("\"markerStyle\":\"labelOnly\"", vm.MapDataJson);

        // Toggle popup templates
        vm.SelectedPopupTemplateItem = vm.PopupTemplates.Single(t => t.Template == BoundaryPopupTemplate.Technical);
        Assert.Contains("MỐC RANH: P1", vm.MapDataJson);

        vm.SelectedPopupTemplateItem = vm.PopupTemplates.Single(t => t.Template == BoundaryPopupTemplate.Simple);
        Assert.Contains("Đỉnh ranh: P1", vm.MapDataJson);

        // Toggle export boundary vertices off
        vm.ExportBoundaryVertices = false;
        Assert.Contains("\"exportBoundaryVertices\":false", vm.MapDataJson);
    }

    [Fact]
    public void MarkerStyleItem_and_PopupTemplateItem_have_icon_kinds_configured()
    {
        var vm = Create();
        Assert.NotEmpty(vm.MarkerStyles);
        foreach (var m in vm.MarkerStyles)
        {
            Assert.False(string.IsNullOrEmpty(m.Label));
            Assert.True(Enum.IsDefined(typeof(BoundaryMarkerStyle), m.Style));
        }

        Assert.NotEmpty(vm.PopupTemplates);
        foreach (var p in vm.PopupTemplates)
        {
            Assert.False(string.IsNullOrEmpty(p.Label));
            Assert.True(Enum.IsDefined(typeof(BoundaryPopupTemplate), p.Template));
        }
    }

    [Fact]
    public void Boundary_with_arc_tessellation_exports_only_cad_control_vertices_as_markers_while_preserving_smooth_boundary_polygon()
    {
        // 4 CAD control vertices
        var controlVertices = new[]
        {
            new PlanePoint(598000, 1232000),
            new PlanePoint(598100, 1232000),
            new PlanePoint(598100, 1232100),
            new PlanePoint(598000, 1232100),
        };
        // 20 tessellated drawing vertices (e.g. arc between vertex 1 and 2)
        var drawingVertices = new List<PlanePoint>
        {
            controlVertices[0],
            controlVertices[1],
        };
        for (int i = 1; i <= 16; i++)
        {
            drawingVertices.Add(new PlanePoint(598100 + i, 1232000 + i * 5));
        }
        drawingVertices.Add(controlVertices[2]);
        drawingVertices.Add(controlVertices[3]);

        var poly = new BoundaryPolyline("RanhCong", drawingVertices, Closed: true, SourceHandle: "TEST", ControlVertices: controlVertices);
        var vm = new GeoExportViewModel(
            Array.Empty<SurveyPoint>(),
            new[] { poly },
            "ArcBoundaryTest",
            null,
            DrawingUnit.Meters,
            new FakeShell());

        // Preview table must only contain the 4 CAD control vertices P1..P4
        Assert.Equal(4, vm.Preview.Count);
        Assert.Equal("P1", vm.Preview[0].Label);
        Assert.Equal("P2", vm.Preview[1].Label);
        Assert.Equal("P3", vm.Preview[2].Label);
        Assert.Equal("P4", vm.Preview[3].Label);

        // MapDataJson must contain 4 boundaryVertices (for markers) but drawingVertices count for boundary line
        Assert.Contains("\"label\":\"P1\"", vm.MapDataJson);
        Assert.Contains("\"label\":\"P4\"", vm.MapDataJson);
        Assert.DoesNotContain("\"label\":\"P5\"", vm.MapDataJson);

        // KMZ export via KmlDocumentBuilder: exactly 4 markers (P1..P4)
        var kmlBuilt = KmlDocumentBuilder.Build(vm.LastConversion!, new KmlExportOptions("ArcKml")
        {
            ExportBoundaryVertices = true,
            MarkerStyle = BoundaryMarkerStyle.Triangle,
        });
        Assert.Equal(4, kmlBuilt.VertexCount);
    }

    [Fact]
    public void ToggleMapFullscreen_switches_state_and_button_properties()
    {
        var vm = Create();
        Assert.False(vm.IsMapFullscreen);
        Assert.Equal("Toàn màn hình", vm.MapFullscreenButtonText);
        Assert.Equal("Fullscreen", vm.MapFullscreenButtonIcon);
        Assert.Equal("Mở rộng bản đồ toàn màn hình", vm.MapFullscreenButtonTooltip);

        vm.ToggleMapFullscreenCommand.Execute(null);
        Assert.True(vm.IsMapFullscreen);
        Assert.Equal("Thu gọn", vm.MapFullscreenButtonText);
        Assert.Equal("FullscreenExit", vm.MapFullscreenButtonIcon);
        Assert.Equal("Thu gọn bản đồ về bố cục 2 cột", vm.MapFullscreenButtonTooltip);

        vm.ToggleMapFullscreenCommand.Execute(null);
        Assert.False(vm.IsMapFullscreen);
        Assert.Equal("Toàn màn hình", vm.MapFullscreenButtonText);
        Assert.Equal("Fullscreen", vm.MapFullscreenButtonIcon);
    }

    [Fact]
    public void Output_mode_filters_map_data_and_summary_badge()
    {
        var poly = new BoundaryPolyline("Ranh", new[] { new PlanePoint(600100, 1231000), new PlanePoint(600200, 1231000), new PlanePoint(600150, 1231100) }, Closed: true);
        var vm = new GeoExportViewModel(Samples, new[] { poly }, "Site plan", null, DrawingUnit.Meters, new FakeShell());

        // Default: Both
        Assert.True(vm.OutputBoth);
        Assert.Contains("\"points\":[", vm.MapDataJson);
        Assert.Contains("\"boundaries\":[", vm.MapDataJson);
        Assert.Contains("\"boundaryVertices\":[", vm.MapDataJson);

        // Switch to Points only
        vm.OutputPoints = true;
        Assert.Contains("\"points\":[", vm.MapDataJson);
        Assert.Contains("\"boundaryVertices\":[", vm.MapDataJson);
        Assert.Contains("\"boundaries\":[]", vm.MapDataJson);
        Assert.StartsWith("Điểm ·", vm.MapSummaryBadge);

        // Switch to Boundaries only
        vm.OutputBoundaries = true;
        Assert.Contains("\"points\":[]", vm.MapDataJson);
        Assert.Contains("\"boundaryVertices\":[]", vm.MapDataJson);
        Assert.Contains("\"boundaries\":[{", vm.MapDataJson);
        Assert.StartsWith("Ranh đất ·", vm.MapSummaryBadge);

        // Switch back to Both
        vm.OutputBoth = true;
        Assert.Contains("\"points\":[", vm.MapDataJson);
        Assert.Contains("\"boundaries\":[{", vm.MapDataJson);
        Assert.Contains("\"boundaryVertices\":[", vm.MapDataJson);
    }
}
