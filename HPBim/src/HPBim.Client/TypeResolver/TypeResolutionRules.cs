namespace HPBim.Client.TypeResolver;

/// <summary>
///     Standards and naming conventions for Vietnamese residential construction element types.
/// </summary>
public static class TypeResolutionRules
{
    public const double DefaultWallThickness110Mm = 110.0;
    public const double DefaultWallThickness220Mm = 220.0;
    public const double DefaultColumnSizeMm = 220.0;
    public const double DefaultBeamWidthMm = 200.0;
    public const double DefaultBeamHeightMm = 400.0;
    public const double DefaultSlabThicknessMm = 100.0;

    public static string GetWallTypeName(double thicknessMm) =>
        $"HP_Tuong_{(int)Math.Round(thicknessMm)}mm";

    public static string GetColumnSymbolName(double widthMm, double depthMm) =>
        $"HP_Cot_C{(int)Math.Round(widthMm)}x{(int)Math.Round(depthMm)}";

    public static string GetBeamSymbolName(double widthMm, double heightMm) =>
        $"HP_Dam_D{(int)Math.Round(widthMm)}x{(int)Math.Round(heightMm)}";

    public static string GetFloorTypeName(double thicknessMm) =>
        $"HP_San_{(int)Math.Round(thicknessMm)}mm";

    public static string GetDoorSymbolName(double widthMm, double heightMm) =>
        $"HP_CuaDi_{(int)Math.Round(widthMm)}x{(int)Math.Round(heightMm)}";

    public static string GetWindowSymbolName(double widthMm, double heightMm) =>
        $"HP_CuaSo_{(int)Math.Round(widthMm)}x{(int)Math.Round(heightMm)}";

    public static string GetPipeTypeName(string? material = null) =>
        string.IsNullOrWhiteSpace(material) ? "Standard" : material!;

    public static string GetPipingSystemTypeName(HPBim.Core.Models.PipeSystemType systemType) => systemType switch
    {
        HPBim.Core.Models.PipeSystemType.WaterSupply => "Domestic Cold Water",
        HPBim.Core.Models.PipeSystemType.SoilDrain => "Sanitary",
        HPBim.Core.Models.PipeSystemType.WasteDrain => "Sanitary",
        _ => "Domestic Cold Water"
    };
}
