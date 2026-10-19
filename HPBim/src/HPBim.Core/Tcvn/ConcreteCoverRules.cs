namespace HPBim.Core.Tcvn;

public enum StructuralElementKind
{
    Footing,  // Móng, đài cọc, giằng tiếp xúc đất
    Beam,     // Dầm
    Column,   // Cột
    Stair,    // Bản thang
    Slab,     // Bản sàn
    Wall      // Tường BTCT
}

public readonly record struct CoverValidationResult(
    bool IsCompliant,
    double MinimumCoverMm,
    double ActualCoverMm,
    string Message);

/// <summary>
/// Concrete cover rules according to TCVN 5574:2018 Table 19.
/// </summary>
public static class ConcreteCoverRules
{
    public const double FootingCoverMm = 50.0;
    public const double BeamCoverMm = 25.0;
    public const double ColumnCoverMm = 25.0;
    public const double StairCoverMm = 20.0;
    public const double SlabCoverMm = 15.0;
    public const double WallCoverMm = 20.0;

    public static double GetMinimumCoverMm(StructuralElementKind elementKind) => elementKind switch
    {
        StructuralElementKind.Footing => FootingCoverMm,
        StructuralElementKind.Beam => BeamCoverMm,
        StructuralElementKind.Column => ColumnCoverMm,
        StructuralElementKind.Stair => StairCoverMm,
        StructuralElementKind.Slab => SlabCoverMm,
        StructuralElementKind.Wall => WallCoverMm,
        _ => BeamCoverMm
    };

    public static bool IsCoverCompliant(StructuralElementKind elementKind, double actualCoverMm)
    {
        return actualCoverMm >= GetMinimumCoverMm(elementKind);
    }

    public static CoverValidationResult ValidateCover(StructuralElementKind elementKind, double actualCoverMm)
    {
        double minCover = GetMinimumCoverMm(elementKind);
        bool compliant = actualCoverMm >= minCover;
        string message = compliant
            ? $"Lớp bảo vệ {actualCoverMm:F1}mm đạt chuẩn TCVN (>={minCover:F1}mm)."
            : $"Lớp bảo vệ {actualCoverMm:F1}mm vi phạm TCVN 5574:2018 (yêu cầu tối thiểu {minCover:F1}mm cho {elementKind}).";

        return new CoverValidationResult(compliant, minCover, actualCoverMm, message);
    }
}
