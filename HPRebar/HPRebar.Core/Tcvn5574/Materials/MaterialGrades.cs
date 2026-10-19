namespace HPRebar.Core.Tcvn5574.Materials;

/// <summary>
/// Standard concrete strength classes per TCVN 5574:2018 Table 6 and Table 7.
/// </summary>
public enum ConcreteGrade
{
    B15 = 15,
    B20 = 20,
    B25 = 25,
    B30 = 30,
    B35 = 35,
    B40 = 40
}

/// <summary>
/// Standard reinforcing steel grades per TCVN 5574:2018 Table 13, Table 14, and Table 15.
/// </summary>
public enum SteelGrade
{
    CB240T = 240,
    CB300V = 300,
    CB400V = 400,
    CB500V = 500
}

/// <summary>
/// Utility methods for material grade string parsing and formatting.
/// </summary>
public static class MaterialGradeParser
{
    /// <summary>
    /// Parses a string into a <see cref="ConcreteGrade"/>.
    /// Supports formats like "B15", "B20", "B25", "B30", "B35", "B40", "M200", "M250", "M350".
    /// </summary>
    public static bool TryParseConcrete(string? input, out ConcreteGrade grade)
    {
        grade = ConcreteGrade.B25;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input!.Trim().ToUpperInvariant();
        switch (normalized)
        {
            case "B15":
            case "M200":
                grade = ConcreteGrade.B15;
                return true;
            case "B20":
            case "M250":
                grade = ConcreteGrade.B20;
                return true;
            case "B25":
            case "M350":
                grade = ConcreteGrade.B25;
                return true;
            case "B30":
            case "M400":
                grade = ConcreteGrade.B30;
                return true;
            case "B35":
            case "M450":
                grade = ConcreteGrade.B35;
                return true;
            case "B40":
            case "M500":
                grade = ConcreteGrade.B40;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Parses a string into a <see cref="SteelGrade"/>.
    /// Supports formats like "CB240-T", "CB240T", "CB300-V", "CB300V", "CB400-V", "CB400V", "CB500-V", "CB500V".
    /// </summary>
    public static bool TryParseSteel(string? input, out SteelGrade grade)
    {
        grade = SteelGrade.CB400V;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input!.Trim().Replace("-", "").ToUpperInvariant();
        switch (normalized)
        {
            case "CB240T":
            case "CB240":
                grade = SteelGrade.CB240T;
                return true;
            case "CB300V":
            case "CB300":
                grade = SteelGrade.CB300V;
                return true;
            case "CB400V":
            case "CB400":
                grade = SteelGrade.CB400V;
                return true;
            case "CB500V":
            case "CB500":
                grade = SteelGrade.CB500V;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Formats a concrete grade into standard Vietnamese notation (e.g. "B25").
    /// </summary>
    public static string FormatConcrete(ConcreteGrade grade) => grade switch
    {
        ConcreteGrade.B15 => "B15",
        ConcreteGrade.B20 => "B20",
        ConcreteGrade.B25 => "B25",
        ConcreteGrade.B30 => "B30",
        ConcreteGrade.B35 => "B35",
        ConcreteGrade.B40 => "B40",
        _ => grade.ToString()
    };

    /// <summary>
    /// Formats a steel grade into standard Vietnamese notation (e.g. "CB400-V").
    /// </summary>
    public static string FormatSteel(SteelGrade grade) => grade switch
    {
        SteelGrade.CB240T => "CB240-T",
        SteelGrade.CB300V => "CB300-V",
        SteelGrade.CB400V => "CB400-V",
        SteelGrade.CB500V => "CB500-V",
        _ => grade.ToString()
    };
}
