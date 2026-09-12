using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Sole owner of the master TransactionGroup("Beam Rebar").
/// Coordinates preflight checks, view generation, dimensions, rebar creation, and schedule annotations.
/// </summary>
public sealed class BeamRebarOrchestrator
{
    private readonly Document _document;
    private readonly BeamStack _stack;
    private readonly RebarShapeResolver _shapes;
    private readonly BeamAnnotationSettings _settings;
    private readonly RebarTypeCatalog _catalog;

    public BeamRebarOrchestrator(
        Document document,
        BeamStack stack,
        RebarShapeResolver shapes,
        BeamAnnotationSettings settings)
    {
        _document = document;
        _stack = stack;
        _shapes = shapes;
        _settings = settings;
        _catalog = new RebarTypeCatalog(document);
    }

    public BeamRebarOrchestrator(
        Document document,
        BeamStack stack,
        IReadOnlyList<BeamFaces> faces,
        RebarShapeResolver shapes,
        BeamAnnotationSettings settings)
        : this(document, stack, shapes, settings)
    {
    }

    public int PlannedCount(BeamRebarSpec spec) =>
        1                                                       // Elevation detail view
        + _stack.Spans.Count * _settings.SectionsPerSpan        // Cross section views
        + DimensionCreator.PlannedCount(_stack.ContinuousStack) // Dimensions
        + _stack.Spans.Count * _settings.SectionsPerSpan        // Bar tables
        + RebarCreationService.PlannedCount(_stack, spec);      // Rebar elements

    public BeamOrchestratorResult Run(BeamRebarSpec spec, IProgress<int>? progress = null)
    {
        var ready = RebarCreationService.CanCreate(_shapes, _stack, spec);
        if (!ready.IsOk)
        {
            return BeamOrchestratorResult.Invalid(ready);
        }

        using var group = new TransactionGroup(_document, "Beam Rebar");
        group.Start();

        try
        {
            int done = 0;

            // 1. Create Views
            var views = CreateViews(progress, ref done);

            // 2. Create Dimensions
            CreateDimensions(views, progress, ref done);

            // 3. Create Reinforcement Elements
            var rebar = RebarCreationService.Create(
                _document, _stack, spec, _shapes, _catalog,
                new Progress<int>(v => progress?.Report(done + v)));

            done += rebar.Total;

            // 4. Create Schedule Tables
            CreateTables(views, spec, progress, ref done);

            group.Assimilate();

            Log.Information("Beam Rebar completed successfully: {Views} view(s), {Rebar} rebar element(s).",
                views.Total, rebar.Total);

            return BeamOrchestratorResult.Success(views, rebar);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Beam Rebar creation failed; rolling back all document mutations.");
            group.RollBack();
            throw;
        }
    }

    private CreatedBeamViews CreateViews(IProgress<int>? progress, ref int done)
    {
        ViewSection? detailView = null;
        using (var t = new Transaction(_document, "Create Detail View"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            detailView = DetailViewCreator.Create(_document, _stack, _settings);
            t.Commit();
        }
        progress?.Report(++done);

        IReadOnlyList<ViewSection> sectionViews;
        using (var t = new Transaction(_document, "Create Section Views"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            sectionViews = SectionViewCreator.Create(_document, _stack, _settings);
            t.Commit();
        }
        done += sectionViews.Count;
        progress?.Report(done);

        return new CreatedBeamViews { DetailView = detailView, SectionViews = sectionViews };
    }

    private void CreateDimensions(CreatedBeamViews views, IProgress<int>? progress, ref int done)
    {
        if (views.DetailView is not null)
        {
            using var t = new Transaction(_document, "Create Elevation Dimensions");
            t.Start();
            RebarFailureHandling.Apply(t);
            done += DimensionCreator.CreateOnElevation(_document, views.DetailView, _stack, _settings);
            t.Commit();
            progress?.Report(done);
        }

        using (var t = new Transaction(_document, "Create Section Dimensions"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            int viewIdx = 0;
            for (int spanIdx = 0; spanIdx < _stack.Spans.Count && spanIdx < _stack.Faces.Count; spanIdx++)
            {
                var span = _stack.Spans[spanIdx];
                int cutCount = SectionViewCreator.ComputeCutStations(span, _settings.SectionsPerSpan).Count;
                for (int cut = 0; cut < cutCount && viewIdx < views.SectionViews.Count; cut++)
                {
                    done += DimensionCreator.CreateOnSection(
                        _document, views.SectionViews[viewIdx], _stack.Faces[spanIdx], span, _settings);
                    viewIdx++;
                }
            }
            t.Commit();
        }
        progress?.Report(done);
    }

    private void CreateTables(CreatedBeamViews views, BeamRebarSpec spec, IProgress<int>? progress, ref int done)
    {
        using var t = new Transaction(_document, "Create Beam Tables");
        t.Start();
        RebarFailureHandling.Apply(t);

        int viewIndex = 0;
        for (int spanIndex = 0; spanIndex < _stack.Spans.Count; spanIndex++)
        {
            var span = _stack.Spans[spanIndex];
            int cutCount = SectionViewCreator.ComputeCutStations(span, _settings.SectionsPerSpan).Count;
            for (int cutIndex = 0; cutIndex < cutCount && viewIndex < views.SectionViews.Count; cutIndex++)
            {
                RebarTableTagCreator.Create(
                    _document, views.SectionViews[viewIndex], span, spanIndex, cutIndex, spec, _settings);
                viewIndex++;
                done++;
            }
        }

        t.Commit();
        progress?.Report(done);
    }
}
