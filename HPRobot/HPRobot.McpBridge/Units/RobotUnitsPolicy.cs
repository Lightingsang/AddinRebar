using System;
using System.Collections.Generic;
using HPRebar.McpBridge.Core.Scripting;
using RobotOM;
using Serilog;

namespace HPRobot.McpBridge.Units;

/// <summary>
///     Enforces standard Metric units (Meter, kN, kN·m, MPa) on the Robot Unit Manager during script execution,
///     restoring user preferences in a finally block regardless of script success or failure.
/// </summary>
public static class RobotUnitsPolicy
{
    public const string MetricSystemName = "Metric";

    /// <summary>
    ///     ScriptUnits descriptor describing the standardized Metric system.
    /// </summary>
    public static ScriptUnits Units { get; } = new(
        MetricSystemName,
        1.0,
        "Units standardized to Metric: length m, force kN, moment kN·m, stress MPa. User preferences restored afterwards.");

    /// <summary>
    ///     Runs the specified body under enforced metric units and restores original user units in finally.
    /// </summary>
    public static string? Run(IRobotUnitMngr? unitMngr, List<string> logs, Action body)
    {
        if (unitMngr == null)
        {
            body();
            return null;
        }

        bool savedMetricDefault = false;
        string? savedDim = null;
        string? savedForce = null;
        string? savedMoment = null;
        string? savedStress = null;

        try
        {
            savedMetricDefault = unitMngr.UseMetricAsDefault;

            var uDim = unitMngr.Get(IRobotUnitType.I_UT_STRUCTURE_DIMENSION);
            if (uDim != null) savedDim = uDim.Name;

            var uForce = unitMngr.Get(IRobotUnitType.I_UT_FORCE);
            if (uForce != null) savedForce = uForce.Name;

            var uMoment = unitMngr.Get(IRobotUnitType.I_UT_MOMENT);
            if (uMoment != null) savedMoment = uMoment.Name;

            var uStress = unitMngr.Get(IRobotUnitType.I_UT_STRESS);
            if (uStress != null) savedStress = uStress.Name;

            // Enforce metric defaults
            unitMngr.UseMetricAsDefault = true;

            if (uDim != null) { uDim.Name = "m"; unitMngr.Set(IRobotUnitType.I_UT_STRUCTURE_DIMENSION, uDim); }
            if (uForce != null) { uForce.Name = "kN"; unitMngr.Set(IRobotUnitType.I_UT_FORCE, uForce); }
            if (uMoment != null) { uMoment.Name = "kN*m"; unitMngr.Set(IRobotUnitType.I_UT_MOMENT, uMoment); }
            if (uStress != null) { uStress.Name = "MPa"; unitMngr.Set(IRobotUnitType.I_UT_STRESS, uStress); }

            unitMngr.Refresh();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply standard Metric units before script execution.");
            logs.Add($"warning: standardizing units to Metric threw {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            body();
        }
        finally
        {
            try
            {
                unitMngr.UseMetricAsDefault = savedMetricDefault;

                if (savedDim != null)
                {
                    var uDim = unitMngr.Get(IRobotUnitType.I_UT_STRUCTURE_DIMENSION);
                    if (uDim != null) { uDim.Name = savedDim; unitMngr.Set(IRobotUnitType.I_UT_STRUCTURE_DIMENSION, uDim); }
                }
                if (savedForce != null)
                {
                    var uForce = unitMngr.Get(IRobotUnitType.I_UT_FORCE);
                    if (uForce != null) { uForce.Name = savedForce; unitMngr.Set(IRobotUnitType.I_UT_FORCE, uForce); }
                }
                if (savedMoment != null)
                {
                    var uMoment = unitMngr.Get(IRobotUnitType.I_UT_MOMENT);
                    if (uMoment != null) { uMoment.Name = savedMoment; unitMngr.Set(IRobotUnitType.I_UT_MOMENT, uMoment); }
                }
                if (savedStress != null)
                {
                    var uStress = unitMngr.Get(IRobotUnitType.I_UT_STRESS);
                    if (uStress != null) { uStress.Name = savedStress; unitMngr.Set(IRobotUnitType.I_UT_STRESS, uStress); }
                }

                unitMngr.Refresh();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to restore original user units.");
                logs.Add($"warning: restoring user units threw {ex.GetType().Name}: {ex.Message}");
            }
        }

        return null;
    }
}
