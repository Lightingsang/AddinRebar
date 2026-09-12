using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Puts the computed reinforcement into the model.
///
///     Ties go in first so the main bars can be seen against them, and each kind gets its own transaction
///     so a failure leaves a clean boundary. This service never opens a TransactionGroup: the caller owns
///     the grouping, which is what keeps the whole run a single undo step.
/// </summary>
public static class RebarCreationService
{
    /// <summary>
    ///     Checks everything that could stop the run before the first transaction opens, so a missing
    ///     family never leaves a half-reinforced model behind.
    /// </summary>
    public static ValidationResult CanCreate(
        RebarShapeResolver shapes,
        ColumnStack stack,
        IReadOnlyList<ColumnRebarSpec> specs)
    {
        if (specs.Count != stack.Sections.Count)
        {
            throw new ArgumentException("One spec is required per column segment.", nameof(specs));
        }

        var needsCrossTies = specs.Any(spec =>
            (spec.Ties.AddH && spec.Ties.TypeH != 0) || (spec.Ties.AddV && spec.Ties.TypeV != 0));

        var shape = stack.Sections[0].Shape;

        return shapes.Require(shape, needsCrossTies);
    }

    /// <summary>Total number of Revit elements the run will create, for a progress bar.</summary>
    public static int PlannedCount(ColumnStack stack, IReadOnlyList<ColumnRebarSpec> specs)
    {
        var total = 0;

        for (var i = 0; i < stack.Sections.Count; i++)
        {
            var section = stack.Sections[i];
            var spec = specs[i];
            var runs = RunsFor(section, spec);

            total += runs.Count;
            total += CrossTieCount(section, spec, runs.Count);
            total += spec.Layout.BarCount;
        }

        return total;
    }

    public static CreatedRebar Create(
        Document document,
        ColumnStack stack,
        IReadOnlyList<ColumnRebarSpec> specs,
        RebarShapeResolver shapes,
        IProgress<int>? progress = null)
    {
        var mapper = PointMapper.For(stack);
        var stirrups = new List<Rebar>();
        var ties = new List<Rebar>();
        var mainBars = new List<Rebar>();
        var done = 0;

        using (var transaction = new Transaction(document, "Create Stirrup Bars"))
        {
            transaction.Start();
            RebarFailureHandling.Apply(transaction);

            for (var i = 0; i < stack.Sections.Count; i++)
            {
                var section = stack.Sections[i];
                var spec = specs[i];
                var faces = stack.Faces[i];
                var runs = RunsFor(section, spec);

                stirrups.AddRange(StirrupCreator.Create(
                    document, faces, section,
                    shapes.MainTie(section.Shape)!,
                    spec.StirrupBarType.BarType,
                    spec.Layout.Cover, runs, spec.PartitionName));

                progress?.Report(done += runs.Count);

                var created = AdditionalTieCreator.Create(
                    document, faces, section, shapes,
                    spec.TieBarType.BarType,
                    spec.TieBarType.DiameterMm,
                    spec.StirrupBarType.DiameterMm,
                    spec.Layout.Cover, spec.Ties, runs, spec.PartitionName);

                ties.AddRange(created);
                progress?.Report(done += created.Count);
            }

            transaction.Commit();
        }

        using (var transaction = new Transaction(document, "Create Main Bars"))
        {
            transaction.Start();
            RebarFailureHandling.Apply(transaction);

            foreach (var polyline in BuildPolylines(stack, specs))
            {
                mainBars.Add(MainBarCreator.Create(
                    document,
                    polyline.Host,
                    polyline.BarType,
                    polyline.Bar,
                    mapper,
                    polyline.PartitionName));

                progress?.Report(++done);
            }

            transaction.Commit();
        }

        Log.Information(
            "Column Rebar created {Stirrups} tie(s), {Ties} cross-tie(s) and {Bars} main bar(s)",
            stirrups.Count, ties.Count, mainBars.Count);

        return new CreatedRebar { MainBars = mainBars, Stirrups = stirrups, Ties = ties };
    }

    /// <summary>Main-bar centre-lines for the whole stack, each paired with the column that hosts it.</summary>
    private static IEnumerable<HostedBar> BuildPolylines(ColumnStack stack, IReadOnlyList<ColumnRebarSpec> specs)
    {
        for (var i = 0; i < stack.Sections.Count; i++)
        {
            var section = stack.Sections[i];
            var spec = specs[i];
            var above = i + 1 < stack.Sections.Count ? stack.Sections[i + 1] : null;
            var stirrupAbove = above is null ? spec.StirrupBarType : specs[i + 1].StirrupBarType;

            var bars = BarLayoutCalculator.Compute(section, spec.Layout);

            var upperPositions = SpliceCalculator.ComputeUpperPositions(
                above, spec.Layout,
                spec.StirrupBarType.DiameterMm,
                stirrupAbove.DiameterMm,
                bars, spec.Splices);

            for (var b = 0; b < bars.Count; b++)
            {
                yield return new HostedBar(
                    stack.Faces[i].Element,
                    spec.MainBarType.BarType,
                    BarPolylineBuilder.Build(section, spec.Layout, bars[b], spec.Splices[b], upperPositions[b], spec.MainBarType.Name),
                    spec.PartitionName);
            }
        }
    }

    private static IReadOnlyList<StirrupRun> RunsFor(ColumnSection section, ColumnRebarSpec spec)
    {
        var length = StirrupDistributionCalculator.ComputeRunLength(section, spec.Stirrups.IsTiesUp);

        return StirrupDistributionCalculator.Compute(length, spec.Stirrups);
    }

    private static int CrossTieCount(ColumnSection section, ColumnRebarSpec spec, int runCount)
    {
        var tie = spec.Ties;
        var count = 0;

        if (section.Shape == SectionShape.Rectangle)
        {
            if (tie.AddH) count += tie.TypeH == 0 ? tie.AH == 0 ? 0 : runCount : tie.NH * runCount;
            if (tie.AddV) count += tie.TypeV == 0 ? tie.AV == 0 ? 0 : runCount : tie.NV * runCount;
        }
        else
        {
            if (tie.AddH) count += runCount;

            // The circular cross-tie places a pair, one on each axis.
            if (tie.AddV) count += 2 * runCount;
        }

        return count;
    }

    private readonly struct HostedBar
    {
        public HostedBar(Element host, RebarBarType barType, BarPolyline bar, string partitionName)
        {
            Host = host;
            BarType = barType;
            Bar = bar;
            PartitionName = partitionName;
        }

        public Element Host { get; }

        public RebarBarType BarType { get; }

        public BarPolyline Bar { get; }

        public string PartitionName { get; }
    }
}
