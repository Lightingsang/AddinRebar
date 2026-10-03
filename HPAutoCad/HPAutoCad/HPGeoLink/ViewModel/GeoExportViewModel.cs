using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using HPAutoCad.Core.HPGeoLink.Catalog;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Settings;
using HPAutoCad.Core.HPGeoLink.Units;
using HPAutoCad.HPGeoLink.Model;
using MaterialDesignThemes.Wpf;

namespace HPAutoCad.HPGeoLink.ViewModel;

/// <summary>
/// State of the VN-2000 → KMZ dialog. Pure: takes what was read from the drawing, drives the Core converter
/// on every change and exposes the preview, the map data, the issues and whether an export is allowed. The
/// coordinate system lives in <see cref="Crs"/> (shared with the import dialog). Commands are in the other
/// half of the class.
/// </summary>
public sealed partial class GeoExportViewModel : ObservableObject
{
    private const int PreviewRowLimit = 1000;
    private const int MapPointLimit = 5000;

    private IReadOnlyList<SurveyPoint> _points;
    private IReadOnlyList<BoundaryPolyline> _boundaries;
    private readonly Vn2000Converter _converter = new();
    private readonly IGeoExportShell _shell;

    public GeoExportViewModel(IReadOnlyList<SurveyPoint> points, IReadOnlyList<BoundaryPolyline> boundaries,
        string documentBaseName, string? documentDirectory, DrawingUnit drawingUnit, IGeoExportShell shell,
        GeoSettings? stored = null, ProvinceCatalog? catalog = null)
    {
        _points = points;
        _boundaries = boundaries;
        _shell = shell;
        DocumentDirectory = documentDirectory;
        fileName = KmzWriter.SafeFileName(documentBaseName);
        sourceSummary = $"{points.Count} POINT · {boundaries.Count} LWPOLYLINE ({boundaries.Count(b => b.Closed)} closed)";
        selectedMarkerStyleItem = MarkerStyles[0];
        selectedPopupTemplateItem = PopupTemplates[0];
        Crs = new CrsSelectionViewModel(drawingUnit, catalog);
        if (stored is not null) ApplySettings(stored);
        Crs.Changed += Recompute;
        Recompute();
    }

    /// <summary>Stored settings (the drawing's, else the user's last) take precedence over the first-run defaults.</summary>
    public void ApplySettings(GeoSettings s)
    {
        Crs.Apply(s.UseCurrentCatalog, s.ProvinceName, s.CentralMeridianDeg, s.ScaleFactor, s.FalseEasting, s.FalseNorthing, s.Unit);
        OutputBoth = s.Output == KmlOutput.Both;
        OutputPoints = s.Output == KmlOutput.Points;
        OutputBoundaries = s.Output == KmlOutput.Boundaries;
        if (KmlColor.IsValid(s.PointColor)) PointColor = s.PointColor;
        if (KmlColor.IsValid(s.LineColor)) LineColor = s.LineColor;
        ExportBoundaryVertices = s.ExportBoundaryVertices;
        SelectedMarkerStyleItem = MarkerStyles.FirstOrDefault(m => m.Style == s.BoundaryMarkerStyle) ?? MarkerStyles[0];
        SelectedPopupTemplateItem = PopupTemplates.FirstOrDefault(t => t.Template == s.BoundaryPopupTemplate) ?? PopupTemplates[0];
        if (s.ImageryResolutionMPerPx is { } res && res > 0) ImageResolutionText = res.ToString("0.###", CultureInfo.InvariantCulture);
        if (s.ImageryAreaRatio is { } ratio && ratio >= 1) ImageAreaRatioText = ratio.ToString("0.#", CultureInfo.InvariantCulture);
        _carryImagery = s.ImageryProvider is not null || s.ImageryResolutionMPerPx is not null || s.ImageryAreaRatio is not null;
    }

    /// <summary>Imagery fields travel back only when the record already had them or the imagery button was used — a plain KMZ export never invents them.</summary>
    private bool _carryImagery;

    /// <summary>What to remember after a successful export.</summary>
    public GeoSettings ToSettings() => new()
    {
        UseCurrentCatalog = Crs.UseCurrentCatalog,
        ProvinceName = Crs.SelectedProvince?.Name,
        CentralMeridianDeg = double.IsFinite(Crs.CurrentTm.CentralMeridianDeg) ? Crs.CurrentTm.CentralMeridianDeg : null,
        ScaleFactor = Crs.CurrentTm.ScaleFactor,
        FalseEasting = Crs.CurrentTm.FalseEasting,
        FalseNorthing = Crs.CurrentTm.FalseNorthing,
        Unit = Crs.UnitFromDrawing ? null : Crs.SelectedUnit?.Unit,
        Output = Output,
        PointColor = KmlColor.TryNormalize(PointColor, out var pc) ? pc : PointColor,
        LineColor = KmlColor.TryNormalize(LineColor, out var lc) ? lc : LineColor,
        ExportBoundaryVertices = ExportBoundaryVertices,
        BoundaryMarkerStyle = SelectedMarkerStyleItem?.Style ?? BoundaryMarkerStyle.Triangle,
        BoundaryPopupTemplate = SelectedPopupTemplateItem?.Template ?? BoundaryPopupTemplate.Cadastral,
        ExportDirectory = LastExportPath is null ? null : System.IO.Path.GetDirectoryName(LastExportPath),
        ImageryProvider = _carryImagery || ImageChoice is not null ? ImageryProviders.Default.Id : null,
        ImageryResolutionMPerPx = _carryImagery || ImageChoice is not null ? ParseNumber(ImageResolutionText) : null,
        ImageryAreaRatio = _carryImagery || ImageChoice is not null ? ParseNumber(ImageAreaRatioText) : null,
        ImageryMarginM = ImageChoice?.MarginM,
    };

    /// <summary>The choice made with "Chèn ảnh vệ tinh vào CAD"; null when the button was not used.</summary>
    public GeoImageChoice? ImageChoice { get; private set; }

    /// <summary>Extent of every point and boundary vertex the dialog holds, in drawing units.</summary>
    public GridBoundingBox SelectionExtentDrawingUnits =>
        GridBoundingBox.Of(_points.Select(p => p.DrawingXY).Concat(_boundaries.SelectMany(b => b.DrawingVertices)));

    internal static double? ParseNumber(string text) =>
        double.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? v : null;

    public CrsSelectionViewModel Crs { get; }
    public string? DocumentDirectory { get; }
    public ObservableCollection<PreviewRow> Preview { get; } = new();

    public IReadOnlyList<MarkerStyleItem> MarkerStyles { get; } = new[]
    {
        new MarkerStyleItem(BoundaryMarkerStyle.Triangle, "Tam giác trắc địa (Vector)", PackIconKind.TriangleOutline),
        new MarkerStyleItem(BoundaryMarkerStyle.Pushpin, "Ghim định vị (Pushpin)", PackIconKind.MapMarker),
        new MarkerStyleItem(BoundaryMarkerStyle.Circle, "Chấm tròn (Circle)", PackIconKind.CircleMedium),
        new MarkerStyleItem(BoundaryMarkerStyle.LabelOnly, "Chỉ nhãn chữ (Label only)", PackIconKind.FormatLetterCase),
    };

    public IReadOnlyList<PopupTemplateItem> PopupTemplates { get; } = new[]
    {
        new PopupTemplateItem(BoundaryPopupTemplate.Cadastral, "Bảng địa chính chuẩn", PackIconKind.TableLarge),
        new PopupTemplateItem(BoundaryPopupTemplate.Technical, "Tọa độ kỹ thuật DMS", PackIconKind.CompassOutline),
        new PopupTemplateItem(BoundaryPopupTemplate.Simple, "Gọn nhẹ (Simple text)", PackIconKind.CardTextOutline),
    };

    public ConversionResult? LastConversion { get; private set; }
    public string? LastExportPath { get; private set; }

    [ObservableProperty] private bool outputBoth = true;
    [ObservableProperty] private bool outputPoints;
    [ObservableProperty] private bool outputBoundaries;
    [ObservableProperty] private string pointColor = KmlColor.DefaultPoint;
    [ObservableProperty] private string lineColor = KmlColor.DefaultLine;
    [ObservableProperty] private bool exportBoundaryVertices = true;
    [ObservableProperty] private MarkerStyleItem selectedMarkerStyleItem;
    [ObservableProperty] private PopupTemplateItem selectedPopupTemplateItem;
    [ObservableProperty] private string fileName;
    [ObservableProperty] private string sourceSummary;
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string issuesText = "";
    [ObservableProperty] private bool canExport;
    [ObservableProperty] private string kmlPreview = "";
    [ObservableProperty] private bool showKmlPreview;
    /// <summary>Satellite imagery under the selection: target ground resolution (m/px) and the image area as a multiple of the selection's box, beside the insert button.</summary>
    [ObservableProperty] private string imageResolutionText = TileCoverage.DefaultResolutionMPerPx.ToString("0.###", CultureInfo.InvariantCulture);
    [ObservableProperty] private string imageAreaRatioText = TileCoverage.DefaultAreaRatio.ToString("0.#", CultureInfo.InvariantCulture);
    /// <summary>What the map panel draws: {points:[{label,lat,lon}], boundaries:[{closed,vertices:[{lat,lon}]}], pointColor, lineColor}.</summary>
    [ObservableProperty] private string mapDataJson = "";
    [ObservableProperty] private string mapSummaryBadge = "Ranh đất · 0 điểm";
    [ObservableProperty] private string mapCenterBadge = "WGS84";
    [ObservableProperty] private bool isMapFullscreen;

    public string MapFullscreenButtonText => IsMapFullscreen ? "Thu gọn" : "Toàn màn hình";
    public string MapFullscreenButtonIcon => IsMapFullscreen ? "FullscreenExit" : "Fullscreen";
    public string MapFullscreenButtonTooltip => IsMapFullscreen ? "Thu gọn bản đồ về bố cục 2 cột" : "Mở rộng bản đồ toàn màn hình";

    partial void OnIsMapFullscreenChanged(bool value)
    {
        OnPropertyChanged(nameof(MapFullscreenButtonText));
        OnPropertyChanged(nameof(MapFullscreenButtonIcon));
        OnPropertyChanged(nameof(MapFullscreenButtonTooltip));
    }

    partial void OnOutputBothChanged(bool value)
    {
        if (value)
        {
            outputPoints = false;
            outputBoundaries = false;
            OnPropertyChanged(nameof(OutputPoints));
            OnPropertyChanged(nameof(OutputBoundaries));
        }
        Recompute();
    }

    partial void OnOutputPointsChanged(bool value)
    {
        if (value)
        {
            outputBoth = false;
            outputBoundaries = false;
            OnPropertyChanged(nameof(OutputBoth));
            OnPropertyChanged(nameof(OutputBoundaries));
        }
        Recompute();
    }

    partial void OnOutputBoundariesChanged(bool value)
    {
        if (value)
        {
            outputBoth = false;
            outputPoints = false;
            OnPropertyChanged(nameof(OutputBoth));
            OnPropertyChanged(nameof(OutputPoints));
        }
        Recompute();
    }
    partial void OnPointColorChanged(string value) => Recompute();
    partial void OnLineColorChanged(string value) => Recompute();
    partial void OnExportBoundaryVerticesChanged(bool value) => Recompute();
    partial void OnSelectedMarkerStyleItemChanged(MarkerStyleItem value) => Recompute();
    partial void OnSelectedPopupTemplateItemChanged(PopupTemplateItem value) => Recompute();

    public KmlOutput Output => OutputPoints ? KmlOutput.Points : OutputBoundaries ? KmlOutput.Boundaries : KmlOutput.Both;

    private ConversionOptions Options => new(Crs.CurrentTm, Crs.MetersPerUnit) { ProvinceName = Crs.SelectedProvince?.Name };

    public void UpdateObjects(IReadOnlyList<SurveyPoint> points, IReadOnlyList<BoundaryPolyline> boundaries, string? baseName = null, string? directoryPath = null)
    {
        _points = points;
        _boundaries = boundaries;
        if (!string.IsNullOrWhiteSpace(baseName))
            FileName = KmzWriter.SafeFileName(baseName);
        SourceSummary = $"{points.Count} POINT · {boundaries.Count} LWPOLYLINE ({boundaries.Count(b => b.Closed)} closed)";
        Recompute();
    }

    private void Recompute()
    {
        var result = _converter.Convert(_points, _boundaries, Options);
        LastConversion = result;

        Preview.Clear();
        var ci = CultureInfo.InvariantCulture;
        var stt = 1;
        foreach (var p in result.Points.Take(PreviewRowLimit))
        {
            Preview.Add(new PreviewRow(
                stt++,
                p.Source.Label,
                "Điểm",
                p.GridM.Easting.ToString("F3", ci),
                p.GridM.Northing.ToString("F3", ci),
                p.Wgs84.LatDeg.ToString("F7", ci),
                p.Wgs84.LonDeg.ToString("F7", ci),
                p.Hint ?? ""));
        }
        foreach (var b in result.Boundaries)
        {
            if (Preview.Count >= PreviewRowLimit) break;
            var gridPts = b.CadGridM;
            var wgsPts = b.CadWgs84;
            var vertexCount = gridPts.Count;
            for (var i = 0; i < vertexCount; i++)
            {
                if (Preview.Count >= PreviewRowLimit) break;
                var pt = gridPts[i];
                var geo = wgsPts[i];
                var vertexLabel = result.Boundaries.Count == 1 ? $"P{i + 1}" : $"{b.Source.Name}-P{i + 1}";
                var note = $"{b.Source.Name} (đỉnh {i + 1}/{vertexCount}{(b.Source.Closed ? ", đóng" : "")})";
                Preview.Add(new PreviewRow(
                    stt++,
                    vertexLabel,
                    "Đỉnh ranh",
                    pt.Easting.ToString("F3", ci),
                    pt.Northing.ToString("F3", ci),
                    geo.LatDeg.ToString("F7", ci),
                    geo.LonDeg.ToString("F7", ci),
                    note));
            }
        }
        MapDataJson = result.Success ? BuildMapData(result, PointColor, LineColor) : "";

        var colorsValid = KmlColor.IsValid(PointColor) && KmlColor.IsValid(LineColor);
        var issues = result.Issues.Select(i => $"[{i.Severity}] {i.Message}").ToList();
        if (!colorsValid) issues.Add("[Error] Màu phải là 8 ký tự hex aabbggrr (vd ff00ffff) hoặc mã HEX #RRGGBB (vd #FF0000).");
        IssuesText = string.Join(Environment.NewLine, issues);
        CanExport = result.Success && colorsValid;
        var tm = Crs.CurrentTm;
        Status = result.Success
            ? $"{result.Points.Count} điểm, {result.Boundaries.Count} ranh · KTT {CentralMeridian.Format(tm.CentralMeridianDeg)}" +
              (result.Center is { } c ? $" · tâm {c.LatDeg.ToString("F5", ci)}, {c.LonDeg.ToString("F5", ci)}" : "")
            : $"{result.Errors.Count()} lỗi — chưa xuất được";

        var totalPts = result.Points.Count > 0 ? result.Points.Count : result.Boundaries.Sum(b => b.CadGridM.Count);
        MapSummaryBadge = Output switch
        {
            KmlOutput.Points => $"Điểm · {totalPts} điểm",
            KmlOutput.Boundaries => $"Ranh đất · {result.Boundaries.Count} ranh",
            _ => $"Ranh đất · {totalPts} điểm",
        };
        MapCenterBadge = result.Center is { } center
            ? $"WGS84 · tâm {center.LatDeg.ToString("F5", ci)}, {center.LonDeg.ToString("F5", ci)}"
            : "WGS84";

        if (ShowKmlPreview) RefreshKmlPreview();
    }

    private string BuildMapData(ConversionResult result, string pointColor, string lineColor)
    {
        var totalBoundaries = result.Boundaries.Count;
        var bndVertices = new List<object>();

        var showPoints = Output != KmlOutput.Boundaries;
        var showBoundaries = Output != KmlOutput.Points;
        var exportVertices = (ExportBoundaryVertices || Output == KmlOutput.Points) && showPoints;

        if (exportVertices)
        {
            foreach (var b in result.Boundaries)
            {
                var gridPts = b.CadGridM;
                var wgsPts = b.CadWgs84;
                var vertexCount = gridPts.Count;
                for (var i = 0; i < vertexCount; i++)
                {
                    var vertexLabel = totalBoundaries == 1 ? $"P{i + 1}" : $"{b.Source.Name}-P{i + 1}";
                    double? segmentDist = null;
                    string? nextLabel = null;
                    if (b.Source.Closed)
                    {
                        var nextIdx = (i + 1) % vertexCount;
                        var currentPt = gridPts[i];
                        var nextPt = gridPts[nextIdx];
                        var dx = nextPt.Easting - currentPt.Easting;
                        var dy = nextPt.Northing - currentPt.Northing;
                        segmentDist = Math.Sqrt(dx * dx + dy * dy);
                        nextLabel = totalBoundaries == 1 ? $"P{nextIdx + 1}" : $"{b.Source.Name}-P{nextIdx + 1}";
                    }
                    else if (i < vertexCount - 1)
                    {
                        var nextIdx = i + 1;
                        var currentPt = gridPts[i];
                        var nextPt = gridPts[nextIdx];
                        var dx = nextPt.Easting - currentPt.Easting;
                        var dy = nextPt.Northing - currentPt.Northing;
                        segmentDist = Math.Sqrt(dx * dx + dy * dy);
                        nextLabel = totalBoundaries == 1 ? $"P{nextIdx + 1}" : $"{b.Source.Name}-P{nextIdx + 1}";
                    }

                    var popupHtml = KmlDocumentBuilder.BuildBalloon(
                        SelectedPopupTemplateItem?.Template ?? BoundaryPopupTemplate.Cadastral,
                        vertexLabel,
                        b.Source.Name,
                        gridPts[i],
                        wgsPts[i],
                        nextLabel,
                        segmentDist);

                    bndVertices.Add(new
                    {
                        label = vertexLabel,
                        lat = wgsPts[i].LatDeg,
                        lon = wgsPts[i].LonDeg,
                        easting = gridPts[i].Easting,
                        northing = gridPts[i].Northing,
                        segmentLength = segmentDist,
                        nextLabel,
                        popupHtml,
                    });
                }
            }
        }

        var markerStyleStr = (SelectedMarkerStyleItem?.Style ?? BoundaryMarkerStyle.Triangle) switch
        {
            BoundaryMarkerStyle.Pushpin => "pushpin",
            BoundaryMarkerStyle.Circle => "circle",
            BoundaryMarkerStyle.LabelOnly => "labelOnly",
            _ => "triangle",
        };

        var data = new
        {
            points = showPoints ? result.Points.Take(MapPointLimit).Select(p => new { label = p.Source.Label, lat = p.Wgs84.LatDeg, lon = p.Wgs84.LonDeg }) : Enumerable.Empty<object>(),
            boundaries = showBoundaries ? result.Boundaries.Select(b => new
            {
                closed = b.Source.Closed,
                vertices = b.Wgs84.Select(v => new { lat = v.LatDeg, lon = v.LonDeg }),
            }) : Enumerable.Empty<object>(),
            boundaryVertices = bndVertices,
            exportBoundaryVertices = exportVertices,
            markerStyle = markerStyleStr,
            pointColor = KmlColor.KmlToRgbHex(pointColor),
            lineColor = KmlColor.KmlToRgbHex(lineColor),
        };
        return JsonSerializer.Serialize(data, MapJsonOptions);
    }

    private static readonly JsonSerializerOptions MapJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
