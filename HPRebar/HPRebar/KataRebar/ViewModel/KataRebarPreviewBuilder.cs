using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataRebar.ViewModel;

/// <summary>Turns a plan into the rows and messages the Kata Rebar window lists.</summary>
public static class KataRebarPreviewBuilder
{
    public const string BlockingPrefix = "[Chặn] ";
    public const string WarningPrefix = "[Cảnh báo] ";
    public const string SkippedPrefix = "[Bỏ qua] ";

    public static IReadOnlyList<KataSpanPreviewItem> Spans(KataRebarPlan plan)
    {
        var spec = plan.Spec;
        var stirrup = spec.GlobalStirrup;
        return spec.Spans.Select((span, i) => new KataSpanPreviewItem
        {
            SpanNumber = i + 1,
            LeftColumnWidthMm = i < spec.Supports.Count ? spec.Supports[i].ColumnWidth : 0.0,
            ClearSpanLengthMm = span.Length,
            RightColumnWidthMm = i + 1 < spec.Supports.Count ? spec.Supports[i + 1].ColumnWidth : 0.0,
            StirrupSpacing = $"Ø{stirrup.Diameter:0} a{stirrup.SupportSpacing:0}/{stirrup.MidspanSpacing:0}/{stirrup.SupportSpacing:0}"
        }).ToList();
    }

    public static IReadOnlyList<KataBarLayerPreviewItem> Bars(KataRebarPlan plan)
    {
        var rows = new List<KataBarLayerPreviewItem>();
        Add(rows, "Thép chủ trên", plan.Spec.TopContinuous, plan.Layout.MainTopBars, plan.Rules.TopBarCentreDepth);
        Add(rows, "Thép chủ dưới", plan.Spec.BottomContinuous, plan.Layout.MainBottomBars, plan.Rules.BottomBarCentreDepth);
        foreach (var group in plan.Layout.ExtraTopBars.GroupBy(b => b.BarMark))
        {
            var first = group.First();
            var points = first.Polyline.Points;
            double z = points[first.StartHookLength > 0.0 ? 1 : 0].Z;
            rows.Add(new KataBarLayerPreviewItem
            {
                Category = "Gia cường gối",
                Location = $"Gối {first.HostSupportIndex + 1}, hàng {12 + first.Layer}",
                Notation = string.Join("+", group.GroupBy(b => b.Diameter).Select(g => $"{g.Count()}f{g.Key:0}")),
                Count = group.Count(),
                DiameterMm = group.Max(b => b.Diameter),
                Details = $"tâm cách mép {-z:0}; x {points[0].X:0} → {points[points.Count - 1].X:0}; "
                          + $"neo trái {End(first.StartHookLength)}, phải {End(first.EndHookLength)}"
            });
        }

        return rows;
    }

    public static IReadOnlyList<string> Messages(KataRebarPlan plan, KataBeamMatchResult? match)
    {
        var messages = new List<string>();
        messages.AddRange(plan.Blocking.Select(m => BlockingPrefix + m));
        if (match is { IsSuccess: false }) messages.Add(BlockingPrefix + match.Message);
        messages.AddRange(plan.Warnings.Select(m => WarningPrefix + m));
        if (match is { IsSuccess: true }) messages.AddRange(match.Warnings.Select(m => WarningPrefix + m));
        messages.AddRange(plan.Skipped.Select(m => SkippedPrefix + m));
        return messages;
    }

    private static void Add(List<KataBarLayerPreviewItem> rows, string category, KataBarItem item, IReadOnlyList<KataRebarCurve> bars, double centreDepth)
    {
        if (item.IsEmpty || bars.Count == 0) return;

        var first = bars[0];
        var points = first.Polyline.Points;
        rows.Add(new KataBarLayerPreviewItem
        {
            Category = category,
            Location = "Suốt dầm",
            Notation = item.ToString(),
            Count = bars.Count,
            DiameterMm = item.Diameter,
            Details = $"tâm cách mép {centreDepth:0}; x {points[0].X:0} → {points[points.Count - 1].X:0}; "
                      + $"neo trái {End(first.StartHookLength)}, phải {End(first.EndHookLength)}"
        });
    }

    private static string End(double leg) => leg > 0.0 ? $"bẻ 90° chân {leg:0}" : "thẳng";
}
