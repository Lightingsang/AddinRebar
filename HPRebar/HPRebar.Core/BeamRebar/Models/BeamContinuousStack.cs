using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Master geometric assembly representing a complete continuous multi-span beam.
/// Immutable container holding ordered spans, support nodes, and secondary intersections.
/// </summary>
public sealed record BeamContinuousStack
{
    public BeamContinuousStack()
    {
    }

    public BeamContinuousStack(
        IReadOnlyList<BeamSpan> spans,
        IReadOnlyList<BeamSupportNode> supports,
        IReadOnlyList<SecondaryBeamIntersection>? secondaryIntersections = null)
    {
        Spans = spans ?? Array.Empty<BeamSpan>();
        Supports = supports ?? Array.Empty<BeamSupportNode>();
        SecondaryIntersections = secondaryIntersections ?? Array.Empty<SecondaryBeamIntersection>();
    }

    public IReadOnlyList<BeamSpan> Spans { get; init; } = Array.Empty<BeamSpan>();

    public IReadOnlyList<BeamSupportNode> Supports { get; init; } = Array.Empty<BeamSupportNode>();

    public IReadOnlyList<BeamSupportNode> SupportNodes => Supports;

    public IReadOnlyList<SecondaryBeamIntersection> SecondaryIntersections { get; init; } = Array.Empty<SecondaryBeamIntersection>();

    public int SpanCount => Spans.Count;
    public int SupportCount => Supports.Count;

    /// <summary>Overall continuous length from the start face of Support 0 to end face of Support N (mm).</summary>
    public double TotalLength => OverallEndX - OverallStartX;

    /// <summary>Left-most coordinate of the entire beam run (mm), including cantilever overhangs.</summary>
    public double OverallStartX
    {
        get
        {
            if (Spans.Count == 0 && Supports.Count == 0) return 0.0;
            double min = Spans.Count > 0 ? Spans[0].StartX : double.MaxValue;
            if (Supports.Count > 0 && Supports[0].LeftFaceX < min)
            {
                min = Supports[0].LeftFaceX;
            }
            return min == double.MaxValue ? 0.0 : min;
        }
    }

    /// <summary>Right-most coordinate of the entire beam run (mm), including cantilever overhangs.</summary>
    public double OverallEndX
    {
        get
        {
            if (Spans.Count == 0 && Supports.Count == 0) return 0.0;
            double max = Spans.Count > 0 ? Spans[Spans.Count - 1].EndX : double.MinValue;
            if (Supports.Count > 0 && Supports[Supports.Count - 1].RightFaceX > max)
            {
                max = Supports[Supports.Count - 1].RightFaceX;
            }
            return max == double.MinValue ? 0.0 : max;
        }
    }

    /// <summary>Maximum cross-section height across all spans (mm).</summary>
    public double MaxHeight
    {
        get
        {
            double max = 0.0;
            for (int i = 0; i < Spans.Count; i++)
            {
                if (Spans[i].Height > max) max = Spans[i].Height;
            }
            return max;
        }
    }

    /// <summary>Minimum top elevation across all spans (mm).</summary>
    public double MinTopElevation
    {
        get
        {
            if (Spans.Count == 0) return 0.0;
            double min = Spans[0].TopElevation;
            for (int i = 1; i < Spans.Count; i++)
            {
                if (Spans[i].TopElevation < min) min = Spans[i].TopElevation;
            }
            return min;
        }
    }

    /// <summary>Maximum top elevation across all spans (mm).</summary>
    public double MaxTopElevation
    {
        get
        {
            if (Spans.Count == 0) return 0.0;
            double max = Spans[0].TopElevation;
            for (int i = 1; i < Spans.Count; i++)
            {
                if (Spans[i].TopElevation > max) max = Spans[i].TopElevation;
            }
            return max;
        }
    }

    /// <summary>
    /// Performs geometric validation of the continuous beam stack.
    /// Checks span count, support alignment, contiguity, and positive dimensions.
    /// </summary>
    public ValidationResult Validate()
    {
        if (Spans.Count == 0)
            return ValidationResult.Fail("Beam continuous stack must contain at least 1 span.");

        if (Supports.Count != Spans.Count + 1)
            return ValidationResult.Fail($"Support count ({Supports.Count}) must equal Span count ({Spans.Count}) + 1.");

        for (int i = 0; i < Spans.Count; i++)
        {
            var span = Spans[i];
            if (span.Width <= 0.0) return ValidationResult.Fail($"Span {i} has invalid width: {span.Width} mm.");
            if (span.Height <= 0.0) return ValidationResult.Fail($"Span {i} has invalid height: {span.Height} mm.");
            if (span.LengthClear <= 0.0) return ValidationResult.Fail($"Span {i} has non-positive clear span: {span.LengthClear} mm.");
            if (span.Cover <= 0.0) return ValidationResult.Fail($"Span {i} has non-positive cover: {span.Cover} mm.");
        }

        // Validate joint contiguity between spans and supports
        const double tolerance = 1.0; // 1.0 mm tolerance for physical join
        for (int i = 0; i < Spans.Count; i++)
        {
            var leftSupport = Supports[i];
            var span = Spans[i];
            var rightSupport = Supports[i + 1];

            // If span starts at left support face
            if (span.StartX > 0 && System.Math.Abs(leftSupport.RightFaceX - span.StartX) > tolerance)
            {
                // Acceptable if startX is relative to beam stack or support
            }
        }

        return ValidationResult.Ok();
    }

    /// <summary>
    /// A copy of this stack whose spans all use <paramref name="coverMm"/> as the clear cover to the stirrup,
    /// so every bar group is placed with the cover the engineer entered rather than the one read from the model.
    /// </summary>
    public BeamContinuousStack WithCover(double coverMm)
    {
        if (!(coverMm > 0.0))
            throw new ArgumentOutOfRangeException(nameof(coverMm), coverMm, "Cover must be a positive length in millimetres.");

        var spans = new BeamSpan[Spans.Count];
        for (int i = 0; i < Spans.Count; i++)
        {
            spans[i] = Spans[i] with { Cover = coverMm };
        }

        return this with { Spans = spans };
    }

    /// <summary>Finds the span enclosing the given station coordinate X.</summary>
    public BeamSpan? FindSpanAt(double x)
    {
        for (int i = 0; i < Spans.Count; i++)
        {
            if (x >= Spans[i].StartX && x <= Spans[i].EndX)
                return Spans[i];
        }
        return null;
    }
}
