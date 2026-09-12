using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar;
using HPRebar.ColumnRebar.Models;
using HPRebar.Core.ColumnRebar.Models;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace HPRebar.Tests;

/// <summary>
///     Reads the sample two-storey stack and checks the numbers that come back, in millimetres.
/// </summary>
public sealed class ColumnStackReaderTests : RevitApiTest
{
    private const double Tolerance = 0.5;

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
    public async Task TheStackIsReadAsTwoRectangularSegments()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var stack = ReadStack();

        await Assert.That(stack.Style).IsEqualTo(ColumnSectionStyle.Rectangle);
        await Assert.That(stack.Sections.Count).IsEqualTo(2);
        await Assert.That(stack.Faces.Count).IsEqualTo(2);
    }

    [Test]
    public async Task TheLowerSegmentReportsItsPlanSizeInMillimetres()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var lower = ReadStack().Sections[0];

        await Assert.That(lower.B).IsEqualTo(400d).Within(Tolerance);
        await Assert.That(lower.H).IsEqualTo(600d).Within(Tolerance);
        await Assert.That(lower.Hc).IsEqualTo(3000d).Within(Tolerance);
    }

    [Test]
    public async Task TheUpperSegmentIsSmallerAndSetInFromTheWestFaceBelow()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var upper = ReadStack().Sections[1];

        await Assert.That(upper.B).IsEqualTo(300d).Within(Tolerance);
        await Assert.That(upper.H).IsEqualTo(500d).Within(Tolerance);

        // The upper column is offset, so its west face does not sit on the stack's west datum.
        await Assert.That(upper.WestPosition).IsEqualTo(50d).Within(Tolerance);
    }

    [Test]
    public async Task TheBeamAtTheLowerHeadGivesItsDepthAndSoffitDrop()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var lower = ReadStack().Sections[0];

        await Assert.That(lower.Hb).IsEqualTo(500d).Within(Tolerance);
        await Assert.That(lower.Zb).IsEqualTo(500d).Within(Tolerance);
    }

    [Test]
    public async Task PositionsAreMeasuredFromTheFoundationTopSoTheLowerBaseIsZero()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var sections = ReadStack().Sections;

        await Assert.That(sections[0].BottomPosition).IsEqualTo(0d).Within(Tolerance);
        await Assert.That(sections[0].TopPosition).IsEqualTo(3000d).Within(Tolerance);
        await Assert.That(sections[1].BottomPosition).IsEqualTo(3000d).Within(Tolerance);
    }

    [Test]
    public async Task AColumnWithNoBeamAboveItFallsBackToItsLargestPlanDimension()
    {
        Skip.Unless(ColumnStackFixture.Exists, ColumnStackFixture.MissingReason);

        var upper = ReadStack().Sections[1];

        // No beam frames into the top of the upper column, so the bend depth is the larger plan side.
        await Assert.That(upper.Hb).IsEqualTo(0d).Within(Tolerance);
        await Assert.That(upper.BendDepth).IsEqualTo(500d).Within(Tolerance);
    }

    private ColumnStack ReadStack()
    {
        var columns = ColumnStackFixture.ColumnsMarked(_document!, "C1-LOWER", "C1-UPPER");

        return ColumnStackReader.Read(_document!, columns);
    }
}
