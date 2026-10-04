using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport;

public enum KataExportRequestKind
{
    Highlight,
    Repick,
    PreviewRebar,
    GenerateRebar
}

/// <summary>One queued unit of work for Revit's API thread and the task the window awaits.</summary>
public sealed class KataExportRequest
{
    public KataExportRequestKind Kind { get; }
    public IReadOnlyList<ElementId>? BeamIds { get; }

    /// <summary>The parsed sheet Dam (rebar requests).</summary>
    public KataBeamRebarSpec? Spec { get; init; }

    /// <summary>Office detailing settings the sheet is planned with (rebar requests).</summary>
    public KataSettings? Settings { get; init; }

    /// <summary>The direction the window writes the sheet in; it settles a run that reads the same both ways.</summary>
    public bool? PreferReversed { get; init; }

    /// <summary>Bar type chosen for each diameter (generation).</summary>
    public IReadOnlyDictionary<double, ElementId>? BarTypeIds { get; init; }

    /// <summary>Keys of the bar groups removed on the canvas (generation), see <c>KataLayoutRemoval</c>.</summary>
    public IReadOnlyCollection<string>? RemovedKeys { get; init; }

    /// <summary><c>KataLayoutRemoval.Fingerprint</c> of the plan the keys were picked on (generation).</summary>
    public string? PlannedFingerprint { get; init; }

    public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public KataExportRequest(KataExportRequestKind kind, IReadOnlyList<ElementId>? beamIds = null)
    {
        Kind = kind;
        BeamIds = beamIds;
    }
}
