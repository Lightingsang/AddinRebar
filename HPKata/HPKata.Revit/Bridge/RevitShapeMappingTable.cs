namespace HPKata.Revit.Bridge;

/// <summary>
/// Static mapping table between standard Revit rebar shape names and Kata 85 shapes (dang00..dang67).
/// </summary>
public static class RevitShapeMappingTable
{
    private static readonly Dictionary<string, string> RevitToKata =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "M_00", "dang01" },
            { "00", "dang01" },
            { "REBAR SHAPE 00", "dang01" },
            { "STRAIGHT", "dang01" },
            { "M_02", "dang03" },
            { "02", "dang03" },
            { "REBAR SHAPE 02", "dang03" },
            { "M_11", "dang22" },
            { "11", "dang22" },
            { "REBAR SHAPE 11", "dang22" },
            { "M_51", "dang22" },
            { "51", "dang22" },
            { "REBAR SHAPE 51", "dang22" },
            { "M_T1", "dang02" },
            { "T1", "dang02" },
            { "REBAR SHAPE T1", "dang02" },
            { "M_77", "dang40" },
            { "77", "dang40" },
            { "REBAR SHAPE 77", "dang40" },
            { "M_21", "dang02" },
            { "21", "dang02" },
            { "REBAR SHAPE 21", "dang02" }
        };

    private static readonly Dictionary<string, string> KataToRevit =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "dang01", "M_00" },
            { "01", "M_00" },
            { "dang02", "M_T1" },
            { "02", "M_T1" },
            { "dang03", "M_02" },
            { "03", "M_02" },
            { "dang04", "M_21" },
            { "04", "M_21" },
            { "dang22", "M_11" },
            { "22", "M_11" },
            { "dang40", "M_77" },
            { "40", "M_77" }
        };

    /// <summary>Attempts to map a Revit shape name to a Kata shape code.</summary>
    public static bool TryGetKataShape(string revitShape, out string kataShape) =>
        RevitToKata.TryGetValue(revitShape.Trim(), out kataShape!);

    /// <summary>Attempts to map a Kata shape code to a standard Revit shape name.</summary>
    public static bool TryGetRevitShape(string kataShape, out string revitShape) =>
        KataToRevit.TryGetValue(kataShape.Trim(), out revitShape!);
}
