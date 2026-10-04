using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Cuts a beam run into the ordered support / span / joint segments of the Kata sheet by working on
/// stations along the beam axis: supports keep their full width, spans are the gaps between them.
/// </summary>
public static class KataSegmenter
{
    public static KataSegmentation Segment(KataRunInput input, KataBuildOptions? options = null)
    {
        KataInputValidator.Validate(input);
        options ??= new KataBuildOptions();

        var warnings = new List<string>();
        var pieces = input.Pieces.OrderBy(p => p.Extent.Start).ToList();
        var run = new Interval1D(pieces.Min(p => p.Extent.Start), pieces.Max(p => p.Extent.End));
        CheckContinuity(pieces, warnings);

        var supports = MergeSupports(input.Supports.Where(s => s.Extent.Overlaps(run)));
        int outside = input.Supports.Count(s => !s.Extent.Overlaps(run));
        if (outside > 0) warnings.Add($"{outside} support(s) outside the beam run were ignored.");

        var joints = options.InsertJoints ? JointStations(pieces) : new List<double>();
        var segments = new List<KataSegment>();
        double cursor = run.Start;
        foreach (var support in supports)
        {
            AddSpans(segments, cursor, support.Extent.Start, pieces, joints);
            segments.Add(new KataSegment(KataSegmentKind.Support, support.Extent, Support: support));
            cursor = Math.Max(cursor, support.Extent.End);
        }

        AddSpans(segments, cursor, run.End, pieces, joints);

        if (segments.Count == 0)
            throw new ArgumentException("The beam run is shorter than one span.", nameof(input));

        return new KataSegmentation(
            segments,
            segments[0].Kind == KataSegmentKind.Support,
            segments[segments.Count - 1].Kind == KataSegmentKind.Support,
            warnings);
    }

    /// <summary>
    /// Merges supports that overlap or touch into one; the merged support takes the kind, element and
    /// section of the strongest member (column, then foundation, then beam; the longer one on a tie).
    /// </summary>
    internal static IReadOnlyList<KataSupport> MergeSupports(IEnumerable<KataSupport> supports)
    {
        var merged = new List<KataSupport>();
        var group = new List<KataSupport>();
        var extent = default(Interval1D);

        foreach (var support in supports.OrderBy(s => s.Extent.Start))
        {
            if (group.Count > 0 && !support.Extent.Overlaps(extent))
            {
                merged.Add(Combine(group, extent));
                group.Clear();
            }

            extent = group.Count == 0 ? support.Extent : extent.Union(support.Extent);
            group.Add(support);
        }

        if (group.Count > 0) merged.Add(Combine(group, extent));
        return merged;
    }

    private static KataSupport Combine(IReadOnlyList<KataSupport> group, Interval1D extent)
    {
        var dominant = group.OrderBy(s => s.Kind).ThenByDescending(s => s.Extent.Length).First();
        var upper = dominant.Upper ?? group.Select(s => s.Upper).FirstOrDefault(u => u.HasValue);

        // A beam that overlaps a column is the beam framing into it across the run, not a support of its own.
        var crossing = dominant.CrossingBeamStationMm;
        var section = dominant.CrossingBeamSection;
        if (crossing is null && dominant.Kind != KataSupportKind.Beam)
        {
            var beam = group
                .Where(s => s.Kind == KataSupportKind.Beam)
                .OrderBy(s => Math.Abs(s.Extent.Mid - extent.Mid))
                .FirstOrDefault();
            crossing = beam?.Extent.Mid;
            section = beam?.SectionText;
        }

        return dominant with { Extent = extent, Upper = upper, CrossingBeamStationMm = crossing, CrossingBeamSection = section };
    }

    private static void CheckContinuity(IReadOnlyList<KataBeamPiece> pieces, List<string> warnings)
    {
        double reach = pieces[0].Extent.End;
        for (int i = 1; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            if (piece.Extent.Start > reach + KataTolerance.StationMm)
                warnings.Add($"Gap of {KataFormat.Round(piece.Extent.Start - reach)} mm before beam {piece.ElementKey}.");
            else if (piece.Extent.Start < reach - KataTolerance.StationMm)
                warnings.Add($"Beam {piece.ElementKey} overlaps the previous beam by {KataFormat.Round(reach - piece.Extent.Start)} mm.");
            reach = Math.Max(reach, piece.Extent.End);
        }
    }

    /// <summary>
    /// Stations where one element ends and another begins, and that no element runs through:
    /// an element lying inside a longer one is a duplicate, not a joint.
    /// </summary>
    private static List<double> JointStations(IReadOnlyList<KataBeamPiece> pieces)
    {
        var stations = new List<double>();
        foreach (var piece in pieces)
        {
            double s = piece.Extent.End;
            bool meetsNext = pieces.Any(p => !ReferenceEquals(p, piece) && Math.Abs(p.Extent.Start - s) <= KataTolerance.StationMm);
            bool runsThrough = pieces.Any(p => p.Extent.Start < s - KataTolerance.StationMm && p.Extent.End > s + KataTolerance.StationMm);
            if (!meetsNext || runsThrough) continue;
            if (stations.Any(existing => Math.Abs(existing - s) < KataTolerance.StationMm)) continue;
            stations.Add(s);
        }

        stations.Sort();
        return stations;
    }

    private static void AddSpans(List<KataSegment> segments, double from, double to, IReadOnlyList<KataBeamPiece> pieces, IReadOnlyList<double> joints)
    {
        if (to - from < KataTolerance.MinimumSpanMm) return;

        double start = from;
        foreach (double joint in joints.Where(j => j > from + KataTolerance.JointSnapMm && j < to - KataTolerance.JointSnapMm))
        {
            AddSpan(segments, start, joint, pieces);
            segments.Add(new KataSegment(KataSegmentKind.Joint, new Interval1D(joint, joint)));
            start = joint;
        }

        AddSpan(segments, start, to, pieces);
    }

    private static void AddSpan(List<KataSegment> segments, double from, double to, IReadOnlyList<KataBeamPiece> pieces)
    {
        var extent = new Interval1D(from, to);
        if (extent.Length < KataTolerance.MinimumSpanMm) return;

        var piece = pieces.FirstOrDefault(p => p.Extent.Contains(extent.Mid, 0.0))
                    ?? pieces.OrderBy(p => Math.Min(Math.Abs(p.Extent.Start - extent.Mid), Math.Abs(p.Extent.End - extent.Mid))).First();
        segments.Add(new KataSegment(KataSegmentKind.Span, extent, Piece: piece));
    }
}
