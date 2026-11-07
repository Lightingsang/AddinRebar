namespace HPFoundation.Core.Modeling;

/// <summary>
///     Elastic moduli of the company's concrete table, sheet Data of CreateSectionEtabs2022v2.xlsm.
///     Kept here so a run does not depend on that workbook's network drive.
/// </summary>
public static class ConcreteCatalog
{
    private const double TonsPerSquareMetrePerMpa = 1_000_000 / (1000 * 9.80665);

    private static readonly Dictionary<string, double> ElasticModulusByGrade = new(StringComparer.OrdinalIgnoreCase)
    {
        ["B15"] = 24000,
        ["B20"] = 27500,
        ["B22.5"] = 29000,
        ["B25"] = 30000,
        ["B30"] = 32500,
        ["B35"] = 34500,
        ["B40"] = 36000,
        ["B45"] = 37000,
        ["B50"] = 38000,
        ["B60"] = 39500,
    };

    public static double ElasticModulusMpa(string grade) =>
        ElasticModulusByGrade.TryGetValue(grade.Trim(), out var modulusMpa)
            ? modulusMpa
            : throw new ArgumentException($"Concrete grade '{grade}' is not in the company table.", nameof(grade));

    public static double ElasticModulusTPerM2(string grade) => ElasticModulusMpa(grade) * TonsPerSquareMetrePerMpa;
}
