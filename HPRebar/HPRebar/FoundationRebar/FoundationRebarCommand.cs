using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using HPRebar.FoundationRebar.Service;
using HPRebar.FoundationRebar.View;
using HPRebar.FoundationRebar.ViewModel;
using HPRebar.Shared.Revit;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace HPRebar.FoundationRebar;

/// <summary>
///     Entry point for the Foundation Rebar tool. Picks a floor slab, checks it is one the tool can
///     reinforce, reads it into the session the window edits, and opens that window.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class FoundationRebarCommand : ExternalCommand
{
    // The window is modeless, so it outlives this command. Holding it here keeps it off the collector and
    // makes a second click focus the open window instead of starting a rival session on the same slab.
    private static FoundationRebarView? _window;

    public override void Execute()
    {
        var uiDocument = Application.ActiveUIDocument;
        var document = uiDocument.Document;

        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        var element = PreselectionPicker.PickElement(
            uiDocument,
            new FoundationRebarSelectionFilter(),
            "Select a foundation slab (Floor) to reinforce");

        if (element is null) return;

        var validation = FoundationRebarValidator.Validate(element);

        if (!validation.IsValid)
        {
            Log.Warning("Foundation Rebar validation failed: {Message}", validation.Message);
            RevitDialogs.Error("Foundation Rebar", validation.Message);
            return;
        }

        if (element is not Floor floor)
        {
            RevitDialogs.Error("Foundation Rebar", "The selected element is not a valid Floor.");
            return;
        }

        try
        {
            var session = FoundationSessionBuilder.Build(document, floor);

            if (session is null)
            {
                RevitDialogs.Error(
                    "Foundation Rebar",
                    "No RebarBarType found in active project. Please load rebar families first.");

                return;
            }

            var handler = new FoundationRebarExternalEventHandler(new FoundationRebarOrchestrator());
            var view = new FoundationRebarView(new FoundationRebarViewModel(session, handler));

            new WindowInteropHelper(view).Owner = Application.MainWindowHandle;

            view.Closed += (_, _) =>
            {
                handler.Dispose();
                _window = null;
            };

            _window = view;

            // Modeless: this returns straight away and Revit stays usable. The transaction group that writes
            // the mat is opened by the external event handler, not here.
            view.Show();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Foundation Rebar failed while reading the selected slab");

            RevitDialogs.Error(
                "Foundation Rebar",
                $"Could not read the selected slab.{Environment.NewLine}{exception.Message}");
        }
    }
}
