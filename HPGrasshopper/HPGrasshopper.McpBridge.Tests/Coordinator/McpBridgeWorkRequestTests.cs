namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.Threading;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class McpBridgeWorkRequestTests
{
    [Fact]
    public void ValidateActiveDocument_RequireTargetIdentity_WhenAllTargetsNull_Throws()
    {
        var work = new McpBridgeWorkRequest(
            "TestWrite",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: null,
            TargetDocPath: null,
            TargetDefId: null,
            RequireTargetIdentity: true);

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Write operation 'TestWrite' rejected", ex.Message);
        Assert.Contains("Target document or definition identity must be specified", ex.Message);
    }

    [Fact]
    public void ValidateActiveDocument_RequireTargetIdentity_WhenFalseAndNoTargets_DoesNotThrow()
    {
        var work = new McpBridgeWorkRequest(
            "TestRead",
            (_, _) => null,
            CancellationToken.None,
            RequireTargetIdentity: false);

        work.ValidateActiveDocument(null);
    }

    [Fact]
    public void ValidateActiveDocument_TargetDefId_WhenNoDefinitionOpen_Throws()
    {
        var expectedGuid = Guid.NewGuid();
        var work = new McpBridgeWorkRequest(
            "TestGhDef",
            (_, _) => null,
            CancellationToken.None,
            TargetDefId: expectedGuid);

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Grasshopper definition identity mismatch", ex.Message);
    }

    [Fact]
    public void Coordinator_UpdateActiveSnapshot_StoresAndUpdatesAllIdentityFields()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        try
        {
            var testGuid = Guid.NewGuid();

            coordinator.UpdateActiveSnapshot(
                "SampleModel.rvt",
                @"C:\Projects\SampleModel.rvt",
                testGuid,
                "ParametricTruss",
                @"C:\Scripts\Truss.gh");

            Assert.Equal("SampleModel.rvt", coordinator.ActiveDocumentTitle);
            Assert.Equal(@"C:\Projects\SampleModel.rvt", coordinator.ActiveDocumentPath);
            Assert.Equal(testGuid, coordinator.ActiveDefinitionId);
            Assert.Equal("ParametricTruss", coordinator.ActiveDefinitionName);
            Assert.Equal(@"C:\Scripts\Truss.gh", coordinator.ActiveDefinitionPath);
        }
        finally
        {
            coordinator.UpdateActiveSnapshot(null, null, null, null, null);
        }
    }

    [Fact]
    public void CanvasReader_Read_NullDocument_ReturnsSafeStructure()
    {
        var reader = new CanvasReader();
        var result = reader.Read(null);

        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.Empty(result.Components);
        Assert.Empty(result.Wires);
    }
}
