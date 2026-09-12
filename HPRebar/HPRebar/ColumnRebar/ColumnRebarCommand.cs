using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using System.Windows.Interop;
using Autodesk.Revit.UI.Selection;
using HPRebar.ColumnRebar.Model;
using HPRebar.ColumnRebar.Service;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.View;
using Nice3point.Revit.Toolkit.External;
using Serilog;

namespace HPRebar.ColumnRebar;

/// <summary>
///     Entry point for the Column Rebar tool. Picks a run of stacked columns, checks it, and reads it into
///     the numeric model the rest of the tool works on.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class ColumnRebarCommand : ExternalCommand
{
    // The window is modeless, so it outlives this command. Holding it here keeps it off the collector and
    // makes a second click focus the open window instead of starting a rival session on the same columns.
    private static ColumnRebarView? _window;

    public override void Execute()
    {
        // Application is the inherited UIApplication of ExternalCommand, not HPRebar.Application.
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
                new ColumnRebarSelectionFilter(),
                "Select the stacked structural columns to reinforce, bottom to top");
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // User pressed Escape.
            return;
        }

        if (references.Count == 0) return;

        try
        {
            var columns = references
                .Select(reference => document.GetElement(reference))
                .OrderBy(column => ColumnStackReader.BottomFace(column).Origin.Z)
                .ToList();

            var validation = ColumnStackValidator.Validate(document, columns);

            if (!validation.IsOk)
            {
                Log.Warning("Column Rebar validation failed with code {Code}: {Message}", validation.Code, validation.Message);
                RevitDialogs.Error("Column Rebar", validation.Message);
                return;
            }

            var stack = ColumnStackReader.Read(document, columns);

            Log.Information("Column Rebar read a {Style} stack of {Count} segment(s)", stack.Style, stack.Sections.Count);

            var specs = DefaultRebarSpecBuilder.Build(document, stack);

            if (specs.Count == 0)
            {
                RevitDialogs.Error("Column Rebar", ValidationMessages.For(23));
                return;
            }

            var shapes = RebarShapeResolver.Load(document);
            var ready = RebarCreationService.CanCreate(shapes, stack, specs);

            if (!ready.IsOk)
            {
                RevitDialogs.Error("Column Rebar", ready.Message);
                return;
            }

            var annotation = AnnotationSettings.Load(document, stack);
            var orchestrator = new ColumnRebarOrchestrator(document, stack, shapes, annotation);
            var handler = new ColumnRebarExternalEventHandler(orchestrator);
            var barTypes = RebarTypeCatalog.BarTypes(document);
            var session = new ColumnRebarSession(stack, specs, barTypes);
            var view = new ColumnRebarView(new ColumnRebarViewModel(session, new LocalizationService(), handler));

            new WindowInteropHelper(view).Owner = Application.MainWindowHandle;

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
        catch (Exception exception)
        {
            Log.Error(exception, "Column Rebar failed while reading the selected columns");
            RevitDialogs.Error("Column Rebar", $"Could not read the selected columns.{Environment.NewLine}{exception.Message}");
        }
    }
}
