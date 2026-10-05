using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitUnits = HPRebar.KataExport.Service.RevitUnits;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Gives the picked beams the stirrup cover of J9 (b) as their Rebar Cover on every face (top, bottom, other), so
/// Revit's cover matches where Kata Rebar puts the stirrups. Only a cover type the model already has is used: none of
/// that distance → the beams keep theirs and the user is told which type to add.
/// </summary>
public static class KataHostCoverService
{
    private const double MatchToleranceMm = 0.5;

    private static readonly BuiltInParameter[] Faces =
    {
        BuiltInParameter.CLEAR_COVER_TOP,
        BuiltInParameter.CLEAR_COVER_BOTTOM,
        BuiltInParameter.CLEAR_COVER_OTHER
    };

    /// <summary>Call inside an open transaction, after the previous run's bars are deleted and before any bar is drawn.</summary>
    public static IReadOnlyList<string> Apply(Document doc, IReadOnlyList<Element> hosts, double stirrupCoverMm)
    {
        var warnings = new List<string>();
        if (stirrupCoverMm <= 0.0 || hosts.Count == 0) return warnings;

        var type = new FilteredElementCollector(doc).OfClass(typeof(RebarCoverType)).Cast<RebarCoverType>()
            .Where(t => Math.Abs(RevitUnits.FtToMm(t.CoverDistance) - stirrupCoverMm) <= MatchToleranceMm)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .FirstOrDefault();
        if (type is null)
        {
            warnings.Add($"Model chưa có loại lớp bảo vệ {stirrupCoverMm:0.#} mm (Structural Settings ▸ Rebar Cover Settings): dầm giữ lớp bảo vệ cũ.");
            Log.Warning("Kata Rebar: no rebar cover type of {Cover} mm, host cover left unchanged", stirrupCoverMm);
            return warnings;
        }

        int changed = 0;
        foreach (var host in hosts)
        {
            foreach (var face in Faces)
            {
                var parameter = host.get_Parameter(face);
                if (parameter is null || parameter.IsReadOnly || parameter.AsElementId() == type.Id) continue;
                parameter.Set(type.Id);
                changed++;
            }
        }

        if (changed > 0)
        {
            var hostIds = new HashSet<ElementId>(hosts.Select(h => h.Id));
            int others = new FilteredElementCollector(doc).OfClass(typeof(Rebar)).Cast<Rebar>().Count(r => hostIds.Contains(r.GetHostId()));
            if (others > 0)
                warnings.Add($"Đổi lớp bảo vệ dầm sang '{type.Name}': {others} thanh không do Kata Rebar vẽ trên dầm có thể dịch theo.");
        }

        Log.Information("Kata Rebar: host cover {Type} ({Cover} mm) on {Hosts} beams, {Changed} faces changed",
            type.Name, stirrupCoverMm, hosts.Count, changed);
        return warnings;
    }
}
