using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.KataRebar.Service;
using Serilog;

namespace HPRebar.KataExport.Service;

/// <summary>What one Kata Column Rebar run did.</summary>
public sealed record KataColumnRebarResult(bool Success, int Created, int Deleted, IReadOnlyList<string> Warnings, string Error);

/// <summary>
/// Turns a <see cref="KataColumnRebarDesign"/> into Revit rebar on the picked columns: bars of the previous run are
/// deleted, the cover is set, then main bars, hoop sets and tie sets are created and numbered, each step its own
/// transaction inside one group (one Undo); a failing step rolls the whole group back. Revit 2026 only, like the
/// command (sheet "Cot" tools are web pages on 2026).
/// </summary>
public sealed class KataColumnRebarCreator
{
    private const double HookAngleDegrees = 135;
    private const double HookTailFactor = 6;

    private readonly Document _doc;
    private readonly IReadOnlyList<FamilyInstance> _columns;
    private readonly Transform _frame;
    private readonly KataColumnSpec _spec;
    private readonly KataColumnRebarDesign _design;
    private readonly IReadOnlyList<KataColumnStoreyGeometry> _storeys;
    private readonly Dictionary<double, RebarBarType> _barTypes;

    /// <summary>What the run created, for the views' tags.</summary>
    private readonly List<KataColumnCreatedRebar> _created = new();

    private int _hooklessRoundHoops;

    public KataColumnRebarCreator(
        Document doc,
        KataColumnStackReading reading,
        IReadOnlyList<KataColumnStoreyGeometry> storeys,
        KataColumnSpec spec,
        KataColumnRebarDesign design,
        Dictionary<double, RebarBarType> barTypes)
    {
        _doc = doc;
        _columns = reading.Columns;
        _frame = reading.Frame ?? throw new ArgumentException("The column stack has no plan frame.", nameof(reading));
        _storeys = storeys;
        _spec = spec;
        _design = design;
        _barTypes = barTypes;
    }

    public KataColumnRebarResult Create()
    {
        string name = string.IsNullOrWhiteSpace(_spec.Name) ? "cột" : _spec.Name;
        var hosts = _columns.Cast<Element>().ToList();
        var runner = new KataTransactionRunner(_doc);
        var hook = KataRebarHookResolver.Find(_doc, (int)HookAngleDegrees, HookTailFactor);
        var warnings = new List<string>();
        if (hook is null)
        {
            warnings.Add("Model không có kiểu móc 135°: đai bao và đai trong được vẽ không móc.");
        }

        using var group = new TransactionGroup(_doc, $"Kata Column Rebar - {name}");
        group.Start();
        try
        {
            int deleted = runner.Run("Kata Column Rebar: xoá thép cũ", () => KataRebarCleanupService.DeletePrevious(_doc, hosts));
            warnings.AddRange(runner.Run("Kata Column Rebar: lớp bảo vệ", () => KataHostCoverService.Apply(_doc, hosts, _spec.CoverMm)));
            int bars = runner.Run("Kata Column Rebar: thép dọc", () => _design.Bars.Count(CreateMainBar));
            var roundHoops = new KataColumnRoundHoopFactory(_doc);
            int hoops = runner.Run("Kata Column Rebar: đai", () => _design.Storeys.Sum(s => CreateHoops(s, hook, roundHoops)));
            if (_hooklessRoundHoops > 0)
            {
                warnings.Add($"{_hooklessRoundHoops} bộ đai tròn vẽ không móc: model không có hình đai tròn có đoạn chồng (như M_T3) và Revit không nhận đai tròn có móc.");
            }
            int ties = runner.Run("Kata Column Rebar: đai trong, móc C", () => _design.Storeys.Sum(s => CreateTies(s, hook)));
            warnings.AddRange(runner.Run("Kata Column Rebar: số hiệu", () => KataRebarNumberAssigner.Apply(_doc, hosts)));
            string drawn = new KataColumnViewsStep(_doc, _columns, _frame, _spec, _storeys, _design).Draw(runner, name, _created, warnings);
            warnings.AddRange(runner.Warnings);

            var status = group.Assimilate();
            if (status != TransactionStatus.Committed)
            {
                throw new InvalidOperationException($"Revit không gộp được các bước thành một lần Undo ({status}).");
            }

            Log.Information("Kata Column Rebar {Name}: {Bars} bars, {Hoops} hoop sets, {Ties} tie sets, {Deleted} deleted, {Drawn}",
                name, bars, hoops, ties, deleted, drawn);
            return new KataColumnRebarResult(true, bars + hoops + ties, deleted, warnings, string.Empty);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (group.HasStarted())
            {
                group.RollBack();
            }

            Log.Error(ex, "Kata Column Rebar {Name} failed", name);
            return new KataColumnRebarResult(false, 0, 0, warnings, ex.Message);
        }
    }

    private bool CreateMainBar(KataColumnNumberedBar bar)
    {
        var run = bar.Run;
        var host = _columns[run.Storey];
        var barType = _barTypes[run.DiameterMm];
        var points = run.Points.Select(ToModel).ToList();
        var curves = points.Zip(points.Skip(1), (a, b) => (Curve)Line.CreateBound(a, b)).ToList();

        Rebar rebar;
        if (KataColumnFrame.PlaneNormal(points, _frame.BasisX) is { } normal)
        {
            rebar = KataRebarCurveFactory.Create(_doc, RebarStyle.Standard, barType, host, normal, curves);
        }
        else
        {
            var result = Rebar.CreateFreeForm(_doc, barType, host, new List<CurveLoop> { CurveLoop.Create(curves) }, RebarStyle.Standard);
            rebar = result.Error == RebarFreeFormValidationResult.Success && result.Rebar is not null
                ? result.Rebar
                : throw new InvalidOperationException($"Revit không tạo được thanh {bar.Number} ({run.Kind}, tầng {run.Storey + 1}): {result.Error}.");
        }

        KataRebarStamp.Apply(rebar, host, _spec.Name, bar.Number);
        if (run.Kind != KataColumnBarKind.Starter)
        {
            var g = _storeys[run.Storey];
            var (x, y) = KataColumnFrame.PlumbPosition(run.Points);
            _created.Add(new KataColumnCreatedRebar(rebar, run.Storey, KataColumnRebarRole.Bar, x - g.MinXMm, y - g.MinYMm, 0, 0));
        }

        return true;
    }

    private int CreateHoops(KataColumnStoreyDesign storey, KataHookChoice? hook, KataColumnRoundHoopFactory roundHoops)
    {
        var host = _columns[storey.Storey];
        var g = _storeys[storey.Storey];
        var barType = _barTypes[_spec.Hoops.DiameterMm];
        double offset = _spec.CoverMm + _spec.Hoops.DiameterMm / 2;
        bool round = _spec.Rows[storey.Storey].IsRound;
        int sets = 0;
        foreach (var zone in storey.HoopZones)
        {
            double z = g.BaseMm + zone.FromMm;
            var centre = ToModel(new KataPoint3((g.MinXMm + g.MaxXMm) / 2, (g.MinYMm + g.MaxYMm) / 2, z));
            Rebar rebar;
            if (round)
            {
                double radius = RevitUnits.MmToFt((g.MaxXMm - g.MinXMm) / 2 - offset);
                rebar = roundHoops.Create(barType, hook, host, centre, radius, _frame.BasisX, _frame.BasisY);
                _hooklessRoundHoops += roundHoops.LastWithoutHooks ? 1 : 0;
            }
            else
            {
                var curves = Rectangle(g.MinXMm + offset, g.MaxXMm - offset, g.MinYMm + offset, g.MaxYMm - offset, z);
                rebar = Tie(barType, hook, host, curves, centre);
            }

            Lay(rebar, zone);
            KataRebarStamp.Apply(rebar, host, _spec.Name, storey.HoopNumber);
            _created.Add(new KataColumnCreatedRebar(rebar, storey.Storey, KataColumnRebarRole.Hoop, 0, 0, g.BaseMm + zone.FromMm, g.BaseMm + zone.ToMm));
            sets++;
        }

        return sets;
    }

    private int CreateTies(KataColumnStoreyDesign storey, KataHookChoice? hook)
    {
        if (storey.TieSet is not { } set)
        {
            return 0;
        }

        var host = _columns[storey.Storey];
        var g = _storeys[storey.Storey];
        var barType = _barTypes[_spec.Ties.DiameterMm];
        var bars = storey.Section.Bars;
        double z = g.BaseMm + set.Zone.FromMm;
        var centre = ToModel(new KataPoint3((g.MinXMm + g.MaxXMm) / 2, (g.MinYMm + g.MaxYMm) / 2, z));
        foreach (var (tie, number) in set.Ties)
        {
            var curves = TieCurves(tie, bars, g, z);
            var rebar = Tie(barType, hook, host, curves, centre);
            Lay(rebar, set.Zone);
            KataRebarStamp.Apply(rebar, host, _spec.Name, number);
            var (lx, ly) = TieLabel(tie, bars, g);
            _created.Add(new KataColumnCreatedRebar(rebar, storey.Storey, KataColumnRebarRole.Tie, lx, ly, 0, 0));
        }

        return set.Ties.Count;
    }

    /// <summary>
    /// Where a tie's tag goes (section mm): inside a closed tie next to its long side's middle, so a tall and a wide tie
    /// crossing at the centre keep their tags apart; at a crosstie's middle.
    /// </summary>
    private (double X, double Y) TieLabel(KataColumnTie tie, IReadOnlyList<KataColumnBar> bars, KataColumnStoreyGeometry g)
    {
        if (KataColumnTieGeometry.Box(tie, bars, _spec.Ties.DiameterMm) is not { } box)
        {
            return ((g.MaxXMm - g.MinXMm) / 2, (g.MaxYMm - g.MinYMm) / 2);
        }

        var (x1, y1, x2, y2) = KataColumnTieGeometry.Outline(tie, box);
        double inset = 0.15 * Math.Max(g.MaxXMm - g.MinXMm, g.MaxYMm - g.MinYMm);
        if (tie.Kind == KataColumnTieKind.Crosstie)
        {
            return ((x1 + x2) / 2, (y1 + y2) / 2);
        }

        return y2 - y1 >= x2 - x1 ? ((x1 + x2) / 2, y2 - inset) : (x2 - inset, (y1 + y2) / 2);
    }

    /// <summary>
    /// A closed tie wraps the two restrained bars of each face line plus their Ø; a crosstie runs across the section
    /// through the restrained slot, hooked on the two face bars.
    /// </summary>
    private IList<Curve> TieCurves(KataColumnTie tie, IReadOnlyList<KataColumnBar> bars, KataColumnStoreyGeometry g, double z)
    {
        var box = KataColumnTieGeometry.Box(tie, bars, _spec.Ties.DiameterMm)
                  ?? throw new InvalidOperationException(
                      $"Đai trong {(tie.AlongB ? "theo b" : "theo h")} cần thanh ở hàng {tie.FirstSlot} và {tie.SecondSlot}, mặt cắt không có thanh ở đó.");
        var (x1, y1, x2, y2) = KataColumnTieGeometry.Outline(tie, box);
        if (tie.Kind == KataColumnTieKind.Crosstie)
        {
            var a = new KataPoint3(g.MinXMm + x1, g.MinYMm + y1, z);
            var b = new KataPoint3(g.MinXMm + x2, g.MinYMm + y2, z);
            return new List<Curve> { Line.CreateBound(ToModel(a), ToModel(b)) };
        }

        return Rectangle(g.MinXMm + x1, g.MinXMm + x2, g.MinYMm + y1, g.MinYMm + y2, z);
    }

    /// <summary>
    /// A hoop or tie in the horizontal plane, hooks turned towards the section centre; a crosstie (one straight leg,
    /// usually through the centre) turns both hooks to one side of its leg, as Kata draws its "C".
    /// </summary>
    private Rebar Tie(RebarBarType barType, KataHookChoice? hook, Element host, IList<Curve> curves, XYZ centre)
    {
        if (hook is null)
        {
            return KataRebarCurveFactory.Create(_doc, RebarStyle.StirrupTie, barType, host, XYZ.BasisZ, curves);
        }

        var toward = centre;
        if (curves.Count == 1)
        {
            var leg = curves[0];
            var along = (leg.GetEndPoint(1) - leg.GetEndPoint(0)).Normalize();
            toward = (leg.GetEndPoint(0) + leg.GetEndPoint(1)) / 2 + XYZ.BasisZ.CrossProduct(along);
        }

        return KataRebarCurveFactory.CreateHooked(_doc, hook.Style, barType, hook.Hook, host, XYZ.BasisZ, curves, toward);
    }

    /// <summary>Spreads a hoop or tie over its zone (first bar at the zone start, upwards).</summary>
    private static void Lay(Rebar rebar, KataColumnHoopZone zone)
    {
        int count = KataColumnRebarSummaryMapper.Count(zone);
        if (count > 1)
        {
            rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(count, RevitUnits.MmToFt(zone.SpacingMm), true, true, true);
        }
    }

    private IList<Curve> Rectangle(double minX, double maxX, double minY, double maxY, double z)
    {
        var corners = new[]
        {
            ToModel(new KataPoint3(minX, maxY, z)), ToModel(new KataPoint3(maxX, maxY, z)),
            ToModel(new KataPoint3(maxX, minY, z)), ToModel(new KataPoint3(minX, minY, z)),
        };
        return corners.Select((c, i) => (Curve)Line.CreateBound(c, corners[(i + 1) % 4])).ToList();
    }

    private XYZ ToModel(KataPoint3 p) => KataColumnFrame.ToModel(_frame, p.X, p.Y, p.Z);
}
