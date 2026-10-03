namespace HPRebar.Core.Shared;

/// <summary>Limits Revit enforces on rebar elements, checked in Core before any transaction opens.</summary>
public static class RevitRebarLimits
{
    /// <summary>Most bar positions one rebar set may hold; Revit's layout call refuses a larger set.</summary>
    public const int MaxBarPositions = 1002;
}
