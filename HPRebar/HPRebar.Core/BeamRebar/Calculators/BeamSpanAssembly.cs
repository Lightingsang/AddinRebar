using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>The cross-section of one framing element of the run, as read from the model (mm).</summary>
public sealed record BeamPieceSection(double Width, double Height, double TopElevation, string ElementUniqueId);

/// <summary>
/// Builds the span carried by the i-th framing element of a run from the supports at its two ends: support
/// <c>i</c> on the left, support <c>i + 1</c> on the right.
/// </summary>
public static class BeamSpanAssembly
{
    /// <summary>Concrete cover every span starts with until the user changes it.</summary>
    public const double DefaultCoverMm = 25.0;

    /// <summary>Centre-to-centre length assumed when the span has no right-hand support.</summary>
    private const double MissingSupportSpanMm = 4000.0;

    /// <summary>
    /// The span between support <paramref name="index"/> and the next one. A missing left support sits at 0 with
    /// no width; a missing right one 4000 mm further. A clear length that is not positive (supports overlapping)
    /// falls back to the centre-to-centre length. An end that is a cantilever tip makes the span a cantilever there.
    /// </summary>
    public static BeamSpan Between(int index, IReadOnlyList<BeamSupportNode> supports, BeamPieceSection section)
    {
        if (supports is null)
        {
            throw new ArgumentNullException(nameof(supports));
        }

        if (section is null)
        {
            throw new ArgumentNullException(nameof(section));
        }

        var left = index < supports.Count ? supports[index] : null;
        var right = index + 1 < supports.Count ? supports[index + 1] : null;

        double leftCenter = left?.CenterX ?? 0.0;
        double rightCenter = right?.CenterX ?? leftCenter + MissingSupportSpanMm;
        double leftWidth = left?.Width ?? 0.0;
        double rightWidth = right?.Width ?? 0.0;

        double lengthCenter = rightCenter - leftCenter;
        double lengthClear = lengthCenter - (leftWidth / 2.0) - (rightWidth / 2.0);
        if (lengthClear <= 0)
        {
            lengthClear = lengthCenter;
        }

        return new BeamSpan(
            index: index,
            name: $"Span {index + 1}",
            lengthCenter: lengthCenter,
            width: section.Width,
            height: section.Height,
            topElevation: section.TopElevation,
            cover: DefaultCoverMm,
            clearLength: lengthClear,
            startX: leftCenter + (leftWidth / 2.0),
            cantilever: CantileverAt(left, right),
            elementUniqueId: section.ElementUniqueId);
    }

    private static CantileverPosition CantileverAt(BeamSupportNode? left, BeamSupportNode? right)
    {
        bool leftTip = left?.Type == SupportType.CantileverEnd;
        bool rightTip = right?.Type == SupportType.CantileverEnd;
        if (leftTip && rightTip)
        {
            return CantileverPosition.Both;
        }

        if (leftTip)
        {
            return CantileverPosition.Left;
        }

        return rightTip ? CantileverPosition.Right : CantileverPosition.None;
    }
}
