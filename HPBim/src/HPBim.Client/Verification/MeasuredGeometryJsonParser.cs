namespace HPBim.Client.Verification;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;

public static class MeasuredGeometryJsonParser
{
    public static IReadOnlyDictionary<string, ActualElementGeometry> ParseActuals(JsonElement root)
    {
        var dict = new Dictionary<string, ActualElementGeometry>(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty("actuals", out var actualsProp) || actualsProp.ValueKind != JsonValueKind.Array)
        {
            return dict;
        }

        foreach (var el in actualsProp.EnumerateArray())
        {
            var geom = ParseSingle(el);
            if (!string.IsNullOrEmpty(geom.Key))
            {
                dict[geom.Key] = geom;
            }
            if (!string.IsNullOrEmpty(geom.UniqueId))
            {
                dict[geom.UniqueId] = geom;
            }
        }

        return dict;
    }

    private static ActualElementGeometry ParseSingle(JsonElement el) => new()
    {
        Key = GetString(el, "key"),
        UniqueId = GetOptionalString(el, "uniqueId"),
        ElementId = GetOptionalLong(el, "elementId"),
        CategoryName = GetString(el, "category"),
        TypeName = GetString(el, "typeName"),
        HasGeometry = el.TryGetProperty("hasGeometry", out var hg) && hg.GetBoolean(),
        Centroid = ParsePoint(el, "centroid"),
        CurveStart = ParsePoint(el, "curveStart"),
        CurveEnd = ParsePoint(el, "curveEnd"),
        RotationDegrees = GetOptionalDouble(el, "rotationDegrees"),
        TopElevationMm = GetOptionalDouble(el, "topElevationMm"),
        BottomElevationMm = GetOptionalDouble(el, "bottomElevationMm"),
        WidthMm = GetOptionalDouble(el, "widthMm"),
        HeightMm = GetOptionalDouble(el, "heightMm"),
        ThicknessMm = GetOptionalDouble(el, "thicknessMm")
    };

    private static Point3D? ParsePoint(JsonElement el, string propName)
    {
        if (el.TryGetProperty(propName, out var pt) && pt.ValueKind == JsonValueKind.Object)
        {
            double x = GetOptionalDouble(pt, "x") ?? 0.0;
            double y = GetOptionalDouble(pt, "y") ?? 0.0;
            double z = GetOptionalDouble(pt, "z") ?? 0.0;
            return new Point3D(x, y, z);
        }
        return null;
    }

    private static string GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? string.Empty : string.Empty;

    private static string? GetOptionalString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static double? GetOptionalDouble(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDouble() : null;

    private static long? GetOptionalLong(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt64() : null;
}
