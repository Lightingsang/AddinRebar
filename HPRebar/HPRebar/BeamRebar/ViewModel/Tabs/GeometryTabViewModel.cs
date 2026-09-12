using System.Collections.Generic;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.BeamRebar.Service;

namespace HPRebar.BeamRebar.ViewModel.Tabs;

/// <summary>
/// Read-only overview of detected continuous beam spans, support nodes, clear lengths, and elevations.
/// </summary>
public sealed partial class GeometryTabViewModel : BeamRebarTabViewModel
{
    public GeometryTabViewModel(BeamRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabGeometry;

    public IReadOnlyList<BeamSpan> Spans => Session.Stack.Spans;
    public IReadOnlyList<BeamSupportNode> Supports => Session.Stack.Supports;

    public int SpanCount => Session.SpanCount;
    public int SupportCount => Session.SupportCount;
    public double TotalLength => Session.TotalLength;
    public double MaxHeight => Session.MaxHeight;
}
