using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataRebar;

public enum KataRebarRequestKind
{
    Repick,
    Match,
    Highlight,
    Generate
}

public sealed class KataRebarRequest
{
    public KataRebarRequestKind Kind { get; }
    public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyList<ElementId>? BeamIds { get; init; }
    public KataBeamRebarSpec? Spec { get; init; }
    public KataBeamMatchResult? MatchResult { get; init; }
    public KataRebarLayoutResult? Layout { get; init; }
    public IReadOnlyDictionary<double, RebarBarType>? ResolvedBarTypes { get; init; }

    public KataRebarRequest(KataRebarRequestKind kind)
    {
        Kind = kind;
    }
}
