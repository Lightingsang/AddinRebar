using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.Service;
using HPRebar.KataRebar.Model;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Measures the picked beams the way Kata Export does — the same run reader, support scan and segmenter —
/// so the sheet Kata Export wrote for a beam lines up column for column with what is measured here.
/// </summary>
public static class KataBeamMatcher
{
    public static KataBeamMatchResult Measure(Document doc, RevitView view, IReadOnlyList<Element> beams)
    {
        if (beams is null || beams.Count == 0)
            return KataBeamMatchResult.Failed("Chưa chọn dầm kết cấu nào trong Revit.");

        try
        {
            var run = KataRunReader.Read(doc, beams);
            var (supports, supportWarnings) = KataSupportCollector.Collect(doc, view, run);

            var pieces = run.Pieces
                .Select(p => new KataBeamPiece(p.Stations, p.WidthMm, p.HeightMm, p.ZOffsetMm, p.Element.UniqueId))
                .ToList();
            var header = new KataHeader(null, null, pieces[0].HeightMm, pieces[0].WidthMm, 0.0);
            var segmentation = KataSegmenter.Segment(new KataRunInput(pieces, supports, Array.Empty<KataGridCrossing>(), header));

            var measured = new KataMeasuredBeam(
                run.Pieces[0].WidthMm,
                run.Pieces[0].HeightMm,
                segmentation.Segments.Select(s => ToMeasured(s, pieces)).ToList(),
                run.Pieces.Count);

            var warnings = supportWarnings.Concat(segmentation.Warnings).ToList();
            if (!segmentation.StartsWithSupport || !segmentation.EndsWithSupport)
                warnings.Add("Một đầu dải dầm không tìm thấy cột/vách/móng (kiểm tra cột có hiện trong view đang mở không).");

            return new KataBeamMatchResult
            {
                IsSuccess = true,
                Message = $"{run.Pieces.Count} dầm, {measured.WidthMm:0}×{measured.HeightMm:0} mm, {Describe(measured)}",
                BeamIds = run.Pieces.Select(p => p.Element.Id).ToList(),
                Measured = measured,
                Run = run,
                SegmentExtents = segmentation.Segments.Select(s => s.Extent).ToList(),
                Warnings = warnings
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or Autodesk.Revit.Exceptions.ApplicationException)
        {
            return KataBeamMatchResult.Failed(ex.Message);
        }
    }

    private static KataMeasuredSegment ToMeasured(KataSegment segment, IReadOnlyList<KataBeamPiece> pieces) => new(
        segment.Kind switch
        {
            KataSegmentKind.Span => KataMeasuredSupportKind.None,
            KataSegmentKind.Joint => KataMeasuredSupportKind.Joint,
            _ => segment.Support?.Kind switch
            {
                KataSupportKind.Foundation => KataMeasuredSupportKind.Foundation,
                KataSupportKind.Beam => KataMeasuredSupportKind.Beam,
                _ => KataMeasuredSupportKind.Column
            }
        },
        segment.Extent.Length,
        segment.Kind == KataSegmentKind.Span ? SpanDepth(segment, pieces) : 0.0,
        segment.Kind == KataSegmentKind.Span ? PiecesOver(segment, pieces) : null);

    /// <summary>The framing elements over a span, clipped to it: their section and top (row 19) from the span's start.</summary>
    private static IReadOnlyList<KataMeasuredPiece> PiecesOver(KataSegment segment, IReadOnlyList<KataBeamPiece> pieces) => pieces
        .Select(p => (Piece: p, Start: Math.Max(p.Extent.Start, segment.Extent.Start), End: Math.Min(p.Extent.End, segment.Extent.End)))
        .Where(c => c.End - c.Start > MinPieceOverlapMm)
        .OrderBy(c => c.Start)
        .Select(c => new KataMeasuredPiece(c.Start - segment.Extent.Start, c.End - c.Start, c.Piece.WidthMm, c.Piece.HeightMm, c.Piece.ZOffsetMm))
        .ToList();

    /// <summary>A framing element reaching less than this into a span (a beam end cut back into a column) is not over it.</summary>
    private const double MinPieceOverlapMm = 50.0;

    /// <summary>Depth of the framing piece under the middle of a span.</summary>
    private static double SpanDepth(KataSegment segment, IReadOnlyList<KataBeamPiece> pieces)
    {
        if (segment.Piece is { } own) return own.HeightMm;
        double mid = segment.Extent.Mid;
        return pieces.FirstOrDefault(p => p.Extent.Contains(mid, 1.0))?.HeightMm ?? 0.0;
    }

    private static string Describe(KataMeasuredBeam measured) =>
        string.Join(" · ", measured.Segments.Select(s => s.IsSupport ? $"gối {s.LengthMm:0}" : $"nhịp {s.LengthMm:0}"));
}
