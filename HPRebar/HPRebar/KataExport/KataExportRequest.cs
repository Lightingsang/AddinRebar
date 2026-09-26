using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace HPRebar.KataExport;

public enum KataExportRequestKind
{
    Highlight,
    Repick
}

public sealed class KataExportRequest
{
    public KataExportRequestKind Kind { get; }
    public IReadOnlyList<ElementId>? BeamIds { get; }
    public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public KataExportRequest(KataExportRequestKind kind, IReadOnlyList<ElementId>? beamIds = null)
    {
        Kind = kind;
        BeamIds = beamIds;
    }
}
