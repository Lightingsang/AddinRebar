using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.Service;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Matches the diameters of a plan to the project's RebarBarTypes: a type named after the notation or the
/// diameter first, then any type within ±0.5 mm (deformed preferred for longitudinal bars). A diameter
/// with no such type stays unmatched and is reported by name; no other diameter is substituted.
/// </summary>
public sealed class KataRebarTypeResolver
{
    public const double DiameterTolerance = 0.5;

    public KataRebarTypeResolver(Document doc)
    {
        AllBarTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarBarType))
            .Cast<RebarBarType>()
            .Select(b => new RebarTypeOption
            {
                Name = b.Name,
                DiameterMm = Math.Round(RevitUnits.FtToMm(b.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)?.AsDouble() ?? b.BarNominalDiameter), 1),
                DeformationType = b.DeformationType,
                Id = b.Id
            })
            .OrderBy(b => b.DiameterMm)
            .ThenBy(b => b.Name)
            .ToList();
    }

    public IReadOnlyList<RebarTypeOption> AllBarTypes { get; }

    public RebarTypeOption? Resolve(string notation, double diameterMm, bool isLongitudinal)
    {
        string d = diameterMm.ToString("0");
        var named = AllBarTypes.FirstOrDefault(b =>
            b.Name.Equals(notation?.Trim(), StringComparison.OrdinalIgnoreCase)
            || new[] { $"D{d}", $"f{d}", $"phi{d}", $"T{d}", $"Ø{d}" }.Any(n => b.Name.Equals(n, StringComparison.OrdinalIgnoreCase)));
        if (named is not null && Math.Abs(named.DiameterMm - diameterMm) <= DiameterTolerance) return named;

        var candidates = AllBarTypes.Where(b => Math.Abs(b.DiameterMm - diameterMm) <= DiameterTolerance).ToList();
        if (candidates.Count == 0) return null;

        if (isLongitudinal)
        {
            var deformed = candidates.FirstOrDefault(b => b.DeformationType == RebarDeformationType.Deformed);
            if (deformed is not null) return deformed;
        }

        return candidates.FirstOrDefault(b => b.Name.Contains(d)) ?? candidates[0];
    }

    /// <summary>One row per diameter the plan draws: top bars, bottom bars, additional top and bottom bars, stirrups.</summary>
    public IReadOnlyList<KataBarTypeMappingItem> BuildMappingItems(KataRebarPlan plan)
    {
        var spec = plan.Spec;
        var items = new List<KataBarTypeMappingItem>();

        void Add(string role, string notation, double diameter, bool longitudinal)
        {
            if (diameter <= 0.0 || items.Any(i => i.DiameterMm == diameter)) return;
            items.Add(new KataBarTypeMappingItem
            {
                Role = role,
                Notation = string.IsNullOrWhiteSpace(notation) ? $"Ø{diameter:0.#}" : notation,
                DiameterMm = diameter,
                SelectedType = Resolve(notation, diameter, longitudinal),
                AvailableTypes = AllBarTypes
            });
        }

        if (!spec.TopContinuous.IsEmpty) Add("Thép chủ trên", spec.TopContinuous.RawNotation, spec.TopContinuous.Diameter, true);
        if (!spec.BottomContinuous.IsEmpty) Add("Thép chủ dưới", spec.BottomContinuous.RawNotation, spec.BottomContinuous.Diameter, true);
        foreach (var bar in plan.Layout.ExtraTopBars)
            Add("Gia cường gối", $"Ø{bar.Diameter:0.#}", bar.Diameter, true);
        foreach (var bar in plan.Layout.ExtraBottomBars)
            Add("Gia cường nhịp", $"Ø{bar.Diameter:0.#}", bar.Diameter, true);
        foreach (var bar in plan.Layout.SideBars)
            Add("Cốt giá", $"Ø{bar.Diameter:0.#}", bar.Diameter, true);
        foreach (var bar in plan.Layout.HangerBars)
            Add("Vai bò", $"Ø{bar.Diameter:0.#}", bar.Diameter, true);
        // Spans with main bars of their own (rows 19 / 21).
        foreach (var bar in plan.Layout.MainTopBars.Concat(plan.Layout.MainBottomBars))
            Add(bar.Role == KataBarRole.MainTop ? "Thép chủ trên" : "Thép chủ dưới", $"Ø{bar.Diameter:0.#}", bar.Diameter, true);
        if (spec.GlobalStirrup.Diameter > 0.0) Add("Cốt đai", $"Ø{spec.GlobalStirrup.Diameter:0.#}", spec.GlobalStirrup.Diameter, false);
        foreach (var zone in plan.Layout.StirrupZones.Where(z => z.Diameter > 0.0))
            Add("Đai gia cường nút", $"Ø{zone.Diameter:0.#}", zone.Diameter, false);
        // Ties and inner stirrups take the stirrup's diameter: that row already covers them.
        foreach (var set in plan.Layout.BarSets)
            Add("Móc C / đai trong", $"Ø{set.Diameter:0.#}", set.Diameter, false);

        return items;
    }
}
