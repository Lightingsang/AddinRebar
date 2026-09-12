using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using HPRebar.BeamRebar.Model;
using HPRebar.BeamRebar.Service;
using HPRebar.BeamRebar.ViewModel;
using HPRebar.BeamRebar.View;
using HPRebar.Core.BeamRebar.Models;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace HPRebar.BeamRebar;

/// <summary>
/// ExternalCommand entry point for continuous beam rebar generation.
/// Picks continuous beam framing elements, validates collinearity and continuity, and opens the MVVM configuration window.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class BeamRebarCommand : ExternalCommand
{
    // The window is modeless, so it outlives this command. Holding it here keeps it off the collector and
    // makes a second click focus the open window instead of starting a rival session on the same beams.
    private static BeamRebarView? _window;

    public override void Execute()
    {
        var uiDocument = Application.ActiveUIDocument;
        var document = uiDocument.Document;

        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        IList<Reference> references;
        try
        {
            references = uiDocument.Selection.PickObjects(
                ObjectType.Element,
                new BeamRebarSelectionFilter(),
                "Select continuous structural beam spans in order from left to right");
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return; // User cancelled
        }

        if (references.Count == 0) return;

        try
        {
            var selectedBeams = references
                .Select(r => document.GetElement(r))
                .Where(e => e is not null)
                .ToList();

            var validation = BeamStackValidator.Validate(document, selectedBeams);
            if (!validation.IsOk)
            {
                Log.Warning("Beam Rebar validation failed: {Message}", validation.Message);
                RevitDialogs.Error("Beam Rebar", validation.Message);
                return;
            }

            var stack = BeamStackReader.Read(document, selectedBeams);
            Log.Information("Beam Rebar loaded continuous stack with {Spans} span(s) and {Supports} support(s).",
                stack.Spans.Count, stack.Supports.Count);

            var spec = BeamDefaultSpecBuilder.Build(document, stack);
            var shapes = RebarShapeResolver.Load(document);

            var ready = RebarCreationService.CanCreate(shapes, stack, spec);
            if (!ready.IsOk)
            {
                RevitDialogs.Error("Beam Rebar", ready.Message);
                return;
            }

            var annotation = BeamAnnotationSettings.Load(document, stack.ContinuousStack);
            var orchestrator = new BeamRebarOrchestrator(document, stack, shapes, annotation);
            var handler = new BeamRebarExternalEventHandler(orchestrator);

            var barTypes = RebarTypeCatalog.LoadBarTypes(document);
            var session = new BeamRebarSession(stack, stack.Faces, spec, barTypes);
            var viewModel = new BeamRebarViewModel(session, new LocalizationService(), handler);
            var view = new BeamRebarView(viewModel);

            new WindowInteropHelper(view).Owner = Application.MainWindowHandle;
            ThemeSwitcher.ApplyFromRevit(view);

            view.Closed += (_, _) =>
            {
                handler.Dispose();
                _window = null;
            };

            _window = view;

            // Modeless: this returns straight away and Revit stays usable. Everything the window does to the
            // document from here goes back through the external event handler.
            view.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Beam Rebar command failed during execution.");
            RevitDialogs.Error("Beam Rebar", $"An unexpected error occurred:\n{ex.Message}");
        }
    }
}
