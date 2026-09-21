namespace HPAutoCad.HPGeoLink.Model;

/// <summary>One row of the import preview.</summary>
public sealed record ImportPreviewRow(string Label, string Lat, string Lon, string Easting, string Northing, string Note);
