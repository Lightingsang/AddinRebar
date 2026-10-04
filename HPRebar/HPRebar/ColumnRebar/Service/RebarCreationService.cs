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
            (spec.Ties.AddH && spec.Ties.KindH != CrossTieKind.ClosedTie)
            || (spec.Ties.AddV && spec.Ties.KindV != CrossTieKind.ClosedTie));

        var shape = stack.Sections[0].Shape;

        return shapes.Require(shape, needsCrossTies);
    }

    /// <summary>Total number of Revit elements the run will create, for a progress bar.</summary>
    public static int PlannedCount(ColumnStack stack, IReadOnlyList<ColumnRebarSpec> specs)
    {
        var total = 0;

        for (var i = 0; i < stack.Sections.Count; i++)
        {
            var spec = specs[i];
            total += ColumnElementCount.Planned(stack.Sections[i], spec.Stirrups, spec.Ties, spec.Layout.BarCount);
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
                var runs = StirrupDistributionCalculator.ComputeRuns(section, spec.Stirrups);

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

            var polylines = ColumnBarPolylines.Compute(
                section, spec.Layout, spec.Splices, above,
                spec.StirrupBarType.DiameterMm, stirrupAbove.DiameterMm, spec.MainBarType.Name);

            foreach (var polyline in polylines)
            {
                yield return new HostedBar(stack.Faces[i].Element, spec.MainBarType.BarType, polyline, spec.PartitionName);
            }
        }
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
