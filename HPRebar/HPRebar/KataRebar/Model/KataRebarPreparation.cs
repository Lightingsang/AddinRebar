using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataRebar.Model;

/// <summary>
/// A sheet planned on the beams Revit measured: what the preview shows and what generation draws. The match
/// holds Revit objects, so a preparation is used on the API thread that made it; the plan alone may leave it.
/// </summary>
public sealed class KataRebarPreparation
{
    public KataRebarPreparation(KataBeamMatchResult match, KataRebarPlan? plan)
    {
        Match = match;
        Plan = plan;
    }

    public KataBeamMatchResult Match { get; }

    /// <summary>Null when the beams could not be measured (<see cref="KataBeamMatchResult.Message"/> says why).</summary>
    public KataRebarPlan? Plan { get; }

    public bool CanGenerate => Match.IsSuccess && Plan is { CanGenerate: true };
}
