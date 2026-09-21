using System;
using System.Collections.Generic;
using HPRobot.McpBridge.Units;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RobotOM;
using Xunit;

namespace HPRobot.McpBridge.Tests;

/// <summary>
///     Empirical adversarial tests challenging RobotUnitsPolicy:
///     1. Enforcement of Metric units (m, kN, kN*m, MPa) prior to script body execution.
///     2. Reliable restoration of original units even when the script body throws an unhandled exception.
///     3. Graceful degradation when RobotOM unit calls fail during setup or restoration.
///     4. Null unit manager safety.
/// </summary>
public sealed class RobotUnitsPolicyChallengerTests
{
    [Fact]
    public void Units_ScriptUnits_Metadata_CompliesWithContract()
    {
        var units = RobotUnitsPolicy.Units;

        Assert.Equal("Metric", RobotUnitsPolicy.MetricSystemName);
        Assert.Equal("Metric", units.Label);
        Assert.Equal(1.0, units.MmPerUnit);
        Assert.NotNull(units.Note);
        Assert.Contains("length m", units.Note);
        Assert.Contains("force kN", units.Note);
        Assert.Contains("moment kN·m", units.Note);
        Assert.Contains("stress MPa", units.Note);
    }

    [Fact]
    public void Run_WithNullUnitMngr_ExecutesBodyDirectlyWithoutError()
    {
        var logs = new List<string>();
        bool bodyExecuted = false;

        var result = RobotUnitsPolicy.Run(null, logs, () =>
        {
            bodyExecuted = true;
        });

        Assert.True(bodyExecuted);
        Assert.Null(result);
        Assert.Empty(logs);
    }

    [Fact]
    public void Run_WithNullUnitMngr_PropagatesUnhandledExceptionFromScript()
    {
        var logs = new List<string>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            RobotUnitsPolicy.Run(null, logs, () => throw new InvalidOperationException("Script failure with null units"));
        });

        Assert.Equal("Script failure with null units", ex.Message);
        Assert.Empty(logs);
    }

    [Fact]
    public void Run_EnforcesMetricUnits_AndRestoresOriginalImperialUnits_OnSuccessfulScript()
    {
        var unitMngr = Substitute.For<IRobotUnitMngr>();
        var logs = new List<string>();

        // Setup initial imperial state
        unitMngr.UseMetricAsDefault.Returns(false);

        var uDim = Substitute.For<RobotUnitData>();
        uDim.Name.Returns("ft");
        unitMngr.Get(IRobotUnitType.I_UT_STRUCTURE_DIMENSION).Returns(uDim);

        var uForce = Substitute.For<RobotUnitData>();
        uForce.Name.Returns("kip");
        unitMngr.Get(IRobotUnitType.I_UT_FORCE).Returns(uForce);

        var uMoment = Substitute.For<RobotUnitData>();
        uMoment.Name.Returns("kip*ft");
        unitMngr.Get(IRobotUnitType.I_UT_MOMENT).Returns(uMoment);

        var uStress = Substitute.For<RobotUnitData>();
        uStress.Name.Returns("ksi");
        unitMngr.Get(IRobotUnitType.I_UT_STRESS).Returns(uStress);

        bool scriptExecuted = false;
        string? capturedDimInside = null;
        string? capturedForceInside = null;
        string? capturedMomentInside = null;
        string? capturedStressInside = null;

        RobotUnitsPolicy.Run(unitMngr, logs, () =>
        {
            scriptExecuted = true;
            capturedDimInside = uDim.Name;
            capturedForceInside = uForce.Name;
            capturedMomentInside = uMoment.Name;
            capturedStressInside = uStress.Name;
        });

        Assert.True(scriptExecuted);

        // 1. Verify Metric units were enforced during script execution
        Assert.Equal("m", capturedDimInside);
        Assert.Equal("kN", capturedForceInside);
        Assert.Equal("kN*m", capturedMomentInside);
        Assert.Equal("MPa", capturedStressInside);

        // 2. Verify unit manager was refreshed before script ran
        unitMngr.Received().UseMetricAsDefault = true;
        unitMngr.Received().Refresh();

        // 3. Verify original units were restored after script finished
        Assert.Equal("ft", uDim.Name);
        Assert.Equal("kip", uForce.Name);
        Assert.Equal("kip*ft", uMoment.Name);
        Assert.Equal("ksi", uStress.Name);
        unitMngr.Received().UseMetricAsDefault = false;

        Assert.Empty(logs);
    }

    [Fact]
    public void Run_RestoresOriginalUnits_EvenWhenScriptThrowsUnhandledException()
    {
        var unitMngr = Substitute.For<IRobotUnitMngr>();
        var logs = new List<string>();

        // Setup initial state: Metric is false, non-standard units
        unitMngr.UseMetricAsDefault.Returns(false);

        var uDim = Substitute.For<RobotUnitData>();
        uDim.Name.Returns("in");
        unitMngr.Get(IRobotUnitType.I_UT_STRUCTURE_DIMENSION).Returns(uDim);

        var uForce = Substitute.For<RobotUnitData>();
        uForce.Name.Returns("lbf");
        unitMngr.Get(IRobotUnitType.I_UT_FORCE).Returns(uForce);

        var uMoment = Substitute.For<RobotUnitData>();
        uMoment.Name.Returns("lbf*in");
        unitMngr.Get(IRobotUnitType.I_UT_MOMENT).Returns(uMoment);

        var uStress = Substitute.For<RobotUnitData>();
        uStress.Name.Returns("psi");
        unitMngr.Get(IRobotUnitType.I_UT_STRESS).Returns(uStress);

        // Assert that unhandled exception is NOT suppressed
        var ex = Assert.Throws<ApplicationException>(() =>
        {
            RobotUnitsPolicy.Run(unitMngr, logs, () =>
            {
                // Verify Metric was active when exception occurred
                Assert.Equal("m", uDim.Name);
                Assert.Equal("kN", uForce.Name);
                Assert.Equal("kN*m", uMoment.Name);
                Assert.Equal("MPa", uStress.Name);

                throw new ApplicationException("Fatal FEA simulation crash in script");
            });
        });

        Assert.Equal("Fatal FEA simulation crash in script", ex.Message);

        // Verify restoration executed in finally block
        Assert.Equal("in", uDim.Name);
        Assert.Equal("lbf", uForce.Name);
        Assert.Equal("lbf*in", uMoment.Name);
        Assert.Equal("psi", uStress.Name);
        unitMngr.Received().UseMetricAsDefault = false;

        Assert.Empty(logs);
    }

    [Fact]
    public void Run_WhenPreExecutionUnitSetupThrows_LogsWarningAndStillExecutesScriptAndFinally()
    {
        var unitMngr = Substitute.For<IRobotUnitMngr>();
        var logs = new List<string>();

        // Force unitMngr.Get to throw a simulated COM exception
        unitMngr.Get(Arg.Any<IRobotUnitType>()).Throws(new System.Runtime.InteropServices.COMException("COM busy error 0x8001010A"));

        bool scriptExecuted = false;
        RobotUnitsPolicy.Run(unitMngr, logs, () =>
        {
            scriptExecuted = true;
        });

        // Script must still be given an opportunity to run
        Assert.True(scriptExecuted);

        // Warning must be logged in logs collection
        Assert.Single(logs);
        Assert.Contains("standardizing units to Metric threw COMException", logs[0]);
    }

    [Fact]
    public void Run_WhenPostExecutionRestorationThrows_LogsWarningWithoutMaskingOriginalException()
    {
        var unitMngr = Substitute.For<IRobotUnitMngr>();
        var logs = new List<string>();

        var uDim = Substitute.For<RobotUnitData>();
        uDim.Name.Returns("cm");
        unitMngr.Get(IRobotUnitType.I_UT_STRUCTURE_DIMENSION).Returns(uDim);

        // Make Set throw during restoration in finally
        unitMngr.When(m => m.Set(IRobotUnitType.I_UT_STRUCTURE_DIMENSION, Arg.Any<RobotUnitData>()))
            .Do(call =>
            {
                // Throw on the second call (restoration)
                if (uDim.Name == "cm")
                    throw new System.Runtime.InteropServices.COMException("RPC server disconnected");
            });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            RobotUnitsPolicy.Run(unitMngr, logs, () =>
            {
                throw new InvalidOperationException("Original script business error");
            });
        });

        // Crucial: The original script exception is preserved, NOT swallowed by the finally block error
        Assert.Equal("Original script business error", ex.Message);

        // Warning about restoration failure must be captured in logs
        Assert.Contains(logs, l => l.Contains("restoring user units threw COMException"));
    }
}
