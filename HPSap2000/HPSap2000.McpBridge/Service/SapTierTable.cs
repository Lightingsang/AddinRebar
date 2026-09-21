using System.IO;
using System.Reflection;

namespace HPSap2000.McpBridge.Service;

/// <summary>What a script may do to the model, decided before it runs: the highest tier of any member it calls.</summary>
public enum SapTier
{
    /// <summary>Only reads (or touches view/selection/result output setup). Runs under `transaction: none`.</summary>
    ReadOnly = 0,

    /// <summary>Changes definitions, assignments or objects. Runs under `auto` after the bridge saved the model and copied a snapshot.</summary>
    Write = 1,

    /// <summary>Unlocks, analyses, opens/saves/exports/imports files or deletes. Needs the second opt-in.</summary>
    Destructive = 2,
}

/// <summary>One row of the tier fixture: the tier and, for path-taking members, which parameters name a file.</summary>
public sealed record TierEntry(SapTier Tier, IReadOnlyList<int> PathParameters);

/// <summary>
///     The allow-list every OAPI call is looked up in: <c>cInterface.Member → R|W|D</c>, one row per method of every
///     SAP2000v1 interface (<c>Resources/sap2000-oapi-tiers.txt</c>). A member that is not here is destructive: the
///     analyzer fails closed rather than guessing from a name.
/// </summary>
public sealed class SapTierTable
{
    public const string FixtureResource = "HPSap2000.McpBridge.Resources.sap2000-oapi-tiers.txt";

    private static readonly Lazy<SapTierTable> EmbeddedTable = new(() => Parse(ReadResource(FixtureResource)));

    private readonly Dictionary<string, TierEntry> _entries;

    private SapTierTable(Dictionary<string, TierEntry> entries) => _entries = entries;

    /// <summary>The fixture compiled into this assembly.</summary>
    public static SapTierTable Embedded => EmbeddedTable.Value;

    public int Count => _entries.Count;

    public IEnumerable<KeyValuePair<string, TierEntry>> Entries => _entries;

    /// <summary>Parses fixture lines: `cInterface.Member<TAB>R|W|D[<TAB>path=i,j]`; `#` comments and blank lines are skipped.</summary>
    public static SapTierTable Parse(IEnumerable<string> lines)
    {
        var entries = new Dictionary<string, TierEntry>(StringComparer.Ordinal);
        var number = 0;

        foreach (var raw in lines)
        {
            number++;
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            var parts = line.Split('\t');
            if (parts.Length < 2) throw new FormatException($"tier fixture line {number}: expected 'cInterface.Member<TAB>tier', got '{line}'");

            var key = parts[0].Trim();
            var tier = parts[1].Trim() switch
            {
                "R" => SapTier.ReadOnly,
                "W" => SapTier.Write,
                "D" => SapTier.Destructive,
                var other => throw new FormatException($"tier fixture line {number} ({key}): unknown tier '{other}'"),
            };

            var paths = Array.Empty<int>();
            if (parts.Length >= 3 && parts[2].StartsWith("path="))
            {
                var spec = parts[2].Substring("path=".Length);
                paths = spec.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => int.TryParse(p.Trim(), out var i) ? i : throw new FormatException($"tier fixture line {number} ({key}): bad path index '{p}'"))
                    .ToArray();
            }

            if (!entries.TryAdd(key, new TierEntry(tier, paths)))
                throw new FormatException($"tier fixture line {number}: duplicate member '{key}'");
        }

        return new SapTierTable(entries);
    }

    public bool TryGet(string interfaceName, string member, out TierEntry entry) =>
        _entries.TryGetValue(interfaceName + "." + member, out entry!);

    /// <summary>Look up a member; unlisted members are Destructive (fail closed).</summary>
    public TierEntry Lookup(string interfaceName, string memberName) =>
        _entries.TryGetValue(interfaceName + "." + memberName, out var entry)
            ? entry
            : new TierEntry(SapTier.Destructive, Array.Empty<int>());

    private static IEnumerable<string> ReadResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded tier fixture '{resourceName}' not found; available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null) yield return line;
    }
}
