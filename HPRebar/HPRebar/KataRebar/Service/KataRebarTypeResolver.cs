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
/// Resolves bar notations (e.g. '2f18', 'd8', 'f25') to project RebarBarType by numerical diameter
/// (~18mm +/- 0.5mm) and deformation type, and matches standard RebarHookType by angle.
/// </summary>
public sealed class KataRebarTypeResolver
{
    private readonly IReadOnlyList<RebarTypeOption> _barTypes;
    private readonly IReadOnlyList<RebarHookOption> _hookTypes;

    public KataRebarTypeResolver(Document doc)
    {
        _barTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarBarType))
            .Cast<RebarBarType>()
            .Select(b =>
            {
                double diamFt = b.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)?.AsDouble()
                                ?? b.BarNominalDiameter;
                return new RebarTypeOption
                {
                    Name = b.Name,
                    DiameterMm = Math.Round(RevitUnits.FtToMm(diamFt), 1),
                    DeformationType = b.DeformationType,
                    Id = b.Id,
                    BarType = b
                };
            })
            .OrderBy(b => b.DiameterMm)
            .ThenBy(b => b.Name)
            .ToList();

        _hookTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarHookType))
            .Cast<RebarHookType>()
            .Select(h =>
            {
                double angleDeg = Math.Round(h.HookAngle * 180.0 / Math.PI, 1);
                return new RebarHookOption
                {
                    Name = h.Name,
                    AngleDegrees = angleDeg,
                    Style = h.Style,
                    Id = h.Id,
                    HookType = h
                };
            })
            .ToList();
    }

    public IReadOnlyList<RebarTypeOption> AllBarTypes => _barTypes;
    public IReadOnlyList<RebarHookOption> AllHookTypes => _hookTypes;

    /// <summary>
    /// Resolves a bar notation and target diameter to the best matching project RebarBarType.
    /// </summary>
    public RebarTypeOption? ResolveBarType(string rawNotation, double targetDiameterMm, bool isLongitudinal = true)
    {
        if (_barTypes.Count == 0) return null;

        // Step 1: Exact Name Match
        string cleanNotation = (rawNotation ?? "").Trim();
        string dName = $"D{targetDiameterMm:0}";
        string fName = $"f{targetDiameterMm:0}";
        string phiName = $"phi{targetDiameterMm:0}";
        string tName = $"T{targetDiameterMm:0}";

        var exact = _barTypes.FirstOrDefault(b =>
            b.Name.Equals(cleanNotation, StringComparison.OrdinalIgnoreCase) ||
            b.Name.Equals(dName, StringComparison.OrdinalIgnoreCase) ||
            b.Name.Equals(fName, StringComparison.OrdinalIgnoreCase) ||
            b.Name.Equals(phiName, StringComparison.OrdinalIgnoreCase) ||
            b.Name.Equals(tName, StringComparison.OrdinalIgnoreCase));

        if (exact is not null) return exact;

        // Step 2: Tolerance Match within ± 0.5 mm
        var candidates = _barTypes
            .Where(b => Math.Abs(b.DiameterMm - targetDiameterMm) <= 0.5)
            .ToList();

        if (candidates.Count == 1) return candidates[0];

        if (candidates.Count > 1)
        {
            // For longitudinal bars (or >= 12mm), prioritize Deformed
            if (isLongitudinal || targetDiameterMm >= 12.0)
            {
                var deformed = candidates.FirstOrDefault(b => b.DeformationType == RebarDeformationType.Deformed);
                if (deformed is not null) return deformed;
            }

            // Prefer name containing the diameter number
            string numStr = targetDiameterMm.ToString("0");
            var nameMatch = candidates.FirstOrDefault(b => b.Name.Contains(numStr));
            if (nameMatch is not null) return nameMatch;

            return candidates[0];
        }

        // Step 3: Closest diameter fallback
        return _barTypes
            .OrderBy(b => Math.Abs(b.DiameterMm - targetDiameterMm))
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds a hook type matching the requested bend angle (e.g. 90 or 135).
    /// </summary>
    public RebarHookType? FindHook(int angleDegrees, RebarStyle style = RebarStyle.Standard)
    {
        double targetRad = angleDegrees * Math.PI / 180.0;
        return _hookTypes
            .Where(h => h.Style == style && (Math.Abs(h.HookType.HookAngle - targetRad) < 1e-2 || h.Name.Contains(angleDegrees.ToString())))
            .Select(h => h.HookType)
            .FirstOrDefault()
            ?? _hookTypes.FirstOrDefault(h => Math.Abs(h.HookType.HookAngle - targetRad) < 1e-2)?.HookType;
    }

    /// <summary>
    /// Builds interactive UI mapping items for all unique bar groups in a KataBeamRebarSpec.
    /// </summary>
    public IReadOnlyList<KataBarTypeMappingItem> BuildMappingItems(KataBeamRebarSpec spec)
    {
        var items = new List<KataBarTypeMappingItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddItem(string role, string notation, double diameter, bool isLongitudinal)
        {
            if (diameter <= 0.0) return;
            string key = $"{role}_{diameter:0.#}";
            if (!seen.Add(key)) return;

            var resolved = ResolveBarType(notation, diameter, isLongitudinal);
            items.Add(new KataBarTypeMappingItem
            {
                Role = role,
                Notation = string.IsNullOrWhiteSpace(notation) ? $"Ø{diameter:0.#}" : notation,
                DiameterMm = diameter,
                IsLongitudinal = isLongitudinal,
                SelectedType = resolved,
                AvailableTypes = _barTypes
            });
        }

        if (!spec.TopContinuous.IsEmpty)
            AddItem("Thép chủ trên", spec.TopContinuous.RawNotation, spec.TopContinuous.Diameter, true);

        if (!spec.BottomContinuous.IsEmpty)
            AddItem("Thép chủ dưới", spec.BottomContinuous.RawNotation, spec.BottomContinuous.Diameter, true);

        foreach (var supp in spec.Supports)
        {
            int lIdx = 1;
            foreach (var layerList in supp.AllTopExtraLayers)
            {
                foreach (var barItem in layerList)
                {
                    if (!barItem.IsEmpty)
                        AddItem($"Tăng cường gối L{lIdx}", barItem.RawNotation, barItem.Diameter, true);
                }
                lIdx++;
            }
        }

        foreach (var span in spec.Spans)
        {
            int lIdx = 1;
            foreach (var layerList in span.AllBottomExtraLayers)
            {
                foreach (var barItem in layerList)
                {
                    if (!barItem.IsEmpty)
                        AddItem($"Tăng cường nhịp L{lIdx}", barItem.RawNotation, barItem.Diameter, true);
                }
                lIdx++;
            }

            foreach (var sideBar in span.SideBars)
            {
                if (!sideBar.IsEmpty)
                    AddItem("Thép giá nhịp", sideBar.RawNotation, sideBar.Diameter, true);
            }
        }

        foreach (var sideBar in spec.GlobalSideBars)
        {
            if (!sideBar.IsEmpty)
                AddItem("Thép giá chung", sideBar.RawNotation, sideBar.Diameter, true);
        }

        if (spec.GlobalStirrup.Diameter > 0.0)
            AddItem("Cốt đai", $"Ø{spec.GlobalStirrup.Diameter:0.#}", spec.GlobalStirrup.Diameter, false);

        return items;
    }
}
