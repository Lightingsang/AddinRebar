using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPGeo.Core.Catalog;
using HPGeo.Core.Conversion;
using HPGeo.Core.Import;
using HPGeo.Core.Settings;
using HPGeo.Core.Units;

namespace HPGeo.AutoCad.UI;

/// <summary>The host side the import dialog needs: an open-file dialog.</summary>
public interface IGeoImportShell
{
    string? AskOpenPath();
}

/// <summary>One row of the import preview.</summary>
public sealed record ImportPreviewRow(string Label, string Lat, string Lon, string Easting, string Northing, string Note);

/// <summary>
/// State of the WGS84 / VN-2000 → drawing dialog. Source is either a KML/KMZ file or pasted text (lat,lon or
/// E,N with the order rule); the plan is recomputed on every change and handed back through <see cref="Result"/>
/// when the user confirms — the command then writes it in one transaction. Pure apart from the shell.
/// </summary>
public sealed partial class GeoImportViewModel : ObservableObject
{
    private const int PreviewRowLimit = 200;
    private readonly ImportPlanner _planner = new();
    private readonly IGeoImportShell _shell;
    private IReadOnlyList<KmlFeature> _features = Array.Empty<KmlFeature>();

    public GeoImportViewModel(DrawingUnit drawingUnit, IGeoImportShell shell, GeoSettings? stored = null, ProvinceCatalog? catalog = null)
    {
        _shell = shell;
        Crs = new CrsSelectionViewModel(drawingUnit, catalog);
        if (stored is not null)
            Crs.Apply(stored.UseCurrentCatalog, stored.ProvinceName, stored.CentralMeridianDeg, stored.ScaleFactor, stored.FalseEasting, stored.FalseNorthing, stored.Unit);
        Crs.Changed += Recompute;
        Recompute();
    }

    /// <summary>The coordinate system to remember after a successful import (merged over the stored record).</summary>
    public GeoSettings ToSettings(GeoSettings? existing) => (existing ?? new GeoSettings()) with
    {
        UseCurrentCatalog = Crs.UseCurrentCatalog,
        ProvinceName = Crs.SelectedProvince?.Name,
        CentralMeridianDeg = double.IsFinite(Crs.CurrentTm.CentralMeridianDeg) ? Crs.CurrentTm.CentralMeridianDeg : null,
        ScaleFactor = Crs.CurrentTm.ScaleFactor,
        FalseEasting = Crs.CurrentTm.FalseEasting,
        FalseNorthing = Crs.CurrentTm.FalseNorthing,
        Unit = Crs.UnitFromDrawing ? null : Crs.SelectedUnit?.Unit,
    };

    public event Action? CloseRequested;

    public CrsSelectionViewModel Crs { get; }
    public ObservableCollection<ImportPreviewRow> Preview { get; } = new();

    /// <summary>The plan the user confirmed with "Vẽ vào bản vẽ"; null when the dialog was closed otherwise.</summary>
    public ImportPlan? Result { get; private set; }
    public ImportPlan? CurrentPlan { get; private set; }

    [ObservableProperty] private bool sourceIsFile = true;
    [ObservableProperty] private string filePath = "";
    [ObservableProperty] private string pastedText = "";
    [ObservableProperty] private bool pastedIsWgs84 = true;
    [ObservableProperty] private int pairOrderIndex; // 0 auto, 1 E,N, 2 X,Y
    [ObservableProperty] private bool joinAsPolyline;
    [ObservableProperty] private bool closeRing = true;
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string issuesText = "";
    [ObservableProperty] private bool canDraw;

    /// <summary>The second radio button of the pair; settable so its TwoWay binding never throws.</summary>
    public bool SourceIsText
    {
        get => !SourceIsFile;
        set => SourceIsFile = !value;
    }
    public PairOrder PairOrder => PairOrderIndex switch { 1 => PairOrder.EastingNorthing, 2 => PairOrder.CadastralXY, _ => PairOrder.Auto };

    partial void OnSourceIsFileChanged(bool value)
    {
        OnPropertyChanged(nameof(SourceIsText));
        Recompute();
    }

    partial void OnFilePathChanged(string value)
    {
        _features = Array.Empty<KmlFeature>();
        Recompute();
    }

    partial void OnPastedTextChanged(string value) => Recompute();
    partial void OnPastedIsWgs84Changed(bool value) => Recompute();
    partial void OnPairOrderIndexChanged(int value) => Recompute();
    partial void OnJoinAsPolylineChanged(bool value) => Recompute();
    partial void OnCloseRingChanged(bool value) => Recompute();

    [RelayCommand]
    private void BrowseFile()
    {
        var path = _shell.AskOpenPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            SourceIsFile = true;
            FilePath = path;
        }
    }

    [RelayCommand]
    private void Draw()
    {
        if (!CanDraw || CurrentPlan is null) return;
        Result = CurrentPlan;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private void Recompute()
    {
        var options = new ConversionOptions(Crs.CurrentTm, Crs.MetersPerUnit) { ProvinceName = Crs.SelectedProvince?.Name };
        var extra = new List<string>();
        ImportPlan plan;
        try
        {
            plan = SourceIsFile ? PlanFromFile(options, extra) : PlanFromText(options);
        }
        catch (Exception exception)
        {
            plan = new ImportPlan(Array.Empty<ImportPoint>(), Array.Empty<ImportPolyline>(),
                new[] { new ConversionIssue(IssueSeverity.Error, "READ_FAILED", exception.Message) }, options);
        }
        CurrentPlan = plan;

        Preview.Clear();
        var ci = CultureInfo.InvariantCulture;
        foreach (var p in plan.Points.Take(PreviewRowLimit))
        {
            Preview.Add(new ImportPreviewRow(p.Label,
                p.Wgs84?.LatDeg.ToString("F7", ci) ?? "", p.Wgs84?.LonDeg.ToString("F7", ci) ?? "",
                p.GridM.Easting.ToString("F3", ci), p.GridM.Northing.ToString("F3", ci), p.Note ?? ""));
        }
        foreach (var pl in plan.Polylines.Take(PreviewRowLimit - Preview.Count))
            Preview.Add(new ImportPreviewRow(pl.Name, "", "", "", "", $"{pl.DrawingVertices.Count} đỉnh{(pl.Closed ? ", đóng" : "")}"));

        IssuesText = string.Join(Environment.NewLine, extra.Concat(plan.Issues.Select(i => $"[{i.Severity}] {i.Message}")));
        CanDraw = plan.Success;
        Status = plan.Success
            ? $"{plan.Points.Count} điểm, {plan.Polylines.Count} polyline sẽ được vẽ lên layer HPGEO-IMPORT · KTT {CentralMeridian.Format(Crs.CurrentTm.CentralMeridianDeg)}"
            : plan.Points.Count + plan.Polylines.Count == 0 && !plan.Errors.Any() ? "Chưa có dữ liệu." : $"{plan.Errors.Count()} lỗi — chưa vẽ được";
    }

    private ImportPlan PlanFromFile(ConversionOptions options, List<string> extra)
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            return new ImportPlan(Array.Empty<ImportPoint>(), Array.Empty<ImportPolyline>(), Array.Empty<ConversionIssue>(), options);
        if (!File.Exists(FilePath))
            return new ImportPlan(Array.Empty<ImportPoint>(), Array.Empty<ImportPolyline>(),
                new[] { new ConversionIssue(IssueSeverity.Error, "FILE_NOT_FOUND", $"Không tìm thấy file: {FilePath}") }, options);
        if (_features.Count == 0) _features = KmlReader.ReadFile(FilePath);
        extra.Add($"{Path.GetFileName(FilePath)}: {_features.Count(f => f.Kind == KmlFeatureKind.Point)} điểm, {_features.Count(f => f.Kind == KmlFeatureKind.Line)} đường, {_features.Count(f => f.Kind == KmlFeatureKind.Polygon)} vùng");
        return _planner.FromKml(_features, options);
    }

    private ImportPlan PlanFromText(ConversionOptions options)
    {
        var kind = PastedIsWgs84 ? PastedCoordinateKind.Wgs84 : PastedCoordinateKind.Vn2000;
        var parsed = CoordinateTextParser.Parse(PastedText, kind, PairOrder);
        if (parsed.Count == 0 && string.IsNullOrWhiteSpace(PastedText))
            return new ImportPlan(Array.Empty<ImportPoint>(), Array.Empty<ImportPolyline>(), Array.Empty<ConversionIssue>(), options);
        return _planner.FromPasted(parsed, options, JoinAsPolyline, JoinAsPolyline && CloseRing);
    }
}
