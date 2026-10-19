namespace HPBim.Cli.Estimation;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Models;
using HPBim.Core.Tcvn;

/// <summary>
/// Mathematical takeoff calculator for concrete, formwork, rebar, and MEP quantities.
/// Pure host-free logic.
/// </summary>
public sealed class BoqEstimator
{
    public BoqReport Estimate(BimModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        var concrete = new Dictionary<string, double>();
        var formwork = new Dictionary<string, double>();

        ComputeColumns(model, concrete, formwork);
        ComputeBeams(model, concrete, formwork);
        ComputeSlabs(model, concrete, formwork);
        ComputeWalls(model, concrete, formwork);

        var (smallRebar, largeRebar) = TcvnRebarCalculator.PartitionWeightByDiameter(model.Rebars, 10.0);
        var rebarByZone = ComputeRebarByZone(model.Rebars);
        var fixtures = ComputeFixtures(model.Fixtures);
        var pipes = ComputePipes(model.Pipes);

        return new BoqReport(
            TotalConcreteM3: Math.Round(concrete.Values.Sum(), 3),
            TotalFormworkM2: Math.Round(formwork.Values.Sum(), 2),
            TotalRebarKg: Math.Round(model.TotalRebarWeightKg, 1),
            SmallRebarKg: Math.Round(smallRebar, 1),
            LargeRebarKg: Math.Round(largeRebar, 1),
            ConcreteByComponent: concrete,
            FormworkByComponent: formwork,
            RebarByZone: rebarByZone,
            FixturesByCategory: fixtures,
            Pipes: pipes);
    }

    private static void ComputeColumns(BimModel model, Dictionary<string, double> c, Dictionary<string, double> f)
    {
        double vol = 0.0, fw = 0.0;
        foreach (var col in model.Columns ?? Array.Empty<ColumnDef>())
        {
            var baseLvl = model.GetLevel(col.BaseLevel);
            var topLvl = model.GetLevel(col.TopLevel);
            double hM = topLvl != null && baseLvl != null ? Math.Max(0.1, topLvl.ElevationM - baseLvl.ElevationM) : 3.3;
            double wM = col.WidthMm / 1000.0, dM = col.DepthMm / 1000.0;
            vol += wM * dM * hM;
            fw += 2.0 * (wM + dM) * hM;
        }
        c["Columns"] = Math.Round(vol, 3);
        f["Columns"] = Math.Round(fw, 2);
    }

    private static void ComputeBeams(BimModel model, Dictionary<string, double> c, Dictionary<string, double> f)
    {
        double vol = 0.0, fw = 0.0;
        foreach (var b in model.Beams ?? Array.Empty<BeamDef>())
        {
            double lM = b.LengthMm / 1000.0, wM = b.WidthMm / 1000.0, hM = b.HeightMm / 1000.0;
            vol += wM * hM * lM;
            fw += (wM + (2.0 * hM)) * lM;
        }
        c["Beams"] = Math.Round(vol, 3);
        f["Beams"] = Math.Round(fw, 2);
    }

    private static void ComputeSlabs(BimModel model, Dictionary<string, double> c, Dictionary<string, double> f)
    {
        double vol = 0.0, fw = 0.0;
        foreach (var s in model.Slabs ?? Array.Empty<SlabDef>())
        {
            double aM2 = s.NetAreaM2, tM = s.ThicknessMm / 1000.0;
            vol += aM2 * tM;
            fw += aM2;
        }
        c["Slabs"] = Math.Round(vol, 3);
        f["Slabs"] = Math.Round(fw, 2);
    }

    private static void ComputeWalls(BimModel model, Dictionary<string, double> c, Dictionary<string, double> f)
    {
        double vol = 0.0, fw = 0.0;
        foreach (var w in model.Walls ?? Array.Empty<WallDef>())
        {
            var baseLvl = model.GetLevel(w.BaseLevel);
            var topLvl = model.GetLevel(w.TopLevel);
            double hM = topLvl != null && baseLvl != null ? Math.Max(0.1, topLvl.ElevationM - baseLvl.ElevationM) : 3.3;
            double grossArea = (w.LengthMm / 1000.0) * hM;
            double openArea = (w.Openings?.Sum(o => (o.WidthMm / 1000.0) * (o.HeightMm / 1000.0)) ?? 0.0);
            double netArea = Math.Max(0.0, grossArea - openArea);
            vol += netArea * (w.ThicknessMm / 1000.0);
            fw += 2.0 * netArea;
        }
        c["Walls"] = Math.Round(vol, 3);
        f["Walls"] = Math.Round(fw, 2);
    }

    private static Dictionary<string, double> ComputeRebarByZone(IReadOnlyList<RebarDef>? rebars)
    {
        var dict = new Dictionary<string, double>();
        if (rebars == null) return dict;
        foreach (var grp in rebars.GroupBy(r => r.Zone ?? "General"))
        {
            dict[grp.Key] = Math.Round(grp.Sum(r => r.TotalWeightKg), 1);
        }
        return dict;
    }

    private static Dictionary<string, int> ComputeFixtures(IReadOnlyList<MepFixtureDef>? fixtures)
    {
        var dict = new Dictionary<string, int>();
        if (fixtures == null) return dict;
        foreach (var grp in fixtures.GroupBy(f => f.Category.ToString()))
        {
            dict[grp.Key] = grp.Count();
        }
        return dict;
    }

    private static List<PipeTakeoffItem> ComputePipes(IReadOnlyList<PipeRunDef>? pipes)
    {
        var list = new List<PipeTakeoffItem>();
        if (pipes == null) return list;
        foreach (var p in pipes)
        {
            list.Add(new PipeTakeoffItem(p.SystemType.ToString(), p.DiameterMm, p.Material, Math.Round(p.TotalLengthMm / 1000.0, 2)));
        }
        return list;
    }
}
