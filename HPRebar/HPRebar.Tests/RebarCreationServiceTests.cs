using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.ColumnRebar;
using HPRebar.ColumnRebar.Models;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace HPRebar.Tests;

/// <summary>
///     Creates reinforcement in the sample model and rolls it straight back, so the fixture on disk is
///     never touched.
/// </summary>
public sealed class RebarCreationServiceTests : RevitApiTest
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
    public async Task ADefaultRunPutsFourBarsAndTheTieGroupsIntoEachSegment()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (stack, specs, shapes) = Prepare();

        using var group = new TransactionGroup(_document!, "Test run");
        group.Start();

        var created = RebarCreationService.Create(_document!, stack, specs, shapes);

        // Four corner bars per segment is the default layout.
        await Assert.That(created.MainBars.Count).IsEqualTo(4 * stack.Sections.Count);

        // The default tie layout is one evenly spaced group per segment.
        await Assert.That(created.Stirrups.Count).IsEqualTo(stack.Sections.Count);

        // Cross-ties are off by default.
        await Assert.That(created.Ties.Count).IsEqualTo(0);

        group.RollBack();
    }

    [Test]
    public async Task ThePlannedCountMatchesWhatIsActuallyCreated()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (stack, specs, shapes) = Prepare();

        var planned = RebarCreationService.PlannedCount(stack, specs);

        using var group = new TransactionGroup(_document!, "Test run");
        group.Start();

        var created = RebarCreationService.Create(_document!, stack, specs, shapes);

        await Assert.That(created.Total).IsEqualTo(planned);

        group.RollBack();
    }

    [Test]
    public async Task RollingBackLeavesTheModelWithoutAnyOfTheNewBars()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (stack, specs, shapes) = Prepare();
        var before = RebarCount();

        using (var group = new TransactionGroup(_document!, "Test run"))
        {
            group.Start();
            RebarCreationService.Create(_document!, stack, specs, shapes);
            group.RollBack();
        }

        await Assert.That(RebarCount()).IsEqualTo(before);
    }

    [Test]
    public async Task TheShapeCheckPassesOnAModelWithTheTieFamiliesLoaded()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (stack, specs, shapes) = Prepare();

        var ready = RebarCreationService.CanCreate(shapes, stack, specs);

        await Assert.That(ready.IsOk).IsTrue();
    }

    [Test]
    public async Task EveryCreatedBarIsHostedByTheColumnItBelongsTo()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var (stack, specs, shapes) = Prepare();

        using var group = new TransactionGroup(_document!, "Test run");
        group.Start();

        var created = RebarCreationService.Create(_document!, stack, specs, shapes);
        var hostIds = stack.Faces.Select(face => face.Element.Id).ToHashSet();

        foreach (var bar in created.MainBars)
        {
            await Assert.That(hostIds.Contains(bar.GetHostId())).IsTrue();
        }

        group.RollBack();
    }

    private (ColumnStack Stack, System.Collections.Generic.IReadOnlyList<ColumnRebarSpec> Specs, RebarShapeResolver Shapes) Prepare()
    {
        var columns = ColumnStackFixture.ColumnsMarked(_document!, "C1-LOWER", "C1-UPPER");
        var stack = ColumnStackReader.Read(_document!, columns);
        var specs = DefaultRebarSpecBuilder.Build(_document!, stack);

        return (stack, specs, RebarShapeResolver.Load(_document!));
    }

    private int RebarCount() =>
        new FilteredElementCollector(_document!)
            .OfClass(typeof(Rebar))
            .WhereElementIsNotElementType()
            .GetElementCount();
}
