using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataRebar;

public enum KataRebarRequestKind
{
    Pick,
    Measure,
    Generate
}

/// <summary>One unit of work queued for Revit's API thread, completed with its result.</summary>
public sealed class KataRebarRequest
{
    public KataRebarRequest(KataRebarRequestKind kind)
    {
        Kind = kind;
    }

    public KataRebarRequestKind Kind { get; }

    public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyList<ElementId> BeamIds { get; init; } = new List<ElementId>();

    public KataBeamRebarSpec? Spec { get; init; }

    /// <summary>RebarBarType id chosen for each diameter of the plan.</summary>
    public IReadOnlyDictionary<double, ElementId> BarTypeIds { get; init; } = new Dictionary<double, ElementId>();
}
