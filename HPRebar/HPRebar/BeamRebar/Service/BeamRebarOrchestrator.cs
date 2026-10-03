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

    public int PlannedCount(BeamRebarSpec spec)
    {
        var views = spec.Views;
        int sections = views.CreateSectionViews
            ? SectionViewCreator.PlannedCount(_stack.Spans, views.SectionsPerSpan)
            : 0;

        return (views.CreateElevationView ? 1 : 0)
               + sections
               + (views.CreateDimensions ? DimensionCreator.PlannedCount(views.CreateElevationView, sections) : 0)
               + (views.CreateTables ? sections : 0)
               + RebarCreationService.PlannedCount(_stack, spec);
    }

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
            var options = spec.Views;
            var settings = _settings.ForRun(_document, options);

            // 1. Create Views
            var views = CreateViews(options, settings, progress, ref done);

            // 2. Create Dimensions
            if (options.CreateDimensions)
            {
                CreateDimensions(views, settings, progress, ref done);
            }

            // 3. Create Reinforcement Elements
            var rebar = RebarCreationService.Create(
                _document, _stack, spec, _shapes, _catalog,
                new Progress<int>(v => progress?.Report(done + v)));

            done += rebar.Total;

            // 4. Create Schedule Tables
            if (options.CreateTables)
            {
                CreateTables(views, spec, settings, progress, ref done);
            }

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

    private CreatedBeamViews CreateViews(
        BeamViewOptions options,
        BeamAnnotationSettings settings,
        IProgress<int>? progress,
        ref int done)
    {
        ViewSection? detailView = null;
        if (options.CreateElevationView)
        {
            using var t = new Transaction(_document, "Create Detail View");
            t.Start();
            RebarFailureHandling.Apply(t);
            detailView = DetailViewCreator.Create(_document, _stack, settings);
            t.Commit();
            progress?.Report(++done);
        }

        IReadOnlyList<ViewSection> sectionViews = Array.Empty<ViewSection>();
        if (options.CreateSectionViews)
        {
            using var t = new Transaction(_document, "Create Section Views");
            t.Start();
            RebarFailureHandling.Apply(t);
            sectionViews = SectionViewCreator.Create(_document, _stack, settings);
            t.Commit();
        }
        done += sectionViews.Count;
        progress?.Report(done);

        return new CreatedBeamViews { DetailView = detailView, SectionViews = sectionViews };
    }

    private void CreateDimensions(CreatedBeamViews views, BeamAnnotationSettings settings, IProgress<int>? progress, ref int done)
    {
        if (views.DetailView is not null)
        {
            using var t = new Transaction(_document, "Create Elevation Dimensions");
            t.Start();
            RebarFailureHandling.Apply(t);
            done += DimensionCreator.CreateOnElevation(_document, views.DetailView, _stack, settings);
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
                int cutCount = SectionViewCreator.ComputeCutStations(span, settings.SectionsPerSpan).Count;
                for (int cut = 0; cut < cutCount && viewIdx < views.SectionViews.Count; cut++)
                {
                    done += DimensionCreator.CreateOnSection(
                        _document, views.SectionViews[viewIdx], _stack.Faces[spanIdx], span, settings);
                    viewIdx++;
                }
            }
            t.Commit();
        }
        progress?.Report(done);
    }

    private void CreateTables(
        CreatedBeamViews views,
        BeamRebarSpec spec,
        BeamAnnotationSettings settings,
        IProgress<int>? progress,
        ref int done)
    {
        using var t = new Transaction(_document, "Create Beam Tables");
        t.Start();
        RebarFailureHandling.Apply(t);

        int viewIndex = 0;
        for (int spanIndex = 0; spanIndex < _stack.Spans.Count; spanIndex++)
        {
            var span = _stack.Spans[spanIndex];
            int cutCount = SectionViewCreator.ComputeCutStations(span, settings.SectionsPerSpan).Count;
            for (int cutIndex = 0; cutIndex < cutCount && viewIndex < views.SectionViews.Count; cutIndex++)
            {
                RebarTableTagCreator.Create(
                    _document, views.SectionViews[viewIndex], span, spanIndex, cutIndex, spec, settings);
                viewIndex++;
                done++;
            }
        }

        t.Commit();
        progress?.Report(done);
    }
}
