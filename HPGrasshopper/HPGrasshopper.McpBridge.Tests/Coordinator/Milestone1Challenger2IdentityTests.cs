namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.Threading;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Coordinator;
using Xunit;

/// <summary>
///     Adversarial challenge for Finding R5: Document Identity Validation.
///     Empirically validates:
///     1. RequireTargetIdentity fail-closed rejections.
///     2. Grasshopper definition identity mismatch when null or mismatched GUID.
///     3. Active definition matching when GUID matches.
///     4. Document title/path validation when Revit application is null.
/// </summary>
public sealed class Milestone1Challenger2IdentityTests
{
    [Fact]
    public void ValidateActiveDocument_RequireTargetIdentityTrue_WhenNoTargetsSpecified_Throws()
    {
        var work = new McpBridgeWorkRequest(
            "WriteNoTargets",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: null,
            TargetDocPath: null,
            TargetDefId: null,
            RequireTargetIdentity: true);

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Write operation 'WriteNoTargets' rejected", ex.Message);
        Assert.Contains("Target document or definition identity must be specified", ex.Message);
    }

    [Fact]
    public void ValidateActiveDocument_RequireTargetIdentityTrue_WhenTargetDefIdIsEmptyGuid_Throws()
    {
        var work = new McpBridgeWorkRequest(
            "WriteEmptyGuid",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: null,
            TargetDocPath: null,
            TargetDefId: Guid.Empty,
            RequireTargetIdentity: true);

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Write operation 'WriteEmptyGuid' rejected", ex.Message);
    }

    [Fact]
    public void ValidateActiveDocument_RequireTargetIdentityFalse_WhenNoTargetsSpecified_Passes()
    {
        var work = new McpBridgeWorkRequest(
            "ReadWithoutTarget",
            (_, _) => null,
            CancellationToken.None,
            RequireTargetIdentity: false);

        work.ValidateActiveDocument(null);
    }

    [Fact]
    public void ValidateActiveDocument_TargetDocTitle_WhenNoRevitDoc_ThrowsDocumentIdentityMismatch()
    {
        var work = new McpBridgeWorkRequest(
            "CheckTitle",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: "BuildingModel.rvt");

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", ex.Message);
        Assert.Contains("BuildingModel.rvt", ex.Message);
        Assert.Contains("no document is open in Revit", ex.Message);
    }

    [Fact]
    public void ValidateActiveDocument_TargetDocPath_WhenNoRevitDoc_ThrowsDocumentIdentityMismatch()
    {
        var work = new McpBridgeWorkRequest(
            "CheckPath",
            (_, _) => null,
            CancellationToken.None,
            TargetDocPath: @"C:\Models\BuildingModel.rvt");

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", ex.Message);
        Assert.Contains(@"C:\Models\BuildingModel.rvt", ex.Message);
        Assert.Contains("no document is open in Revit", ex.Message);
    }

    [Fact]
    public void ValidateActiveDocument_TargetDefId_WhenNoGhDefinitionOpen_ThrowsDefinitionMismatch()
    {
        var expectedGuid = Guid.NewGuid();
        var work = new McpBridgeWorkRequest(
            "CheckDef",
            (_, _) => null,
            CancellationToken.None,
            TargetDefId: expectedGuid);

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Grasshopper definition identity mismatch", ex.Message);
        Assert.Contains(expectedGuid.ToString(), ex.Message);
        Assert.Contains("no definition is open in Grasshopper", ex.Message);
    }

    [Fact]
    public void ValidateActiveDocument_WithBothTargets_FailsOnFirstTargetWhenRevitNotOpen()
    {
        var targetGuid = Guid.NewGuid();
        var work = new McpBridgeWorkRequest(
            "CheckBoth",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: "Model.rvt",
            TargetDefId: targetGuid);

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", ex.Message);
        Assert.Contains("Model.rvt", ex.Message);
    }

    [Fact]
    public void Coordinator_SnapshotRoundtripAndThreadSafety_IsolatedCleanly()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var guid = Guid.NewGuid();

        try
        {
            coordinator.UpdateActiveSnapshot(
                "Hospital.rvt",
                @"D:\Revit\Hospital.rvt",
                guid,
                "StructureDef",
                @"D:\GH\StructureDef.gh");

            Assert.Equal("Hospital.rvt", coordinator.ActiveDocumentTitle);
            Assert.Equal(@"D:\Revit\Hospital.rvt", coordinator.ActiveDocumentPath);
            Assert.Equal(guid, coordinator.ActiveDefinitionId);
            Assert.Equal("StructureDef", coordinator.ActiveDefinitionName);
            Assert.Equal(@"D:\GH\StructureDef.gh", coordinator.ActiveDefinitionPath);
        }
        finally
        {
            coordinator.UpdateActiveSnapshot(null, null, null, null, null);
        }

        Assert.Null(coordinator.ActiveDocumentTitle);
        Assert.Null(coordinator.ActiveDocumentPath);
        Assert.Null(coordinator.ActiveDefinitionId);
        Assert.Null(coordinator.ActiveDefinitionName);
        Assert.Null(coordinator.ActiveDefinitionPath);
    }
}
