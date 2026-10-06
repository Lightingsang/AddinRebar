using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitUnits = HPRebar.KataExport.Service.RevitUnits;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Gives the picked beams the stirrup cover of J9 (b, "30/25" → 25) as their Rebar Cover the way Revit's Cover tool does
/// with "Pick Elements": one cover type on the element (top, bottom, other) and on every exposed face, faces set one by
/// one with "Pick Faces" included, so Revit's cover matches where Kata Rebar puts the stirrups. A model without a cover
/// type of that distance gets one, "Rebar Cover &lt;b&gt;mm" (user decision 2026-10-06).
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

        var type = CoverType(doc, stirrupCoverMm, warnings);
        int changed = 0;
        foreach (var host in hosts)
            changed += SetOn(host, type);

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

    /// <summary>The model's cover type of that distance, or a new one named after it.</summary>
    private static RebarCoverType CoverType(Document doc, double coverMm, List<string> warnings)
    {
        var types = new FilteredElementCollector(doc).OfClass(typeof(RebarCoverType)).Cast<RebarCoverType>().ToList();
        var match = types.Where(t => Math.Abs(RevitUnits.FtToMm(t.CoverDistance) - coverMm) <= MatchToleranceMm)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .FirstOrDefault();
        if (match is not null) return match;

        string name = $"Rebar Cover {coverMm:0.#}mm";
        var names = new HashSet<string>(types.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        for (int i = 2; names.Contains(name); i++) name = $"Rebar Cover {coverMm:0.#}mm ({i})";

        var created = RebarCoverType.Create(doc, name, RevitUnits.MmToFt(coverMm));
        warnings.Add($"Model chưa có loại lớp bảo vệ {coverMm:0.#} mm (J9): đã tạo '{name}'.");
        Log.Information("Kata Rebar: created rebar cover type {Name} ({Cover} mm)", name, coverMm);
        return created;
    }

    /// <summary>
    /// The cover type on the element's three cover parameters and on every exposed face; returns how many of them
    /// held another type. Faces are counted first: setting a parameter already carries the faces that follow it.
    /// </summary>
    private static int SetOn(Element host, RebarCoverType type)
    {
        var data = RebarHostData.GetRebarHostData(host);
        bool hasFaces = data is not null && data.IsValidHost();
        int changed = hasFaces ? data!.GetExposedFaces().Count(f => data.GetCoverType(f)?.Id != type.Id) : 0;
        foreach (var face in Faces)
        {
            var parameter = host.get_Parameter(face);
            if (parameter is null || parameter.IsReadOnly || parameter.AsElementId() == type.Id) continue;
            parameter.Set(type.Id);
            changed++;
        }

        if (!hasFaces) return changed;

        data!.SetCommonCoverType(type);
        // A face "Pick Faces" set on its own that the common type did not reach is set directly.
        foreach (var face in data.GetExposedFaces().Where(f => data.GetCoverType(f)?.Id != type.Id))
            data.SetCoverType(face, type);
        return changed;
    }
}
