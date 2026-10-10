using System.Text.Json;

namespace HPNavis.BIMCoordinator.Rules;

/// <summary>
///     The company clash matrix loaded once from the embedded resource and checked on load: 21 distinct
///     groups, every rule naming two known groups, one rule per unordered pair, priorities 1..3. A matrix that
///     fails these checks throws — a wrong standard must never turn into clash tests.
/// </summary>
public sealed class ClashMatrix
{
    public const string ResourceName = "HPNavis.BIMCoordinator.hp-clash-matrix.json";
    public static readonly string[] KnownLods = { "200", "300", "350", "400" };

    private static readonly Lazy<ClashMatrix> Embedded = new(() => Parse(ReadResource(ResourceName)));
    private readonly Dictionary<string, ElementGroup> _groups;

    private ClashMatrix(ClashMatrixDocument document)
    {
        Document = document;
        _groups = document.Groups.ToDictionary(g => g.Code, StringComparer.Ordinal);
    }

    public static ClashMatrix Default => Embedded.Value;

    public ClashMatrixDocument Document { get; }

    public IReadOnlyList<ClashRule> Rules => Document.Rules;

    public IReadOnlyList<ElementGroup> Groups => Document.Groups;

    public ElementGroup Group(string code) =>
        _groups.TryGetValue(code, out var group) ? group : throw new ArgumentException($"'{code}' is not a group of the clash matrix (A1..A9, S1..S5, M1..M7)");

    /// <summary>Tolerance of a LOD in millimetres; unknown or not yet approved (LOD 400) is the caller's error.</summary>
    public double ToleranceMmFor(string lod)
    {
        RequireKnownLod(lod);
        if (!Document.ToleranceMm.TryGetValue(lod, out var tolerance) || tolerance is null)
            throw new ArgumentException($"LOD {lod} has no approved tolerance in the HP clash matrix; it cannot be run until the BIM lead sets one");
        return tolerance.Value;
    }

    /// <summary>The rules checked at a LOD, optionally narrowed to priorities and rule ids, in matrix order.</summary>
    public IReadOnlyList<ClashRule> RulesFor(string lod, IReadOnlyCollection<int>? priorities = null, IReadOnlyCollection<string>? ruleIds = null)
    {
        ToleranceMmFor(lod);
        var badPriorities = priorities?.Where(p => p is < 1 or > 3).ToList();
        if (badPriorities is { Count: > 0 }) throw new ArgumentException($"priority {string.Join(", ", badPriorities)} is not 1 (HIGH), 2 (MEDIUM) or 3 (LOW)");
        var unknown = ruleIds?.Where(id => Rules.All(r => r.Id != id)).ToList();
        if (unknown is { Count: > 0 }) throw new ArgumentException($"unknown rule id(s): {string.Join(", ", unknown)}");
        return Rules
            .Where(r => r.IsEligibleAt(lod))
            .Where(r => priorities is not { Count: > 0 } || priorities.Contains(r.Priority))
            .Where(r => ruleIds is not { Count: > 0 } || ruleIds.Contains(r.Id))
            .ToList();
    }

    public static void RequireKnownLod(string lod)
    {
        if (!KnownLods.Contains(lod)) throw new ArgumentException($"lod '{lod}' is not one of {string.Join(", ", KnownLods)}");
    }

    public static ClashMatrix Parse(string json)
    {
        var document = JsonSerializer.Deserialize<ClashMatrixDocument>(json)
                       ?? throw new InvalidOperationException("clash matrix JSON is empty");
        Validate(document);
        return new ClashMatrix(document);
    }

    public static string ReadResource(string name)
    {
        using var stream = typeof(ClashMatrix).Assembly.GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException($"embedded resource {name} is missing from {typeof(ClashMatrix).Assembly.GetName().Name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void Validate(ClashMatrixDocument document)
    {
        var codes = document.Groups.Select(g => g.Code).ToList();
        if (codes.Count != 21 || codes.Distinct(StringComparer.Ordinal).Count() != 21)
            throw new InvalidOperationException($"clash matrix must define 21 distinct groups, found {codes.Count}");
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in document.Rules)
        {
            if (!codes.Contains(rule.Left) || !codes.Contains(rule.Right))
                throw new InvalidOperationException($"rule {rule.Id} names an unknown group ({rule.Left}, {rule.Right})");
            if (rule.Priority is < 1 or > 3)
                throw new InvalidOperationException($"rule {rule.Id} has priority {rule.Priority}, expected 1..3");
            if (!pairs.Add(string.CompareOrdinal(rule.Left, rule.Right) <= 0 ? rule.Left + "|" + rule.Right : rule.Right + "|" + rule.Left))
                throw new InvalidOperationException($"pair {rule.Left}-{rule.Right} appears twice");
        }
    }
}
