using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPGeo.Core.Catalog;

/// <summary>
/// The province → central-meridian table, read from the JSON that tools/extract-province-data.js generated
/// from the reference tool. Two catalogues: <see cref="Current"/> (34, post-2025) and <see cref="Legacy"/> (63).
/// For a current province with several meridians, <see cref="FormerProvinceLabel"/> names the former province
/// each meridian came from (e.g. TP. Hồ Chí Minh · 107°45′ → "Bà Rịa - Vũng Tàu cũ").
/// </summary>
public sealed class ProvinceCatalog
{
    private const string ResourceName = "HPGeo.Core.Data.vn2000-provinces.json";

    private readonly Dictionary<string, Dictionary<string, string>> _origins;

    private ProvinceCatalog(IReadOnlyList<Province> current, IReadOnlyList<Province> legacy,
        Dictionary<string, Dictionary<string, string>> origins, IReadOnlyList<double> meridians, string source)
    {
        Current = current;
        Legacy = legacy;
        _origins = origins;
        CentralMeridians = meridians;
        Source = source;
    }

    public IReadOnlyList<Province> Current { get; }
    public IReadOnlyList<Province> Legacy { get; }

    /// <summary>Every distinct central meridian across both catalogues, ascending.</summary>
    public IReadOnlyList<double> CentralMeridians { get; }

    /// <summary>File name of the reference tool the table was extracted from.</summary>
    public string Source { get; }

    public static ProvinceCatalog Default { get; } = LoadEmbedded();

    public IReadOnlyList<Province> Get(ProvinceCatalogKind kind) => kind == ProvinceCatalogKind.Current ? Current : Legacy;

    public Province? Find(ProvinceCatalogKind kind, string name) =>
        Get(kind).FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The former province a current province's meridian comes from, or "" when there is no such note.</summary>
    public string FormerProvinceLabel(string currentProvinceName, double centralMeridian)
    {
        if (!_origins.TryGetValue(currentProvinceName, out var byMeridian)) return "";
        return byMeridian.TryGetValue(MeridianKey(centralMeridian), out var label) ? label : "";
    }

    // JSON keys are the JS Number → String form ("105.5", "105", "104.75").
    private static string MeridianKey(double cm) => cm.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

    private static ProvinceCatalog LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
        return Load(stream);
    }

    public static ProvinceCatalog Load(Stream json)
    {
        var doc = JsonSerializer.Deserialize<CatalogJson>(json, JsonOptions)
            ?? throw new InvalidOperationException("Province catalogue JSON is empty.");
        if (doc.Current is null || doc.Legacy is null || doc.Origins is null)
            throw new InvalidOperationException("Province catalogue JSON lacks current/legacy/origins.");

        var current = doc.Current.Select(p => ToProvince(p, ProvinceCatalogKind.Current)).ToList();
        var legacy = doc.Legacy.Select(p => ToProvince(p, ProvinceCatalogKind.Legacy)).ToList();
        var meridians = current.Concat(legacy).SelectMany(p => p.CentralMeridians).Distinct().OrderBy(x => x).ToList();
        return new ProvinceCatalog(current, legacy, doc.Origins, meridians, doc.Source ?? "");
    }

    private static Province ToProvince(ProvinceJson p, ProvinceCatalogKind kind)
    {
        if (string.IsNullOrWhiteSpace(p.Name) || p.Cms is null || p.Cms.Count == 0)
            throw new InvalidOperationException($"Province catalogue record '{p.Name}' is incomplete.");
        return new Province(p.Name, p.Cms, kind);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class CatalogJson
    {
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("current")] public List<ProvinceJson>? Current { get; set; }
        [JsonPropertyName("legacy")] public List<ProvinceJson>? Legacy { get; set; }
        [JsonPropertyName("origins")] public Dictionary<string, Dictionary<string, string>>? Origins { get; set; }
    }

    private sealed class ProvinceJson
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("cms")] public List<double>? Cms { get; set; }
    }
}
