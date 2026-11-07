using HPFoundation.Core.Model;

namespace HPFoundation.Core.Modeling;

public enum SpringKind
{
    None,
    Interior,
    End,
}

/// <summary>One node along the footing axis; a column load sits on the node at its axis.</summary>
public sealed record ModelNode(double XM, SpringKind Spring, ColumnLoad? Load);

/// <summary>How the flange depth of the sheet divides into the edge and the haunch; the sheet does not say.</summary>
public sealed record FlangeProfile(double EdgeThicknessM, double HaunchHeightM)
{
    private const double UsualEdgeThicknessM = 0.3;

    public static FlangeProfile DefaultFor(double flangeDepthM) =>
        new(UsualEdgeThicknessM, flangeDepthM - UsualEdgeThicknessM);
}

/// <summary>Inverted tee: edge plate, haunch rising to the stem, stem above.</summary>
public sealed record TeeSection(
    double FootingWidthM,
    double BeamWidthM,
    double BeamDepthM,
    double StemHeightM,
    double HaunchHeightM,
    double EdgeThicknessM)
{
    public double AreaM2 =>
        BeamWidthM * StemHeightM
        + (FootingWidthM + BeamWidthM) / 2 * HaunchHeightM
        + FootingWidthM * EdgeThicknessM;
}

/// <summary>The analysis model of one footing, decided here so it can be tested without ETABS.</summary>
public sealed record ModelSpec(
    string SheetName,
    IReadOnlyList<ModelNode> Nodes,
    TeeSection Section,
    double ConcreteModulusTPerM2,
    double InteriorStiffnessTPerM,
    double EndStiffnessTPerM);
