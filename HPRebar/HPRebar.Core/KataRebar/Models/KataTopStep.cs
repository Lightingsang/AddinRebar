namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// A change of the beam top inside one span: from <see cref="AtMm"/> (mm from the span's start) the top sits at
/// <see cref="TopDrop"/> (row 19, negative = lower). It comes from a support of no width joining two spans whose tops
/// differ (B01: H at −50, J at 0, one span H+J).
/// </summary>
public sealed record KataTopStep(double AtMm, double TopDrop);
