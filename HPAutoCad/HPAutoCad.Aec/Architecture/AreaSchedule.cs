namespace HPAutoCad.Aec.Architecture;

/// <summary>One line of an area schedule: a group (department, name or one room), its rooms, area and share of the total.</summary>
public sealed record AreaRow(string Group, int Count, double AreaMm2, double AreaM2, double Percent, IReadOnlyList<string> Rooms);

/// <summary>Rooms grouped and totalled — by department, by name, or one row per room. No room standard: the groups are whatever the labels said. Pure.</summary>
public static class AreaSchedule
{
    public static readonly IReadOnlyList<string> GroupBy = ["room", "name", "department"];

    /// <summary>Rooms listed per row; the count and area are exact whatever the cap.</summary>
    public const int MaxRoomsPerRow = 20;

    public const string Ungrouped = "(none)";

    public static IReadOnlyList<AreaRow> Build(IReadOnlyList<Room> rooms, string groupBy)
    {
        var key = (groupBy ?? "room").Trim().ToLowerInvariant();
        if (!GroupBy.Contains(key)) throw new ArgumentException($"groupBy must be one of {string.Join(", ", GroupBy)}.");
        var total = rooms.Sum(r => r.AreaMm2);
        // "office" / "Office" / "OFFICE" are one group under one spelling, and the rooms of a row are listed in id order
        Func<Room, string> group = key switch
        {
            "name" => r => r.Name?.Trim().ToUpperInvariant() ?? Ungrouped,
            "department" => r => r.Department?.Trim().ToUpperInvariant() ?? Ungrouped,
            _ => r => r.Id,
        };
        var rows = rooms.GroupBy(group, StringComparer.Ordinal)
            .Select(g => new AreaRow(g.Key, g.Count(), Math.Round(g.Sum(r => r.AreaMm2), 1), Math.Round(g.Sum(r => r.AreaMm2) / 1_000_000, 2), total > 0 ? Math.Round(100 * g.Sum(r => r.AreaMm2) / total, 1) : 0,
                g.OrderBy(r => r.Id, StringComparer.Ordinal).Select(RoomLabel).Take(MaxRoomsPerRow).ToArray()));
        return key == "room"
            ? rows.OrderBy(r => rooms.First(x => x.Id == r.Group).Id, StringComparer.Ordinal).ToArray()
            : rows.OrderByDescending(r => r.AreaMm2).ThenBy(r => r.Group, StringComparer.Ordinal).ToArray();
    }

    private static string RoomLabel(Room r) => r.Number is not null && r.Name is not null ? $"{r.Number} {r.Name}" : r.Number ?? r.Name ?? r.Id;
}
