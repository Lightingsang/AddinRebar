using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// A support along the beam run axis (mm): a column, wall or girder found under the run, or a node the layout adds
/// itself (a cantilever tip, a stand-in at a bare joint).
/// </summary>
public sealed record MeasuredSupport(
    double CenterX, double Width, double Depth, SupportType Type, string ElementUniqueId);

/// <summary>Where one framing element of the run starts and ends along the run axis (mm).</summary>
public sealed record BeamPieceExtent(double Start, double End);

/// <summary>
/// Turns the supports found under a continuous beam run into its support nodes: one per support, a tip at a free
/// end, a stand-in at a beam joint nothing was found under, each named and classed exterior or interior.
/// </summary>
public static class BeamSupportLayout
{
    /// <summary>Length assumed for a framing element whose location is not a straight line.</summary>
    public const double UnknownBeamLengthMm = 4000.0;

    /// <summary>Two finds whose centres are closer than this are one support found twice.</summary>
    private const double MergeDistanceMm = 100.0;

    /// <summary>A run reaching further than this past its outermost support face ends in a cantilever.</summary>
    private const double CantileverMinOverhangMm = 200.0;

    /// <summary>A beam joint with a node closer than this already has its support.</summary>
    private const double JointSearchRadiusMm = 300.0;

    /// <summary>Width and depth of a support the model does not contain.</summary>
    private const double StandInSupportSizeMm = 300.0;

    /// <summary>
    /// The support nodes of a run with at least one measured support, in order along the run.
    /// </summary>
    /// <param name="measured">Every support found, in the order found; duplicates are merged.</param>
    /// <param name="pieces">
    /// One extent per framing element of the run, in any order; the run starts at the smallest Start and ends at
    /// the End of the piece that starts last.
    /// </param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// No support was measured, a support centre is not a finite number, or the run has no piece.
    /// </exception>
    public static IReadOnlyList<BeamSupportNode> Arrange(
        IEnumerable<MeasuredSupport> measured, IReadOnlyList<BeamPieceExtent> pieces)
    {
        if (measured is null)
        {
            throw new ArgumentNullException(nameof(measured));
        }

        if (pieces is null)
        {
            throw new ArgumentNullException(nameof(pieces));
        }

        var found = measured.ToList();
        if (found.Any(support => !FiniteNumber.IsFinite(support.CenterX)))
        {
            // A NaN centre sorts first and every distance from it compares false, so each real support would be
            // dropped as its duplicate.
            throw new ArgumentException("Every support centre must be a finite number.", nameof(measured));
        }

        var supports = Merge(found);
        if (supports.Count == 0)
        {
            throw new ArgumentException("At least one support must be measured.", nameof(measured));
        }

        if (pieces.Count == 0)
        {
            throw new ArgumentException("The run has no framing element.", nameof(pieces));
        }

        var ordered = pieces.OrderBy(piece => piece.Start).ToList();
        var withTips = WithCantileverTips(supports, ordered[0].Start, ordered[ordered.Count - 1].End);
        var nodes = WithStandInJoints(withTips, ordered);
        return Classify(nodes.OrderBy(node => node.CenterX).ToList());
    }

    /// <summary>
    /// Support nodes for a run under which nothing was found: a 300 mm column at the start and after each
    /// framing element, spaced by the element lengths from 0.
    /// </summary>
    /// <param name="beamLengths">The length of each framing element, in run order (mm).</param>
    /// <exception cref="ArgumentNullException"><paramref name="beamLengths"/> is null.</exception>
    public static IReadOnlyList<BeamSupportNode> Synthesize(IReadOnlyList<double> beamLengths)
    {
        if (beamLengths is null)
        {
            throw new ArgumentNullException(nameof(beamLengths));
        }

        var nodes = new List<BeamSupportNode>();
        double x = 0.0;
        for (int i = 0; i < beamLengths.Count; i++)
        {
            if (i == 0)
            {
                nodes.Add(new BeamSupportNode(
                    index: 0,
                    name: "Support 1",
                    centerX: x,
                    width: StandInSupportSizeMm,
                    type: SupportType.ExteriorColumn,
                    depth: StandInSupportSizeMm,
                    isExterior: true));
            }

            x += beamLengths[i];
            bool isLast = i == beamLengths.Count - 1;
            nodes.Add(new BeamSupportNode(
                index: i + 1,
                name: $"Support {i + 2}",
                centerX: x,
                width: StandInSupportSizeMm,
                type: isLast ? SupportType.ExteriorColumn : SupportType.Column,
                depth: StandInSupportSizeMm,
                isExterior: isLast));
        }

        return nodes;
    }

    /// <summary>
    /// The supports in order along the run. A find whose centre is less than <see cref="MergeDistanceMm"/> from
    /// the last support kept is the same support found again and is dropped, so the one nearest the start is kept
    /// (the first found on equal centres); measuring from the kept support stops a row of close finds chaining
    /// into one.
    /// </summary>
    private static List<MeasuredSupport> Merge(IEnumerable<MeasuredSupport> measured)
    {
        var kept = new List<MeasuredSupport>();
        foreach (var support in measured.OrderBy(support => support.CenterX))
        {
            if (kept.Count == 0 || support.CenterX - kept[kept.Count - 1].CenterX >= MergeDistanceMm)
            {
                kept.Add(support);
            }
        }

        return kept;
    }

    /// <summary>The supports with a tip node added at each run end overhanging its outermost support face.</summary>
    private static List<MeasuredSupport> WithCantileverTips(
        List<MeasuredSupport> supports, double runStart, double runEnd)
    {
        var first = supports[0];
        var last = supports[supports.Count - 1];
        var nodes = new List<MeasuredSupport>(supports.Count + 2);

        if (first.CenterX - (first.Width / 2.0) - runStart > CantileverMinOverhangMm)
        {
            nodes.Add(Tip(runStart));
        }

        nodes.AddRange(supports);

        if (runEnd - (last.CenterX + (last.Width / 2.0)) > CantileverMinOverhangMm)
        {
            nodes.Add(Tip(runEnd));
        }

        return nodes;
    }

    /// <summary>
    /// The nodes with a stand-in column at each joint between consecutive pieces that has no node nearby, added
    /// in run order only while there are fewer nodes than pieces + 1.
    /// </summary>
    private static List<MeasuredSupport> WithStandInJoints(
        List<MeasuredSupport> nodes, List<BeamPieceExtent> orderedPieces)
    {
        var result = new List<MeasuredSupport>(nodes);
        int wanted = orderedPieces.Count + 1;
        for (int i = 0; i < orderedPieces.Count - 1 && result.Count < wanted; i++)
        {
            double joint = orderedPieces[i].End;
            if (!result.Any(node => Math.Abs(node.CenterX - joint) < JointSearchRadiusMm))
            {
                result.Add(new MeasuredSupport(
                    joint, StandInSupportSizeMm, StandInSupportSizeMm, SupportType.Column, string.Empty));
            }
        }

        return result;
    }

    /// <summary>Names each node and classes the first and last as exterior, the others as interior.</summary>
    private static List<BeamSupportNode> Classify(List<MeasuredSupport> nodes)
    {
        var result = new List<BeamSupportNode>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            bool isExterior = i == 0 || i == nodes.Count - 1;
            var type = ClassOf(node.Type, isExterior);
            string name = type == SupportType.CantileverEnd ? $"Tip {i + 1}" : $"Support {i + 1}";

            result.Add(new BeamSupportNode(
                index: i,
                name: name,
                centerX: node.CenterX,
                width: node.Width,
                type: type,
                depth: node.Depth,
                elementUniqueId: node.ElementUniqueId,
                isExterior: isExterior));
        }

        return result;
    }

    /// <summary>
    /// A column at an end is exterior, one in between interior; tips, walls and girders keep their type.
    /// </summary>
    private static SupportType ClassOf(SupportType measured, bool isExterior)
    {
        if (measured == SupportType.CantileverEnd)
        {
            return SupportType.CantileverEnd;
        }

        if (isExterior)
        {
            return measured is SupportType.Column or SupportType.ExteriorColumn ? SupportType.ExteriorColumn : measured;
        }

        return measured is SupportType.Column or SupportType.ExteriorColumn ? SupportType.InteriorColumn : measured;
    }

    private static MeasuredSupport Tip(double x) => new(x, 0.0, 0.0, SupportType.CantileverEnd, string.Empty);
}
