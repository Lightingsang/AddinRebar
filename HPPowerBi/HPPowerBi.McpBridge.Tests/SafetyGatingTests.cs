using HPPowerBi.McpBridge.Safety;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class SafetyGatingTests
{
    [Fact]
    public void SafetyGuard_InitialState_AllGatedOff()
    {
        var guard = new PbiSafetyGuard();

        Assert.False(guard.IsExecutionEnabled);
        Assert.False(guard.IsMutationEnabled);

        Assert.Throws<BridgeRequestException>(() => guard.EnsureExecutionAllowed());
        Assert.Throws<BridgeRequestException>(() => guard.EnsureMutationAllowed());
    }

    [Fact]
    public void SafetyGuard_EnableExecutionOnly_ExecutionPassesMutationFails()
    {
        var guard = new PbiSafetyGuard
        {
            IsExecutionEnabled = true
        };

        // Execution allowed
        guard.EnsureExecutionAllowed();

        // Mutation still gated off
        Assert.False(guard.IsMutationEnabled);
        Assert.Throws<BridgeRequestException>(() => guard.EnsureMutationAllowed());
    }

    [Fact]
    public void SafetyGuard_DisableExecution_TurnsOffMutationAutomatically()
    {
        var guard = new PbiSafetyGuard
        {
            IsExecutionEnabled = true,
            IsMutationEnabled = true
        };

        Assert.True(guard.IsMutationEnabled);

        // Turn off execution
        guard.IsExecutionEnabled = false;

        // Mutation must be automatically turned off
        Assert.False(guard.IsMutationEnabled);
        Assert.Throws<BridgeRequestException>(() => guard.EnsureExecutionAllowed());
        Assert.Throws<BridgeRequestException>(() => guard.EnsureMutationAllowed());
    }

    [Fact]
    public void ValidateDaxQuery_ValidQuery_Passes()
    {
        var valid = PbiSafetyGuard.ValidateDaxQuery("EVALUATE TOPN(10, Customers)", out var error);
        Assert.True(valid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateDaxQuery_XmlaBatchCommand_Rejected()
    {
        var valid = PbiSafetyGuard.ValidateDaxQuery("<Batch xmlns=\"...\"><Create>...</Create></Batch>", out var error);
        Assert.False(valid);
        Assert.NotNull(error);
        Assert.Contains("XMLA", error);
    }

    [Fact]
    public void ValidateDaxQuery_AdminKillSpid_Rejected()
    {
        var valid = PbiSafetyGuard.ValidateDaxQuery("KILL SPID 42", out var error);
        Assert.False(valid);
        Assert.NotNull(error);
        Assert.Contains("KILL", error);
    }

    [Fact]
    public void IsMutationScript_DetectsModelModifications()
    {
        Assert.True(PbiSafetyGuard.IsMutationScript("model.SaveChanges(); return true;"));
        Assert.True(PbiSafetyGuard.IsMutationScript("table.Measures.Add(new Measure());"));
        Assert.True(PbiSafetyGuard.IsMutationScript("model.Relationships.Remove(rel);"));
        Assert.False(PbiSafetyGuard.IsMutationScript("var count = model.Tables.Count; return count;"));
    }
}
