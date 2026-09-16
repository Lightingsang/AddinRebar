using System.Text.RegularExpressions;
using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Structural;

/// <summary>A mark decided for one member, and why it was (or was not) assigned.</summary>
public sealed record MemberTag(StructuralMember Member, string? Mark, string? ExistingMark, string Outcome)
{
    public const string Assigned = "assigned";
    public const string KeptExisting = "kept_existing";
    /// <summary>The member carries a mark with another prefix (D1 under prefix B); kept untouched unless <c>overwrite</c>, never renumbered silently.</summary>
    public const string KeptForeign = "kept_foreign";
    public const string Overwritten = "overwritten";

    /// <summary>Nothing to write for this member.</summary>
    public bool Kept => Outcome is KeptExisting or KeptForeign;
}

/// <summary>One line of a member schedule: same kind and section, counted and totalled.</summary>
public sealed record ScheduleRow(string Kind, string Section, int Count, double TotalLengthMm, double TotalAreaMm2, IReadOnlyList<string> Marks, IReadOnlyList<string> Handles);

/// <summary>
///     Marks and schedules as pure decisions: which member gets which mark (prefix per kind, sequence, reading order), which
///     ones keep the mark they already carry, and how members group into schedule rows. Writing the text is the adapter's job.
/// </summary>
public static partial class MemberTagging
{
    public static readonly IReadOnlyList<string> SortOrders = ["row", "column", "handle"];

    /// <summary>Handles listed per schedule row; the count is exact whatever the cap.</summary>
    public const int MaxHandlesPerRow = 20;

    /// <summary>Members whose centres differ by less than this along the reading direction sit in the same row (or column).</summary>
    public const double DefaultRowBandMm = 250;

    /// <summary>A mark is letters, an optional hyphen and a number: C1, B12, KC-3. Letters are the prefix, the number is what gets reserved.</summary>
    [GeneratedRegex(@"^(?<prefix>[A-Za-z]{1,3})-?(?<number>\d{1,4})$", RegexOptions.CultureInvariant)]
    public static partial Regex MarkPattern();

    /// <summary>Reading order: <c>row</c> = top-left to bottom-right (y descending in bands, x ascending), <c>column</c> = x then y, <c>handle</c> = drawing order.</summary>
    public static IReadOnlyList<StructuralMember> Order(IEnumerable<StructuralMember> members, string sortBy, double bandMm = DefaultRowBandMm)
    {
        var key = (sortBy ?? "row").Trim().ToLowerInvariant();
        if (!SortOrders.Contains(key)) throw new ArgumentException($"sortBy must be one of {string.Join(", ", SortOrders)}.");
        return key switch
        {
            "column" => Banded(members, m => m.CenterMm.X, bandMm, descending: false).SelectMany(band => band.OrderBy(m => m.CenterMm.Y).ThenBy(m => m.Handle, StringComparer.Ordinal)).ToArray(),
            "handle" => members.OrderBy(m => m.Handle.Length).ThenBy(m => m.Handle, StringComparer.Ordinal).ToArray(),
            _ => Banded(members, m => m.CenterMm.Y, bandMm, descending: true).SelectMany(band => band.OrderBy(m => m.CenterMm.X).ThenBy(m => m.Handle, StringComparer.Ordinal)).ToArray(),
        };
    }

    /// <summary>Bands by gap, not by rounding: a new band starts where consecutive sorted values differ by more than the band, so a row on y = 5 125 never splits at a rounding boundary.</summary>
    private static IEnumerable<List<StructuralMember>> Banded(IEnumerable<StructuralMember> members, Func<StructuralMember, double> value, double bandMm, bool descending)
    {
        var sorted = (descending ? members.OrderByDescending(value) : members.OrderBy(value)).ToList();
        var band = new List<StructuralMember>();
        double? last = null;
        foreach (var m in sorted)
        {
            var v = value(m);
            if (last is { } previous && Math.Abs(v - previous) > bandMm) { yield return band; band = []; }
            band.Add(m);
            last = v;
        }

        if (band.Count > 0) yield return band;
    }

    /// <summary>
    ///     Assigns marks per kind: <c>{prefix}{n}</c> from <paramref name="start"/>, padded to <paramref name="digits"/>. A member whose
    ///     existing mark carries the kind's prefix keeps it and reserves its number (whatever its padding); a mark with another prefix is
    ///     kept as it is — reported <see cref="MemberTag.KeptForeign"/> — unless <paramref name="overwrite"/>, which renumbers everything.
    /// </summary>
    public static IReadOnlyList<MemberTag> Assign(IReadOnlyList<StructuralMember> ordered, IReadOnlyDictionary<string, string> prefixes, int start, int digits, bool overwrite)
    {
        var tags = new List<MemberTag>();
        var next = new Dictionary<string, int>();
        var taken = new HashSet<(string Prefix, int Number)>();
        if (!overwrite)
            foreach (var m in ordered)
                if (Parse(m.Mark) is { } parts && parts.Prefix.Equals(Prefix(prefixes, m.Kind), StringComparison.OrdinalIgnoreCase)) taken.Add((parts.Prefix.ToUpperInvariant(), parts.Number));
        foreach (var m in ordered)
        {
            var prefix = Prefix(prefixes, m.Kind);
            if (!overwrite && m.Mark is not null)
            {
                var own = Parse(m.Mark) is { } parts && parts.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase);
                tags.Add(new MemberTag(m, m.Mark, m.Mark, own ? MemberTag.KeptExisting : MemberTag.KeptForeign));
                continue;
            }

            if (!next.ContainsKey(m.Kind)) next[m.Kind] = start;
            int number;
            do number = next[m.Kind]++;
            while (taken.Contains((prefix.ToUpperInvariant(), number)));
            taken.Add((prefix.ToUpperInvariant(), number));
            tags.Add(new MemberTag(m, prefix + number.ToString().PadLeft(digits, '0'), m.Mark, m.Mark is null ? MemberTag.Assigned : MemberTag.Overwritten));
        }

        return tags;
    }

    /// <summary>Existing marks carried by more than one member ("C2" ×2): kept as they are, but the caller is told.</summary>
    public static IReadOnlyList<string> DuplicateExisting(IEnumerable<StructuralMember> members) => members
        .Where(m => m.Mark is not null).GroupBy(m => m.Mark!, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1)
        .Select(g => $"{g.Key} ×{g.Count()}").OrderBy(x => x, StringComparer.Ordinal).ToArray();

    /// <summary>Rows by kind + section, kinds in the canonical order, sections by count descending then name.</summary>
    public static IReadOnlyList<ScheduleRow> Schedule(IEnumerable<StructuralMember> members) => members
        .GroupBy(m => (m.Kind, m.Section))
        .Select(g => new ScheduleRow(g.Key.Kind, g.Key.Section, g.Count(), Math.Round(g.Sum(m => m.LengthMm ?? 0), 1), Math.Round(g.Sum(m => m.AreaMm2 ?? 0), 1),
            g.Where(m => m.Mark is not null).Select(m => m.Mark!).OrderBy(x => x, StringComparer.Ordinal).ToArray(), g.Select(m => m.Handle).Take(MaxHandlesPerRow).ToArray()))
        .OrderBy(r => MemberKind.Rank(r.Kind)).ThenByDescending(r => r.Count).ThenBy(r => r.Section, StringComparer.Ordinal)
        .ToArray();

    /// <summary>The prefix a kind is tagged with: the caller's, else the default.</summary>
    public static string Prefix(IReadOnlyDictionary<string, string> prefixes, string kind) => prefixes.TryGetValue(kind, out var p) && !string.IsNullOrWhiteSpace(p) ? p.Trim() : MemberKind.DefaultPrefix(kind);

    /// <summary>Letters + number of a mark, or null when the text is not shaped like one.</summary>
    public static (string Prefix, int Number)? Parse(string? mark)
    {
        if (mark is null) return null;
        var m = MarkPattern().Match(mark.Trim());
        return m.Success ? (m.Groups["prefix"].Value, int.Parse(m.Groups["number"].Value)) : null;
    }
}
