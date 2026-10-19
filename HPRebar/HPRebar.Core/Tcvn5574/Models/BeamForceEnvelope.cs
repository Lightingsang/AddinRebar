using System;

namespace HPRebar.Core.Tcvn5574.Models;

/// <summary>
/// Envelope of design internal forces (bending moment and shear) for a beam span.
/// Extracted from structural calculation results (Excel schedule or ETABS analysis).
/// </summary>
public sealed record BeamForceEnvelope
{
    /// <summary>Beam label or mark (e.g., "D1", "B-101").</summary>
    public string BeamMark { get; init; } = string.Empty;

    /// <summary>Zero-based index of this span along the continuous beam.</summary>
    public int SpanIndex { get; init; }

    /// <summary>Span length in millimetres.</summary>
    public double SpanLengthMm { get; init; }

    /// <summary>Design bending moment at left support |M_left| (kNm, top tension).</summary>
    public double MomentSupportLeftKnm { get; init; }

    /// <summary>Design bending moment at mid-span |M_mid| (kNm, bottom tension).</summary>
    public double MomentMidSpanKnm { get; init; }

    /// <summary>Design bending moment at right support |M_right| (kNm, top tension).</summary>
    public double MomentSupportRightKnm { get; init; }

    /// <summary>Design shear force at left support |Q_left| (kN).</summary>
    public double ShearSupportLeftKn { get; init; }

    /// <summary>Design shear force at mid-span |Q_mid| (kN).</summary>
    public double ShearMidSpanKn { get; init; }

    /// <summary>Design shear force at right support |Q_right| (kN).</summary>
    public double ShearSupportRightKn { get; init; }

    /// <summary>Maximum absolute design moment across the entire span (kNm).</summary>
    public double MaxMomentKnm => Math.Max(
        Math.Abs(MomentMidSpanKnm),
        Math.Max(Math.Abs(MomentSupportLeftKnm), Math.Abs(MomentSupportRightKnm)));

    /// <summary>Maximum absolute design shear across the entire span (kN).</summary>
    public double MaxShearKn => Math.Max(
        Math.Abs(ShearMidSpanKn),
        Math.Max(Math.Abs(ShearSupportLeftKn), Math.Abs(ShearSupportRightKn)));
}
