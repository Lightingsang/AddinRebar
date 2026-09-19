using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using HPGeo.Core.Kml;
using HPGeo.Core.Settings;
using HPGeo.Core.Units;

namespace HPGeo.AutoCad.Cad;

/// <summary>
/// The drawing's own copy of the settings: an Xrecord named <see cref="GeoSettings.Key"/> in the named
/// objects dictionary, so the central meridian a survey was converted with travels with the DWG. Stored as
/// typed DXF groups (1 = string, 40 = real) under stable keys, read leniently: a missing or foreign entry is
/// "no settings". Writing is one small undoable change inside the caller's command.
/// </summary>
internal static class DocumentSettingsStore
{
    private const int Version = 1;
    private const string K = "k=";

    public static GeoSettings? Read(Database db)
    {
        try
        {
            return ReadCore(db);
        }
        catch (Exception exception)
        {
            // A foreign or damaged HPGEO entry must never take every command down: it reads as "no settings".
            HPGeoLog.Warning($"HPGEO settings record unreadable, ignored: {exception.Message}");
            return null;
        }
    }

    private static GeoSettings? ReadCore(Database db)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains(GeoSettings.Key)) return null;
        if (tr.GetObject(nod.GetAt(GeoSettings.Key), OpenMode.ForRead) is not Xrecord xrecord || xrecord.Data is null) return null;

        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        string? pendingKey = null;
        foreach (var tv in xrecord.Data)
        {
            if (tv.TypeCode == (short)DxfCode.Text && tv.Value is string s && s.StartsWith(K, StringComparison.Ordinal))
            {
                pendingKey = s[K.Length..];
                continue;
            }
            if (pendingKey is not null)
            {
                values[pendingKey] = tv.Value;
                pendingKey = null;
            }
        }
        tr.Commit();
        return new GeoSettings
        {
            UseCurrentCatalog = Str(values, "catalog") != "legacy",
            ProvinceName = Str(values, "province"),
            CentralMeridianDeg = Real(values, "cm"),
            ScaleFactor = Real(values, "k0"),
            FalseEasting = Real(values, "fe"),
            FalseNorthing = Real(values, "fn"),
            Unit = Enum.TryParse<DrawingUnit>(Str(values, "unit"), out var unit) ? unit : null,
            Output = ParseOutputLenient(Str(values, "output")),
            PointColor = Str(values, "pcolor") ?? KmlColor.DefaultPoint,
            LineColor = Str(values, "lcolor") ?? KmlColor.DefaultLine,
            SavedBy = Str(values, "savedBy"),
            ImageryProvider = Str(values, "imgProvider"),
            ImageryResolutionMPerPx = Real(values, "imgRes"),
            ImageryMarginM = Real(values, "imgMargin"),
            ImageryAreaRatio = Real(values, "imgArea"),
        };
    }

    public static void Write(Database db, GeoSettings settings)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
        var ci = CultureInfo.InvariantCulture;
        var items = new List<TypedValue>
        {
            Key("version"), new TypedValue((int)DxfCode.Int32, Version),
            Key("catalog"), Text(settings.UseCurrentCatalog ? "current" : "legacy"),
            Key("province"), Text(settings.ProvinceName ?? ""),
            Key("unit"), Text(settings.Unit?.ToString() ?? ""),
            Key("output"), Text(settings.Output.ToString().ToLowerInvariant()),
            Key("pcolor"), Text(settings.PointColor),
            Key("lcolor"), Text(settings.LineColor),
            Key("savedBy"), Text("HPGeo " + Entry.Version + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", ci)),
        };
        AddReal(items, "cm", settings.CentralMeridianDeg);
        AddReal(items, "k0", settings.ScaleFactor);
        AddReal(items, "fe", settings.FalseEasting);
        AddReal(items, "fn", settings.FalseNorthing);
        if (settings.ImageryProvider is { Length: > 0 } provider)
        {
            items.Add(Key("imgProvider"));
            items.Add(Text(provider));
        }
        AddReal(items, "imgRes", settings.ImageryResolutionMPerPx);
        AddReal(items, "imgMargin", settings.ImageryMarginM);
        AddReal(items, "imgArea", settings.ImageryAreaRatio);
        var data = new ResultBuffer(items.ToArray());

        if (nod.Contains(GeoSettings.Key) && tr.GetObject(nod.GetAt(GeoSettings.Key), OpenMode.ForWrite) is Xrecord existing)
        {
            existing.Data = data;
        }
        else
        {
            var xrecord = new Xrecord { Data = data };
            nod.SetAt(GeoSettings.Key, xrecord);
            tr.AddNewlyCreatedDBObject(xrecord, true);
        }
        tr.Commit();
    }

    private static KmlOutput ParseOutputLenient(string? text)
    {
        try { return KmlExportOptions.ParseOutput(text ?? "both"); }
        catch (ArgumentException) { return KmlOutput.Both; }
    }

    private static TypedValue Key(string name) => Text(K + name);
    private static TypedValue Text(string value) => new((int)DxfCode.Text, value);
    /// <summary>A missing number is simply absent — never a NaN in the DWG.</summary>
    private static void AddReal(List<TypedValue> items, string name, double? value)
    {
        if (value is { } v && double.IsFinite(v))
        {
            items.Add(Key(name));
            items.Add(new TypedValue((int)DxfCode.Real, v));
        }
    }

    private static string? Str(Dictionary<string, object> values, string key) =>
        values.TryGetValue(key, out var v) && v is string s && s.Length > 0 ? s : null;

    private static double? Real(Dictionary<string, object> values, string key) =>
        values.TryGetValue(key, out var v) && v is double d && double.IsFinite(d) ? d : null;
}
