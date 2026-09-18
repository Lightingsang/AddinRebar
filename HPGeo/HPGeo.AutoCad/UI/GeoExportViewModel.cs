using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using HPGeo.Core.Catalog;
using HPGeo.Core.Conversion;
using HPGeo.Core.Kml;
using HPGeo.Core.Settings;
using HPGeo.Core.Units;

namespace HPGeo.AutoCad.UI;

/// <summary>
/// State of the VN-2000 → KMZ dialog. Pure: takes what was read from the drawing, drives the Core converter
/// on every change and exposes the preview, the map data, the issues and whether an export is allowed. The
/// coordinate system lives in <see cref="Crs"/> (shared with the import dialog). Commands are in the other
/// half of the class.
/// </summary>
public sealed partial class GeoExportViewModel : ObservableObject
{
    private const int PreviewRowLimit = 200;
    private const int MapPointLimit = 5000;

    private readonly IReadOnlyList<SurveyPoint> _points;
    private readonly IReadOnlyList<BoundaryPolyline> _boundaries;
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
    }

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
        PointColor = PointColor,
        LineColor = LineColor,
        ExportDirectory = LastExportPath is null ? null : System.IO.Path.GetDirectoryName(LastExportPath),
    };

    public CrsSelectionViewModel Crs { get; }
    public string? DocumentDirectory { get; }
    public ObservableCollection<PreviewRow> Preview { get; } = new();

    public ConversionResult? LastConversion { get; private set; }
    public string? LastExportPath { get; private set; }

    [ObservableProperty] private bool outputBoth = true;
    [ObservableProperty] private bool outputPoints;
    [ObservableProperty] private bool outputBoundaries;
    [ObservableProperty] private string pointColor = KmlColor.DefaultPoint;
    [ObservableProperty] private string lineColor = KmlColor.DefaultLine;
    [ObservableProperty] private string fileName;
    [ObservableProperty] private string sourceSummary;
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string issuesText = "";
    [ObservableProperty] private bool canExport;
    [ObservableProperty] private string kmlPreview = "";
    [ObservableProperty] private bool showKmlPreview;
    /// <summary>What the map panel draws: {points:[{label,lat,lon}], boundaries:[{closed,vertices:[{lat,lon}]}]}.</summary>
    [ObservableProperty] private string mapDataJson = "";

    partial void OnOutputBothChanged(bool value) => Recompute();
    partial void OnOutputPointsChanged(bool value) => Recompute();
    partial void OnOutputBoundariesChanged(bool value) => Recompute();
    partial void OnPointColorChanged(string value) => Recompute();
    partial void OnLineColorChanged(string value) => Recompute();

    public KmlOutput Output => OutputPoints ? KmlOutput.Points : OutputBoundaries ? KmlOutput.Boundaries : KmlOutput.Both;

    private ConversionOptions Options => new(Crs.CurrentTm, Crs.MetersPerUnit) { ProvinceName = Crs.SelectedProvince?.Name };

    private void Recompute()
    {
        var result = _converter.Convert(_points, _boundaries, Options);
        LastConversion = result;

        Preview.Clear();
        var ci = CultureInfo.InvariantCulture;
        foreach (var p in result.Points.Take(PreviewRowLimit))
        {
            Preview.Add(new PreviewRow(p.Source.Label, p.GridM.Easting.ToString("F3", ci), p.GridM.Northing.ToString("F3", ci),
                p.Wgs84.LatDeg.ToString("F7", ci), p.Wgs84.LonDeg.ToString("F7", ci), p.Hint ?? ""));
        }
        foreach (var b in result.Boundaries.Take(PreviewRowLimit - Preview.Count))
        {
            var first = b.Wgs84[0];
            Preview.Add(new PreviewRow(b.Source.Name, b.GridM[0].Easting.ToString("F3", ci), b.GridM[0].Northing.ToString("F3", ci),
                first.LatDeg.ToString("F7", ci), first.LonDeg.ToString("F7", ci), $"{b.GridM.Count} đỉnh{(b.Source.Closed ? ", đóng" : "")}"));
        }
        MapDataJson = result.Success ? BuildMapData(result) : "";

        var colorsValid = KmlColor.IsValid(PointColor) && KmlColor.IsValid(LineColor);
        var issues = result.Issues.Select(i => $"[{i.Severity}] {i.Message}").ToList();
        if (!colorsValid) issues.Add("[Error] Màu phải là 8 ký tự hex aabbggrr (vd ff00ffff).");
        IssuesText = string.Join(Environment.NewLine, issues);
        CanExport = result.Success && colorsValid;
        var tm = Crs.CurrentTm;
        Status = result.Success
            ? $"{result.Points.Count} điểm, {result.Boundaries.Count} ranh · KTT {CentralMeridian.Format(tm.CentralMeridianDeg)}" +
              (result.Center is { } c ? $" · tâm {c.LatDeg.ToString("F5", ci)}, {c.LonDeg.ToString("F5", ci)}" : "")
            : $"{result.Errors.Count()} lỗi — chưa xuất được";
        if (ShowKmlPreview) RefreshKmlPreview();
    }

    private static string BuildMapData(ConversionResult result)
    {
        var data = new
        {
            points = result.Points.Take(MapPointLimit).Select(p => new { label = p.Source.Label, lat = p.Wgs84.LatDeg, lon = p.Wgs84.LonDeg }),
            boundaries = result.Boundaries.Select(b => new
            {
                closed = b.Source.Closed,
                vertices = b.Wgs84.Select(v => new { lat = v.LatDeg, lon = v.LonDeg }),
            }),
        };
        return JsonSerializer.Serialize(data);
    }
}
