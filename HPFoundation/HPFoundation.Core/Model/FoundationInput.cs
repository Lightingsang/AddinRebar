namespace HPFoundation.Core.Model;

/// <summary>Column base forces as the sheet's table gives them, in metric tons and metres.</summary>
public sealed record ColumnLoad(
    string Axis,
    double AxisPositionM,
    double AxialT,
    double MomentXTm,
    double ShearXT,
    double MomentYTm,
    double ShearYT);

/// <summary>Inverted-tee strip section. Edge thickness and haunch height are not in the sheet.</summary>
public sealed record BeamSection(
    double FootingWidthM,
    double BeamWidthM,
    double BeamDepthM,
    double FlangeDepthM,
    double ColumnWidthM);

/// <summary>Soil springs from the sheet's table: one interior spring per spacing, a smaller one at the ends.</summary>
public sealed record SpringSupport(
    double InteriorStiffnessTPerM,
    double EndStiffnessTPerM,
    double InteriorSpacingM,
    double EndTributaryLengthM);

/// <summary>Everything the ETABS model needs from one sheet.</summary>
public sealed record FoundationInput(
    string SheetName,
    IReadOnlyList<ColumnLoad> Columns,
    double FootingLengthM,
    BeamSection Section,
    SpringSupport Springs,
    string ConcreteGrade,
    string SteelGrade);
