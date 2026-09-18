using HPGeo.Core.Conversion;

namespace HPGeo.Core.Kml;

public sealed record KmzExportRequest(
    IReadOnlyList<SurveyPoint> Points,
    IReadOnlyList<BoundaryPolyline> Boundaries,
    ConversionOptions Conversion,
    KmlExportOptions Kml,
    string OutputPath);

/// <summary>What an export produced. <see cref="WrittenPath"/> is null when the conversion failed.</summary>
public sealed record KmzExportOutcome(ConversionResult Conversion, KmlBuildResult? Kml, string? WrittenPath)
{
    public bool Success => WrittenPath is not null;
}

/// <summary>
/// Convert → build KML → write KMZ, the one path both the dialog and the script command take. A conversion
/// with errors writes nothing; warnings never block.
/// </summary>
public static class KmzExportPipeline
{
    public static KmzExportOutcome Run(KmzExportRequest request, Vn2000Converter? converter = null)
    {
        var conversion = (converter ?? new Vn2000Converter()).Convert(request.Points, request.Boundaries, request.Conversion);
        if (!conversion.Success) return new KmzExportOutcome(conversion, null, null);

        var kml = KmlDocumentBuilder.Build(conversion, request.Kml);
        KmzWriter.WriteFile(kml.Kml, request.OutputPath);
        return new KmzExportOutcome(conversion, kml, Path.GetFullPath(request.OutputPath));
    }
}
