namespace HPRebar.Core.Tcvn5574.Models;

/// <summary>
/// Summary report model of beam structural design and verification per TCVN 5574:2018.
/// Ready for Excel report export or UI display.
/// </summary>
public sealed record BeamCalculationReportModel
{
    public string BeamMark { get; init; } = string.Empty;
    public int SpanIndex { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public double LengthMm { get; init; }
    public string ConcreteGrade { get; init; } = string.Empty;
    public string LongitudinalSteelGrade { get; init; } = string.Empty;
    public string StirrupSteelGrade { get; init; } = string.Empty;

    // Flexural design results at 3 key stations
    public FlexureDesignResult? FlexureLeft { get; init; }
    public FlexureDesignResult? FlexureMid { get; init; }
    public FlexureDesignResult? FlexureRight { get; init; }

    // Shear design results at 3 key stations
    public ShearDesignResult? ShearLeft { get; init; }
    public ShearDesignResult? ShearMid { get; init; }
    public ShearDesignResult? ShearRight { get; init; }

    // Selected reinforcement arrangements
    public RebarOption? TopSupportRebar { get; init; }
    public RebarOption? BottomSpanRebar { get; init; }
    public string StirrupDescription { get; init; } = string.Empty;

    /// <summary>Overall compliance status: "ĐẠT" or "NG" (Không đạt).</summary>
    public string Status => IsCompliant ? "ĐẠT" : "NG";

    /// <summary>True if all flexure, shear, and spacing criteria are satisfied.</summary>
    public bool IsCompliant { get; init; }

    /// <summary>Summary notes, warnings or reasons for non-compliance.</summary>
    public string Notes { get; init; } = string.Empty;
}
