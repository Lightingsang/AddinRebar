namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// What Kata's tab "Thép mặc định" asks for round one kind of load resting on a span (a crossing beam or a stub
/// column): joint stirrups on each face and hanger bars ("vai bò") under it.
/// </summary>
public sealed record KataJointRebarSettings
{
    public static readonly KataJointRebarSettings Default = new();

    /// <summary>Joint stirrups on each face of the load, "count f diameter a spacing" ("5f8a50"); a count of 0 draws none.</summary>
    public string Stirrups { get; init; } = "5f8a50";

    /// <summary>Kata's "Spec số lượng đai" (None, H1, H, W, a number, W+H-1…): kept for later, not applied yet.</summary>
    public string StirrupCountSpec { get; init; } = "None";

    /// <summary>Kata's "Đai băng qua": the span's own stirrups carry on through the load. Kept for later, not applied yet.</summary>
    public bool StirrupsThroughJoint { get; init; }

    /// <summary>Whether hanger bars are drawn under the load.</summary>
    public bool HangerEnabled { get; init; } = true;

    /// <summary>Hanger bars, "count f diameter" ("2f16").</summary>
    public string Hanger { get; init; } = "2f16";

    /// <summary>Level run of a hanger bar at the top past each slope (mm).</summary>
    public double HangerTopLengthMm { get; init; } = 150.0;

    /// <summary>Slope of a hanger bar from the horizontal: 45 or 60 degrees.</summary>
    public int HangerAngleDegrees { get; init; } = 45;
}
