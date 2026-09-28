using System;

namespace HPRebar.KataRebar.Model;

/// <summary>
/// Status and execution statistics resulting from generating 3D rebar elements in Revit.
/// </summary>
public sealed class KataRebarGenerationResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = "";
    public int DeletedOldBarsCount { get; init; }
    public int CreatedMainBarsCount { get; init; }
    public int CreatedExtraBarsCount { get; init; }
    public int CreatedSideBarsCount { get; init; }
    public int CreatedStirrupsCount { get; init; }

    public int TotalCreatedBarsCount =>
        CreatedMainBarsCount + CreatedExtraBarsCount + CreatedSideBarsCount + CreatedStirrupsCount;
}
