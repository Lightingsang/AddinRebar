using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using HPGeo.Core.Conversion;
using HPGeo.Core.Kml;

namespace HPGeo.AutoCad.UI;

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
        }
        catch (Exception exception)
        {
            Status = "Xuất thất bại: " + exception.Message;
            HPGeoLog.Error("HPGEO export failed", exception);
        }
    }

    /// <summary>Opens the last KMZ in Google Earth (file association); before an export, the centre as a web URL.</summary>
    [RelayCommand]
    private void OpenGoogleEarth()
    {
        if (LastExportPath is not null && File.Exists(LastExportPath))
        {
            _shell.OpenPath(LastExportPath);
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

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
