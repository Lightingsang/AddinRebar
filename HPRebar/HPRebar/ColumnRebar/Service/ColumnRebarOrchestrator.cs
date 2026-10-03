using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     The whole Column Rebar action, start to finish.
///
///     This is the only place that opens a TransactionGroup: everything the run creates lands in one undo
///     step, and any failure rolls the lot back rather than leaving a half-annotated column behind. The
///     individual creators open their own named transactions inside it, each one clearing Revit's warnings
///     to the log so no dialog can stop the run half way; an error still comes through and rolls the group
///     back. See <see cref="RebarFailureHandling"/>.
///
///     Views are made before the reinforcement, as the original tool did, so the cross-sections exist by
///     the time the bar tables are written into them.
/// </summary>
public sealed class ColumnRebarOrchestrator
{
    private readonly Document _document;
    private readonly ColumnStack _stack;
    private readonly RebarShapeResolver _shapes;
    private readonly AnnotationSettings _settings;

    public ColumnRebarOrchestrator(
        Document document,
        ColumnStack stack,
        RebarShapeResolver shapes,
        AnnotationSettings settings)
    {
        _document = document;
        _stack = stack;
        _shapes = shapes;
        _settings = settings;
    }

    /// <summary>Everything the run will create, for sizing the progress bar.</summary>
    public int PlannedCount(IReadOnlyList<ColumnRebarSpec> specs) =>
        2                                             // the two elevations
        + _stack.Sections.Count                       // one cross-section each
        + DimensionCreator.PlannedCount(_stack)
        + _stack.Sections.Count                       // one bar table each
        + RebarCreationService.PlannedCount(_stack, specs);

    /// <summary>
    ///     Builds the reinforcement and its drawings. Anything that would stop the run is checked before the
    ///     first transaction opens, so a missing family never leaves a partly built model.
    /// </summary>
    public OrchestratorResult Run(
        IReadOnlyList<ColumnRebarSpec> specs,
        IProgress<int>? progress = null,
        ViewNaming? naming = null)
    {
        if (naming is not null)
        {
            _settings.DetailViewName = naming.DetailViewName;
            _settings.SectionSuffix = naming.SectionSuffix;
        }

        var ready = RebarCreationService.CanCreate(_shapes, _stack, specs);

        if (!ready.IsOk) return OrchestratorResult.Invalid(ready);

        using var group = new TransactionGroup(_document, "Column Rebar");
        group.Start();

        try
        {
            var done = 0;

            var views = CreateViews(specs, progress, ref done);

            CreateDimensions(views, progress, ref done);

            var rebar = RebarCreationService.Create(
                _document, _stack, specs, _shapes,
                new Progress<int>(value => progress?.Report(done + value)));

            done += rebar.Total;

            CreateTables(views, specs, progress, ref done);

            group.Assimilate();

            Log.Information(
                "Column Rebar finished: {Views} view(s), {Rebar} rebar element(s)",
                views.Total, rebar.Total);

            return new OrchestratorResult { Views = views, Rebar = rebar };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Column Rebar failed; the model was rolled back");
            group.RollBack();

            throw;
        }
    }

    private CreatedViews CreateViews(IReadOnlyList<ColumnRebarSpec> specs, IProgress<int>? progress, ref int done)
    {
        ViewSection? detailX;
        ViewSection? detailY;

        using (var transaction = new Transaction(_document, "Create Detail View"))
        {
            transaction.Start();
            RebarFailureHandling.Apply(transaction);
            (detailX, detailY) = DetailViewCreator.Create(_document, _stack, _settings);
            transaction.Commit();
        }

        progress?.Report(done += 2);

        IReadOnlyList<ViewSection> sections;

        using (var transaction = new Transaction(_document, "Create Section View"))
        {
            transaction.Start();
            RebarFailureHandling.Apply(transaction);
            sections = SectionViewCreator.Create(_document, _stack, specs, _settings);
            transaction.Commit();
        }

        progress?.Report(done += sections.Count);

        return new CreatedViews { DetailX = detailX, DetailY = detailY, Sections = sections };
    }

    private void CreateDimensions(CreatedViews views, IProgress<int>? progress, ref int done)
    {
        using (var transaction = new Transaction(_document, "Create Dimension View"))
        {
            transaction.Start();
            RebarFailureHandling.Apply(transaction);

            if (views.DetailX is not null)
            {
                done += DimensionCreator.CreateOnElevation(_document, views.DetailX, _stack, _settings, acrossWidth: true);
            }

            if (views.DetailY is not null)
            {
                done += DimensionCreator.CreateOnElevation(_document, views.DetailY, _stack, _settings, acrossWidth: false);
            }

            transaction.Commit();
        }

        progress?.Report(done);

        using (var transaction = new Transaction(_document, "Create Dimension Section"))
        {
            transaction.Start();
            RebarFailureHandling.Apply(transaction);

            for (var i = 0; i < views.Sections.Count; i++)
            {
                done += DimensionCreator.CreateOnSection(_document, views.Sections[i], _stack, i, _settings);
            }

            transaction.Commit();
        }

        progress?.Report(done);
    }

    private void CreateTables(
        CreatedViews views,
        IReadOnlyList<ColumnRebarSpec> specs,
        IProgress<int>? progress,
        ref int done)
    {
        using var transaction = new Transaction(_document, "Create Tag Bars");
        transaction.Start();
        RebarFailureHandling.Apply(transaction);

        for (var i = 0; i < views.Sections.Count; i++)
        {
            RebarTableTagCreator.Create(_document, views.Sections[i], _stack, i, specs[i], _settings);
            done++;
        }

        transaction.Commit();

        progress?.Report(done);
    }
}
