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
    /// <summary>
    ///     The design phases a model's clash tests follow, one at a time: basic design (200), technical design (300),
    ///     construction drawings (350-400 — the sheet's column G covers both, at one tolerance). "350" and "400" are read
    ///     as "350-400", so tests named the earlier way still belong to their phase.
    /// </summary>
    public static readonly string[] Phases = { "200", "300", "350-400" };

    public const string ConstructionPhase = "350-400";

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

    /// <summary>The phase a LOD belongs to ("350" and "400" → "350-400"); anything else is the caller's error.</summary>
    public static string PhaseOf(string lod) =>
        lod is "350" or "400" or ConstructionPhase ? ConstructionPhase
        : Phases.Contains(lod) ? lod
        : throw new ArgumentException($"lod '{lod}' is not one of {string.Join(", ", Phases)} (200 basic design, 300 technical design, 350-400 construction drawings)");

    /// <summary>The sheet column (JSON key) a phase reads its Y/N and tolerance from: 350-400 → "350".</summary>
    private static string SheetKey(string lod) => PhaseOf(lod) == ConstructionPhase ? "350" : PhaseOf(lod);

    /// <summary>Tolerance of a phase in millimetres; an unknown phase or one without an approved tolerance is the caller's error.</summary>
    public double ToleranceMmFor(string lod)
    {
        var key = SheetKey(lod);
        if (!Document.ToleranceMm.TryGetValue(key, out var tolerance) || tolerance is null)
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
            .Where(r => r.IsEligibleAt(SheetKey(lod)))
            .Where(r => priorities is not { Count: > 0 } || priorities.Contains(r.Priority))
            .Where(r => ruleIds is not { Count: > 0 } || ruleIds.Contains(r.Id))
            .ToList();
    }

    public static void RequireKnownLod(string lod) => PhaseOf(lod);

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
