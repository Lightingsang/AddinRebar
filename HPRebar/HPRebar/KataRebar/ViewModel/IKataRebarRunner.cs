using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataRebar.ViewModel;

/// <summary>
/// Interface enabling the modeless ViewModel to dispatch thread-safe commands to the Revit main API thread.
/// </summary>
public interface IKataRebarRunner
{
    Task<KataBeamMatchResult?> RepickBeamsAsync(KataBeamRebarSpec spec);

    Task<KataBeamMatchResult?> MatchBeamsAsync(IReadOnlyList<ElementId> beamIds, KataBeamRebarSpec spec);

    Task HighlightBeamsAsync(IReadOnlyList<ElementId> beamIds);

    Task<KataRebarGenerationResult> GenerateRebarAsync(
        KataBeamMatchResult matchResult,
        KataBeamRebarSpec spec,
        KataRebarLayoutResult layout,
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes);
}
