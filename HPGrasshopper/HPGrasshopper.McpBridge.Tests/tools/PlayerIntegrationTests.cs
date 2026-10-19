namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class PlayerIntegrationTests
{
    [Fact]
    public void CollectRuntimeMessages_ComponentPhaseFailed_CapturesPhaseFailure()
    {
        var comp = TestProxyFactory.CreateComponent("SolverFailureNode", "FAIL");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.Phase = GH_SolutionPhase.Failed;

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp });

        Assert.NotEmpty(errors);
        Assert.Single(errors);
        Assert.Equal("[FAIL] Component execution failed (SolutionPhase.Failed).", errors[0]);
    }

    [Fact]
    public void CollectRuntimeMessages_ExplicitRuntimeError_CapturesExplicitMessage()
    {
        var comp = TestProxyFactory.CreateComponent("MathDivide", "DIV");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.ErrorMessages.Add("Division by zero in formula evaluation.");

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp });

        Assert.Single(errors);
        Assert.Equal("[DIV] Division by zero in formula evaluation.", errors[0]);
    }

    [Fact]
    public void CollectRuntimeMessages_ExplicitErrorWithFailedPhase_PreservesExplicitErrorOnly()
    {
        var comp = TestProxyFactory.CreateComponent("SolverNode", "SOLVE");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.Phase = GH_SolutionPhase.Failed;
        proxy.ErrorMessages.Add("Domain singularity encountered.");

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp });

        Assert.Single(errors);
        Assert.Equal("[SOLVE] Domain singularity encountered.", errors[0]);
    }

    [Fact]
    public void CollectRuntimeDiagnostics_WarningMessages_CapturesFormattedWarnings()
    {
        var comp = TestProxyFactory.CreateComponent("Sampler", "SMP");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.WarningMessages.Add("Input list lengths are uneven; shortest list used.");

        var (errors, warnings) = PlayerRunner.CollectRuntimeDiagnostics(new[] { comp });

        Assert.Empty(errors);
        Assert.Single(warnings);
        Assert.Equal("[SMP] Input list lengths are uneven; shortest list used.", warnings[0]);
    }

    [Fact]
    public void CollectRuntimeMessages_BlankPhaseWithoutErrors_ReturnsEmptyList()
    {
        var comp = TestProxyFactory.CreateComponent("AddNode", "+");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.Phase = GH_SolutionPhase.Blank;

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp });

        Assert.Empty(errors);
    }

    [Fact]
    public void CollectRuntimeMessages_MultipleComponents_CapturesOnlyFailedNodes()
    {
        var okComp = TestProxyFactory.CreateComponent("OkNode", "OK");
        var failComp = TestProxyFactory.CreateComponent("FailingNode", "ERR");
        var failProxy = TestProxyFactory.GetProxy<TestComponentProxy>(failComp);
        failProxy.Phase = GH_SolutionPhase.Failed;

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { okComp, failComp });

        Assert.Single(errors);
        Assert.Equal("[ERR] Component execution failed (SolutionPhase.Failed).", errors[0]);
    }
}
