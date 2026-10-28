namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Something resting on a span between its supports: a beam framing into it (its soffit higher than this beam's) or a
/// stub column standing on it ("cột cấy"). Kata reinforces it with joint stirrups on both faces and a pair of hanger
/// bars (B01: the beam at mid-D, the stub column on F). The sheet has no cell for it; the Revit model gives it.
/// </summary>
/// <param name="AtMm">Centre of the load from the span's start (mm).</param>
/// <param name="WidthMm">Its width along this beam.</param>
/// <param name="SoffitBelowTopMm">A crossing beam's soffit below this beam's top; 0 for a stub column.</param>
/// <param name="IsColumn">A stub column standing on the beam (the hanger bars then reach this beam's own soffit).</param>
/// <param name="SectionWidthMm">A crossing beam's own width b; 0 when unknown (then <paramref name="WidthMm"/> stands for it).</param>
public sealed record KataSpanLoad(double AtMm, double WidthMm, double SoffitBelowTopMm, bool IsColumn, double SectionWidthMm = 0.0)
{
    public string Kind => IsColumn ? "cột cấy" : "dầm giao";

    /// <summary>The crossing beam's width b, or its width along this beam when the model did not give it.</summary>
    public double BeamWidthMm => SectionWidthMm > 0.0 ? SectionWidthMm : WidthMm;
}
