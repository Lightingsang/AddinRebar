using HPRebar.Core.ColumnRebar.Models;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar;
using HPRebar.ColumnRebar.Models;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace HPRebar.Tests;

/// <summary>
///     Runs the whole action against the sample model and rolls it back, so the fixture on disk is never
///     changed.
/// </summary>
public sealed class ColumnRebarOrchestratorTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void OpenFixture()
    {
        if (!ColumnStackFixture.Exists) return;

        _document = Application.OpenDocumentFile(ColumnStackFixture.Path);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseFixture()
    {
        _document?.Close(false);
        _document = null;
    }

    [Test]
    public async Task ARunProducesBothElevationsAndOneSectionPerSegment()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (orchestrator, specs, stack) = Prepare();

        using var undo = new TransactionGroup(_document!, "Test run");
        undo.Start();

        var result = orchestrator.Run(specs, ViewNaming.Default);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Views.DetailX).IsNotNull();
        await Assert.That(result.Views.DetailY).IsNotNull();
        await Assert.That(result.Views.Sections.Count).IsEqualTo(stack.Sections.Count);

        undo.RollBack();
    }

    [Test]
    public async Task TheElevationViewsAreNamedFromTheSettings()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (orchestrator, specs, _) = Prepare();

        using var undo = new TransactionGroup(_document!, "Test run");
        undo.Start();

        var result = orchestrator.Run(specs, ViewNaming.Default);

        // The tool falls back to a suffixed name if one is taken, so both spellings are acceptable.
        await Assert.That(result.Views.DetailX!.Name.StartsWith("DetailX")).IsTrue();
        await Assert.That(result.Views.DetailY!.Name.StartsWith("DetailY")).IsTrue();

        undo.RollBack();
    }

    [Test]
    public async Task TheReinforcementIsBuiltAlongsideTheViews()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (orchestrator, specs, stack) = Prepare();

        using var undo = new TransactionGroup(_document!, "Test run");
        undo.Start();

        var result = orchestrator.Run(specs, ViewNaming.Default);

        await Assert.That(result.Rebar.MainBars.Count).IsEqualTo(4 * stack.Sections.Count);
        await Assert.That(result.Rebar.Stirrups.Count).IsEqualTo(stack.Sections.Count);

        undo.RollBack();
    }

    [Test]
    public async Task RollingBackTheRunLeavesNoViewsBehind()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (orchestrator, specs, _) = Prepare();
        var before = ViewCount();

        using (var undo = new TransactionGroup(_document!, "Test run"))
        {
            undo.Start();
            orchestrator.Run(specs, ViewNaming.Default);
            undo.RollBack();
        }

        await Assert.That(ViewCount()).IsEqualTo(before);
    }

    [Test]
    public async Task ThePlannedCountCoversViewsAsWellAsBars()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (orchestrator, specs, stack) = Prepare();

        var planned = orchestrator.PlannedCount(specs);
        var bars = RebarCreationService.PlannedCount(stack, specs);

        await Assert.That(planned > bars).IsTrue();
    }

    private (ColumnRebarOrchestrator Orchestrator, System.Collections.Generic.IReadOnlyList<ColumnRebarSpec> Specs, ColumnStack Stack) Prepare()
    {
        var columns = ColumnStackFixture.ColumnsMarked(_document!, "C1-LOWER", "C1-UPPER");
        var stack = ColumnStackReader.Read(_document!, columns);
        var specs = DefaultRebarSpecBuilder.Build(_document!, stack);
        var shapes = RebarShapeResolver.Load(_document!);
        var settings = AnnotationSettings.Load(_document!, stack);

        return (new ColumnRebarOrchestrator(_document!, stack, shapes, settings), specs, stack);
    }

    private int ViewCount() =>
        new FilteredElementCollector(_document!)
            .OfClass(typeof(ViewSection))
            .WhereElementIsNotElementType()
            .GetElementCount();
}
