using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// B01 as Kata drew it at TL 1/25 (T2-DY7.dwg, 2026-10-05), read once from <c>Fixtures/b01-dwg.json</c>: the elevation
/// (origin = outer face of column C at the beam top) and the 14 sections (origin = the title's insertion point, the
/// section's centre line). Every kata_* entity with its points relative to that origin, mm.
/// </summary>
internal static class KataB01DwgFixture
{
    internal sealed record Entity(string Type, string Layer, IReadOnlyList<double[]> Points, string Block, IReadOnlyList<string> Attributes,
        double[] At, double[] Min, double[] Max, double Measurement, double Rotation);

    private static Dictionary<string, List<Entity>>? _views;

    public static IReadOnlyList<Entity> View(string name) => (_views ??= Load())[name];

    public static IEnumerable<string> Views() => (_views ??= Load()).Keys;

    private static Dictionary<string, List<Entity>> Load([CallerFilePath] string source = "")
    {
        string path = Path.Combine(Path.GetDirectoryName(source)!, "Fixtures", "b01-dwg.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var views = new Dictionary<string, List<Entity>>();
        foreach (var view in json.RootElement.GetProperty("views").EnumerateArray())
        {
            var entities = new List<Entity>();
            foreach (var e in Items(view.GetProperty("entities")))
                entities.Add(new Entity(
                    e.GetProperty("type").GetString()!,
                    e.GetProperty("layer").GetString()!,
                    e.TryGetProperty("points", out var p) ? Items(p).Select(Numbers).ToList() : new List<double[]>(),
                    e.TryGetProperty("block", out var b) ? b.GetString() ?? "" : "",
                    e.TryGetProperty("attributes", out var a) ? Items(a).Select(x => x.GetString() ?? "").ToList() : new List<string>(),
                    e.TryGetProperty("at", out var at) ? Numbers(at) : e.TryGetProperty("textAt", out var t) ? Numbers(t) : new double[0],
                    Numbers(e.GetProperty("min")),
                    Numbers(e.GetProperty("max")),
                    e.TryGetProperty("measurement", out var m) ? m.GetDouble() : 0.0,
                    e.TryGetProperty("rotation", out var r) ? r.GetDouble() : 0.0));
            views[view.GetProperty("name").GetString()!] = entities;
        }

        return views;
    }

    // ConvertTo-Json writes a one-item array as the item itself.
    private static IEnumerable<JsonElement> Items(JsonElement e) =>
        e.ValueKind == JsonValueKind.Array ? e.EnumerateArray() : new[] { e };

    private static double[] Numbers(JsonElement e) => e.EnumerateArray().Select(x => x.GetDouble()).ToArray();
}
