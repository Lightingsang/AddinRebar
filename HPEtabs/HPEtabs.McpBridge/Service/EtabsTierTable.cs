using System.IO;
using System.Reflection;

namespace HPEtabs.McpBridge.Service;

/// <summary>What a script may do to the model, decided before it runs: the highest tier of any member it calls.</summary>
public enum EtabsTier
{
    /// <summary>Only reads (or touches view/selection/result output setup). Runs under `transaction: none`.</summary>
    ReadOnly = 0,

    /// <summary>Changes definitions, assignments or objects. Runs under `auto` after the bridge saved the model and copied a snapshot.</summary>
    Write = 1,

    /// <summary>Unlocks, analyses, opens/saves/exports/imports files or deletes. Needs the second opt-in.</summary>
    Destructive = 2,
}

/// <summary>One row of the tier fixture: the tier and, for path-taking members, which parameters name a file.</summary>
public sealed record TierEntry(EtabsTier Tier, IReadOnlyList<int> PathParameters);

/// <summary>
///     The allow-list every OAPI call is looked up in: <c>cInterface.Member → R|W|D</c>, one row per method of every
///     ETABSv1 interface, generated once from the installed wrapper by <c>tools/generate-oapi-tier-fixture.ps1</c>
///     and reviewed by hand (<c>Resources/etabs-oapi-tiers.txt</c>). A member that is not here is destructive: the
///     analyzer fails closed rather than guessing from a name.
/// </summary>
public sealed class EtabsTierTable
{
    public const string FixtureResource = "HPEtabs.McpBridge.Resources.etabs-oapi-tiers.txt";
    public const string IndexResource = "HPEtabs.McpBridge.Resources.etabs-oapi-index.txt";

    private static readonly Lazy<EtabsTierTable> EmbeddedTable = new(() => Parse(ReadResource(FixtureResource)));

    private readonly Dictionary<string, TierEntry> _entries;

    private EtabsTierTable(Dictionary<string, TierEntry> entries) => _entries = entries;

    /// <summary>The fixture compiled into this assembly.</summary>
    public static EtabsTierTable Embedded => EmbeddedTable.Value;

    public int Count => _entries.Count;

    public IEnumerable<KeyValuePair<string, TierEntry>> Entries => _entries;

    /// <summary>Parses fixture lines: `cInterface.Member<TAB>R|W|D[<TAB>path=i,j]`; `#` comments and blank lines are skipped.</summary>
    public static EtabsTierTable Parse(IEnumerable<string> lines)
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

            var tier = parts[1] switch
            {
                "R" => EtabsTier.ReadOnly,
                "W" => EtabsTier.Write,
                "D" => EtabsTier.Destructive,
                _ => throw new FormatException($"tier fixture line {number}: tier must be R, W or D, got '{parts[1]}'"),
            };

            var paths = Array.Empty<int>();
            if (parts.Length > 2 && parts[2].StartsWith("path=", StringComparison.Ordinal))
                paths = parts[2]["path=".Length..].Split(',').Select(int.Parse).ToArray();

            if (!entries.TryAdd(parts[0], new TierEntry(tier, paths)))
                throw new FormatException($"tier fixture line {number}: duplicate member '{parts[0]}'");
        }

        return new EtabsTierTable(entries);
    }

    public bool TryGet(string interfaceName, string member, out TierEntry entry) => _entries.TryGetValue(interfaceName + "." + member, out entry!);

    /// <summary>The documentation's member topics, one per overload group, as committed beside the fixture.</summary>
    public static IReadOnlyList<string> EmbeddedIndex() =>
        ReadResource(IndexResource).Select(l => l.Trim()).Where(l => l.Length > 0 && l[0] != '#').ToArray();

    private static IEnumerable<string> ReadResource(string name)
    {
        using var stream = typeof(EtabsTierTable).Assembly.GetManifestResourceStream(name)
                           ?? throw new FileNotFoundException("embedded resource missing: " + name);
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);
        return lines;
    }
}
