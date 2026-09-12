using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace HPRebar.Tests;

/// <summary>
///     Checks the validator reports the first rule a selection breaks, not the last one tested.
/// </summary>
public sealed class ColumnStackValidatorTests : RevitApiTest
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
    public async Task AProperlyStackedPairPasses()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var columns = ColumnStackFixture.ColumnsMarked(_document!, "C1-LOWER", "C1-UPPER");

        var result = ColumnStackValidator.Validate(_document!, columns);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Code).IsEqualTo(0);
    }

    [Test]
    public async Task ASlantedColumnIsReportedAsSuchAndNotAsSomeLaterRule()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var columns = ColumnStackFixture.ColumnsMarked(_document!, "C-SLANTED");

        var result = ColumnStackValidator.Validate(_document!, columns);

        // A slanted column also breaks later rules; the first failure is the one worth showing.
        await Assert.That(result.Code).IsEqualTo(2);
    }

    [Test]
    public async Task TwoColumnsThatDoNotTouchAreReportedAsDiscontinuous()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var columns = ColumnStackFixture.ColumnsMarked(_document!, "C1-LOWER", "C2-DETACHED");

        var result = ColumnStackValidator.Validate(_document!, columns);

        await Assert.That(result.Code).IsEqualTo(4);
    }

    [Test]
    public async Task AColumnGrowingWiderGoingUpIsRejected()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var columns = ColumnStackFixture.ColumnsMarked(_document!, "C3-LOWER", "C3-WIDER");

        var result = ColumnStackValidator.Validate(_document!, columns);

        await Assert.That(result.Code).IsEqualTo(7);
    }

    [Test]
    public async Task AnEmptySelectionIsRejectedRatherThanCrashing()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var result = ColumnStackValidator.Validate(_document!, new Element[0]);

        await Assert.That(result.IsOk).IsFalse();
    }
}
