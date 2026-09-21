using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RobotOM;
using Serilog;

namespace HPRobot.McpBridge.Com;

public enum RobotConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Attached = 2
}

/// <summary>
///     Manages out-of-process COM attachment to Autodesk Robot Structural Analysis Professional 2026:
///     - Detects running `robot.exe` processes.
///     - Attaches via Marshal2.GetActiveObject("Robot.Application") against the ROT.
///     - Tracks COM lifecycle, active model file, structure type, and object counts.
/// </summary>
public sealed class RobotAttachment : IDisposable
{
    private readonly object _gate = new();
    private IRobotApplication? _robot;
    private Process? _processWatch;

    public RobotConnectionState State { get; private set; } = RobotConnectionState.Disconnected;

    public bool IsAttached => State == RobotConnectionState.Attached && _robot != null;

    public int? AttachedPid { get; private set; }

    public string? RobotVersion { get; private set; }

    public string? ProgramVersion { get; private set; }

    public string? ActiveModelPath { get; private set; }

    public string? ActiveModelFileName => string.IsNullOrEmpty(ActiveModelPath) ? null : Path.GetFileName(ActiveModelPath);

    public string? StructureType { get; private set; }

    public bool IsModelActive { get; private set; }

    public bool IsCalculated { get; private set; }

    public int NodeCount { get; private set; }

    public int BarCount { get; private set; }

    public int PanelCount { get; private set; }

    public int LoadCaseCount { get; private set; }

    public event Action? StateChanged;

    /// <summary>
    ///     Gets the active IRobotApplication instance. Throws if not attached.
    /// </summary>
    public IRobotApplication GetApplication()
    {
        lock (_gate)
        {
            if (_robot == null || State != RobotConnectionState.Attached)
                throw new InvalidOperationException("Not attached to Autodesk Robot Structural Analysis Professional 2026. Attach to a running instance first.");
            return _robot;
        }
    }

    /// <summary>
    ///     Attempts to attach to an active Robot 2026 instance.
    /// </summary>
    public void Attach()
    {
        lock (_gate)
        {
            if (IsAttached) return;

            State = RobotConnectionState.Connecting;
            StateChanged?.Invoke();

            try
            {
                var processes = Process.GetProcessesByName("robot");
                if (processes.Length == 0)
                {
                    Log.Information("No running 'robot.exe' process found.");
                }

                var punk = Marshal2.GetActiveObject(ComInteropHelper.RobotProgId);
                if (punk == null)
                {
                    var robotType = Type.GetTypeFromProgID(ComInteropHelper.RobotProgId);
                    if (robotType != null)
                    {
                        try
                        {
                            punk = Activator.CreateInstance(robotType);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex, "Activator.CreateInstance fallback for Robot failed");
                        }
                    }
                }

                if (punk == null)
                {
                    State = RobotConnectionState.Disconnected;
                    StateChanged?.Invoke();
                    throw new InvalidOperationException(
                        processes.Length > 0
                            ? "Robot is running but not registered in the Running Object Table (ROT). If Robot was run as Administrator, run this bridge as Administrator too."
                            : "Robot Structural Analysis Professional 2026 is not running. Launch robot.exe and click Attach.");
                }

                _robot = (IRobotApplication)punk;

                if (processes.Length > 0)
                {
                    AttachedPid = processes[0].Id;
                    try
                    {
                        _processWatch = processes[0];
                        _processWatch.EnableRaisingEvents = true;
                        _processWatch.Exited += (_, _) => Detach("Robot process exited");
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Could not enable process exit events for PID {Pid}", AttachedPid);
                    }
                }

                try
                {
                    RobotVersion = _robot.Version.ToString();
                    ProgramVersion = _robot.ProgramVersion;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Could not read version from Robot");
                }

                State = RobotConnectionState.Attached;
                RefreshContext();
                Log.Information("Successfully attached to Robot 2026 (PID: {Pid}, Version: {Ver}, ProgVer: {ProgVer})",
                    AttachedPid, RobotVersion, ProgramVersion);
                StateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Detach("Attachment failed: " + ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    ///     Detaches from Robot and releases COM references.
    /// </summary>
    public void Detach(string reason = "Disconnected")
    {
        lock (_gate)
        {
            if (State == RobotConnectionState.Disconnected && _robot == null)
                return;

            Log.Information("Detaching from Robot: {Reason}", reason);

            try
            {
                if (_processWatch != null)
                {
                    _processWatch.Dispose();
                    _processWatch = null;
                }
            }
            catch { }

            if (_robot != null)
            {
                ComInteropHelper.ReleaseComObject(_robot);
                _robot = null;
            }

            State = RobotConnectionState.Disconnected;
            AttachedPid = null;
            ActiveModelPath = null;
            StructureType = null;
            IsModelActive = false;
            IsCalculated = false;
            NodeCount = 0;
            BarCount = 0;
            PanelCount = 0;
            LoadCaseCount = 0;

            StateChanged?.Invoke();
        }
    }

    /// <summary>
    ///     Refreshes model metadata, file path, structure type, and object counts from active Robot instance.
    /// </summary>
    public void RefreshContext()
    {
        lock (_gate)
        {
            if (_robot == null || State != RobotConnectionState.Attached)
                return;

            try
            {
                var project = _robot.Project;
                if (project == null || project.IsActive == 0)
                {
                    IsModelActive = false;
                    ActiveModelPath = null;
                    StructureType = null;
                    IsCalculated = false;
                    NodeCount = 0;
                    BarCount = 0;
                    PanelCount = 0;
                    LoadCaseCount = 0;
                    return;
                }

                IsModelActive = true;
                ActiveModelPath = project.FileName;
                StructureType = project.Type.ToString();

                var structure = project.Structure;
                if (structure != null)
                {
                    try { NodeCount = structure.Nodes?.GetAll()?.Count ?? 0; } catch { NodeCount = 0; }
                    try { BarCount = structure.Bars?.GetAll()?.Count ?? 0; } catch { BarCount = 0; }
                    try { PanelCount = structure.Objects?.GetAll()?.Count ?? 0; } catch { PanelCount = 0; }
                    try { LoadCaseCount = structure.Cases?.GetAll()?.Count ?? 0; } catch { LoadCaseCount = 0; }
                    try { IsCalculated = (structure.Results?.Available ?? 0) != 0; } catch { IsCalculated = false; }
                }
            }
            catch (COMException comEx)
            {
                Log.Warning(comEx, "COMException refreshing Robot context");
                DetachIfGone(comEx);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception refreshing Robot context");
            }
            finally
            {
                StateChanged?.Invoke();
            }
        }
    }

    /// <summary>
    ///     Checks if an exception indicates the host Robot instance terminated or disconnected.
    /// </summary>
    public Exception? DetachIfGone(Exception ex)
    {
        if (ex is COMException or InvalidComObjectException)
        {
            Detach("COM communication failed: " + ex.Message);
        }
        return null;
    }

    public void Dispose()
    {
        Detach("Disposed");
    }
}
