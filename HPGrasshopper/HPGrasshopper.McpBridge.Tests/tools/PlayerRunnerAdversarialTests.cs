namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class PlayerRunnerAdversarialTests
{
    [Fact]
    public void CollectRuntimeMessages_SingleComponentPhaseFailed_CapturesFailure()
    {
        var comp = TestProxyFactory.CreateComponent("SolverNode", "SOLVE");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.Phase = GH_SolutionPhase.Failed;

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp });

        Assert.Single(errors);
        Assert.Equal("[SOLVE] Component execution failed (SolutionPhase.Failed).", errors[0]);
    }

    [Fact]
    public void CollectRuntimeMessages_SingleComponentExplicitError_TakesPrecedence()
    {
        var comp = TestProxyFactory.CreateComponent("CalcNode", "CALC");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.Phase = GH_SolutionPhase.Failed;
        proxy.ErrorMessages.Add("Domain singularity.");

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp });

        Assert.Single(errors);
        Assert.Equal("[CALC] Domain singularity.", errors[0]);
    }

    [Fact]
    public void CollectRuntimeMessages_BlankPhase_ReturnsEmpty()
    {
        var comp = TestProxyFactory.CreateComponent("CleanNode", "CLEAN");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.Phase = GH_SolutionPhase.Blank;

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp });

        Assert.Empty(errors);
    }

    [Fact]
    public void CollectRuntimeDiagnostics_WarningOnlyOnFirst_DoesNotSuppressFailedPhaseOnSecond()
    {
        var comp1 = TestProxyFactory.CreateComponent("WarnNode", "WARN");
        var proxy1 = TestProxyFactory.GetProxy<TestComponentProxy>(comp1);
        proxy1.WarningMessages.Add("Uneven list length.");

        var comp2 = TestProxyFactory.CreateComponent("FailNode", "FAIL");
        var proxy2 = TestProxyFactory.GetProxy<TestComponentProxy>(comp2);
        proxy2.Phase = GH_SolutionPhase.Failed;

        var (errors, warnings) = PlayerRunner.CollectRuntimeDiagnostics(new[] { comp1, comp2 });

        Assert.Single(warnings);
        Assert.Single(errors);
        Assert.Equal("[WARN] Uneven list length.", warnings[0]);
        Assert.Equal("[FAIL] Component execution failed (SolutionPhase.Failed).", errors[0]);
    }

    [Fact]
    public void CollectRuntimeMessages_MultipleFailedComponents_CapturesAllFailedComponents()
    {
        var comp1 = TestProxyFactory.CreateComponent("SolverA", "A");
        var proxy1 = TestProxyFactory.GetProxy<TestComponentProxy>(comp1);
        proxy1.Phase = GH_SolutionPhase.Failed;

        var comp2 = TestProxyFactory.CreateComponent("SolverB", "B");
        var proxy2 = TestProxyFactory.GetProxy<TestComponentProxy>(comp2);
        proxy2.Phase = GH_SolutionPhase.Failed;

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp1, comp2 });

        // Adversarial challenge: when two independent components fail, both must be reported.
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("[A]") && e.Contains("SolutionPhase.Failed"));
        Assert.Contains(errors, e => e.Contains("[B]") && e.Contains("SolutionPhase.Failed"));
    }

    [Fact]
    public void CollectRuntimeMessages_ExplicitErrorOnFirstComponent_DoesNotSuppressFailedPhaseOnSecond()
    {
        var comp1 = TestProxyFactory.CreateComponent("CalcNode", "CALC");
        var proxy1 = TestProxyFactory.GetProxy<TestComponentProxy>(comp1);
        proxy1.ErrorMessages.Add("Calculated value out of range.");

        var comp2 = TestProxyFactory.CreateComponent("SolverNode", "SOLVER");
        var proxy2 = TestProxyFactory.GetProxy<TestComponentProxy>(comp2);
        proxy2.Phase = GH_SolutionPhase.Failed;

        var errors = PlayerRunner.CollectRuntimeMessages(new[] { comp1, comp2 });

        // Adversarial challenge: an explicit error on node 1 must NOT mask the failure of node 2.
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("[CALC] Calculated value out of range."));
        Assert.Contains(errors, e => e.Contains("[SOLVER]") && e.Contains("SolutionPhase.Failed"));
    }
}
