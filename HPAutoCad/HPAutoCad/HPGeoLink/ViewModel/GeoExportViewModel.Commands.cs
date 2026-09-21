using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Units;
using HPAutoCad.HPGeoLink.Model;
using HPAutoCad.HPGeoLink.Support;

namespace HPAutoCad.HPGeoLink.ViewModel;

/// <summary>The dialog's buttons: KML preview, KMZ export, opening the result in Google Earth / Google Maps, close.</summary>
public sealed partial class GeoExportViewModel
{
    public event Action? CloseRequested;

    private KmlExportOptions KmlOptions => new(KmzWriter.SafeFileName(FileName))
    {
        Output = Output,
        PointColor = PointColor,
        LineColor = LineColor,
    };

    [RelayCommand]
    private void ToggleKmlPreview()
    {
        ShowKmlPreview = !ShowKmlPreview;
        if (ShowKmlPreview) RefreshKmlPreview();
    }

    private void RefreshKmlPreview()
    {
        if (LastConversion is null || !CanExport)
        {
            KmlPreview = "";
            return;
        }
        try
        {
            KmlPreview = KmlDocumentBuilder.Build(LastConversion, KmlOptions).Kml;
        }
        catch (ArgumentException exception)
        {
            KmlPreview = exception.Message;
        }
    }

    [RelayCommand]
    private void ExportKmz()
    {
        if (!CanExport) return;
        var path = _shell.AskSavePath(DocumentDirectory, KmzWriter.SafeFileName(FileName) + ".kmz");
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var outcome = KmzExportPipeline.Run(new KmzExportRequest(_points, _boundaries, Options, KmlOptions, path), _converter);
            if (!outcome.Success)
            {
                Status = "Xuất thất bại: " + string.Join("; ", outcome.Conversion.Errors.Select(e => e.Message));
                return;
            }
            LastExportPath = outcome.WrittenPath;
            var kml = outcome.Kml!;
            Status = $"Đã ghi {Path.GetFileName(outcome.WrittenPath)}: {kml.MarkerCount} điểm, {kml.BoundaryCount} ranh" +
                     (kml.BoundaryFromPoints ? " (ranh dựng từ thứ tự điểm)" : "");
            HPGeoLog.Information($"HPGEO wrote {outcome.WrittenPath}: {kml.MarkerCount} markers, {kml.BoundaryCount} boundaries, KTT {Crs.CurrentTm.CentralMeridianDeg.ToString(CultureInfo.InvariantCulture)}");
            // The point of the export is to see the plot in Google Earth: the button opens it there at once, and says what happened.
            var note = _shell.OpenInGoogleEarth(outcome.WrittenPath!);
            Status += " · " + note;
            HPGeoLog.Information($"HPGEO open after export: {note}");
        }
        catch (Exception exception)
        {
            Status = "Xuất thất bại: " + exception.Message;
            HPGeoLog.Error("HPGEO export failed", exception);
        }
    }

    /// <summary>Opens the last KMZ in the desktop Google Earth; before an export, the centre as a web URL.</summary>
    [RelayCommand]
    private void OpenGoogleEarth()
    {
        if (LastExportPath is not null && File.Exists(LastExportPath))
        {
            Status = _shell.OpenInGoogleEarth(LastExportPath);
            return;
        }
        if (LastConversion?.Center is { } c)
            _shell.OpenUrl(string.Create(CultureInfo.InvariantCulture, $"https://earth.google.com/web/search/{c.LatDeg:F7},{c.LonDeg:F7}"));
    }

    [RelayCommand]
    private void OpenGoogleMaps()
    {
        if (LastConversion?.Center is { } c)
            _shell.OpenUrl(string.Create(CultureInfo.InvariantCulture, $"https://www.google.com/maps?q={c.LatDeg:F7},{c.LonDeg:F7}"));
    }

    /// <summary>
    /// "Chèn ảnh vệ tinh vào CAD": the dialog only records the choice and closes; the command then fetches, warps
    /// and inserts on the command line (progress printed, Escape cancels) — the drawing is never touched from here.
    /// </summary>
    [RelayCommand]
    private void InsertImage()
    {
        if (LastConversion is not { Success: true })
        {
            Status = "Chưa chèn được ảnh: hệ toạ độ/đơn vị chưa hợp lệ (xem lỗi ở trên).";
            return;
        }
        var res = ParseNumber(ImageResolutionText);
        var ratio = ParseNumber(ImageAreaRatioText);
        if (res is null || !(res > 0) || ratio is null || !(ratio >= 1))
        {
            Status = "Chưa chèn được ảnh: độ phân giải (m/px) phải > 0 và vùng ảnh (× diện tích ranh) phải ≥ 1.";
            return;
        }
        if (_points.Count + _boundaries.Count == 0) return;
        var extent = SelectionExtentDrawingUnits;
        var f = Crs.MetersPerUnit;
        var extentM = new GridBoundingBox(extent.MinE * f, extent.MinN * f, extent.MaxE * f, extent.MaxN * f);
        var marginM = TileCoverage.MarginForAreaRatio(extentM, ratio.Value);
        ImageChoice = new GeoImageChoice(Crs.CurrentTm, Crs.SelectedUnit?.Unit ?? DrawingUnit.Unknown, f, res.Value, ratio.Value, marginM,
            extent, _points.Count, _boundaries.Count);
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
