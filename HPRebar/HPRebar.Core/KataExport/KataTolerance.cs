namespace HPRebar.Core.KataExport;

/// <summary>
/// Distances in millimetres used when comparing stations along a beam axis.
/// </summary>
public static class KataTolerance
{
    /// <summary>Two stations closer than this are the same point (support faces, element ends).</summary>
    public const double StationMm = 1.0;

    /// <summary>A span shorter than this is a gap between touching supports, not a span.</summary>
    public const double MinimumSpanMm = 1.0;

    /// <summary>
    /// An element joint this close to a support face belongs to the support: elements are usually split at
    /// the column face or centre, and a joint a few millimetres outside the face would otherwise leave a
    /// sliver span in the sheet.
    /// </summary>
    public const double JointSnapMm = 50.0;
}
