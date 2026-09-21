using System.Collections.Generic;

namespace HPRebar.Mcp.Contracts.Messages;

/// <summary>Parameters of `revit.context`.</summary>
public sealed record ContextRequest(bool IncludeSelection = false);

/// <summary>
///     Snapshot of the Revit session the AI is about to script against. Enough to write correct code
///     (units, active view, what is selected) without a round trip per question.
/// </summary>
public sealed class ContextResult
{
    /// <summary>Major version of the host; the name predates the AutoCAD bridge and stays for wire compatibility.</summary>
    public string RevitVersion { get; set; } = string.Empty;

    /// <summary>Host application id (`revit`, `autocad`). Null from bridges built before the field existed — read as `revit`.</summary>
    public string? Host { get; set; }

    /// <summary>Same value as <see cref="RevitVersion"/> under a host-neutral name; null from older bridges.</summary>
    public string? HostVersion { get; set; }

    /// <summary>AutoCAD-only facts; null for Revit.</summary>
    public AutocadInfo? Autocad { get; set; }

    /// <summary>Navisworks-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public NavisInfo? Navis { get; set; }

    /// <summary>ETABS-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public EtabsInfo? Etabs { get; set; }

    /// <summary>Civil 3D-only facts; null for the other hosts (and omitted from the JSON). The Civil 3D bridge also fills <see cref="Autocad"/>.</summary>
    public Civil3dInfo? Civil3d { get; set; }

    /// <summary>SAP2000-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public Sap2000Info? Sap2000 { get; set; }

    /// <summary>Power BI-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public PowerBiInfo? PowerBi { get; set; }

    /// <summary>Excel-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public ExcelInfo? Excel { get; set; }

    /// <summary>Robot Structural Analysis-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public RobotInfo? Robot { get; set; }

    /// <summary>Tekla Structures-only facts; null for the other hosts (and omitted from the JSON).</summary>
    public TeklaInfo? Tekla { get; set; }

    public string? DocTitle { get; set; }

    public string? DocPath { get; set; }

    public bool IsFamily { get; set; }

    public bool IsReadOnly { get; set; }

    /// <summary>
    ///     Host-specific readiness flag. Revit: a transaction is currently open on the document
    ///     (<c>Document.IsModifiable</c>). AutoCAD: the drawing is writable and the editor is quiescent —
    ///     no command or dialog in progress — so a script can run now.
    /// </summary>
    public bool IsModifiable { get; set; }

    public UnitsInfo Units { get; set; } = new UnitsInfo("mm");

    public ViewInfo? ActiveView { get; set; }

    public IReadOnlyList<ElementInfo> Selection { get; set; } = System.Array.Empty<ElementInfo>();

    public IReadOnlyList<string> OpenDocs { get; set; } = System.Array.Empty<string>();

    public bool ExecutionEnabled { get; set; }
}

/// <summary>Display unit for lengths in the document; the Revit API itself always works in feet.</summary>
public sealed record UnitsInfo(string Length);

/// <summary>
///     What an AutoCAD script needs to know that has no Revit counterpart: drawing units are a label
///     (<see cref="Insunits"/>), the current layout/space decides where new entities land, and a
///     non-quiescent editor means a command or dialog is blocking the main thread.
/// </summary>
public sealed record AutocadInfo(
    string Insunits,
    string Measurement,
    string? CurrentLayout,
    string? CurrentLayer,
    bool IsModelSpace,
    bool IsQuiescent,
    bool IsNamedDrawing);

/// <summary>
///     What a Navisworks script needs to know that has no counterpart elsewhere: the document unit every
///     API length is expressed in, the appended models (each with its source unit), how many review
///     artefacts exist, whether the Clash module is available (Manage vs Simulate), whether the user has
///     enabled heavy operations, and the bridge's own busy/idle composite.
/// </summary>
public sealed record NavisInfo(
    string DocumentUnits,
    int ModelCount,
    IReadOnlyList<ModelSummary> Models,
    int SelectionSetCount,
    int SavedViewpointCount,
    int ClashTestCount,
    bool HasClashModule,
    bool HeavyOperationsEnabled,
    bool IsClear,
    bool IsBusy,
    bool IsModified);

/// <summary>One appended model: file name, the unit its geometry was authored in, and the source it came from.</summary>
public sealed record ModelSummary(string FileName, string Units, string? SourceFileName);

/// <summary>
///     What an ETABS script needs to know that has no counterpart elsewhere. The bridge is a separate desktop
///     app attached over COM to one running ETABS: whether it is attached and to which process, the OAPI
///     wrapper version, whether the model is locked (definitions cannot change until it is unlocked, which
///     discards results), the unit system the user's API session is in and the one the model is stored in
///     (the bridge forces kN/mm for every run and restores this afterwards), whether the user allowed
///     destructive operations, and coarse object counts. Path, title, host version and busy state travel in
///     the common <see cref="ContextResult"/> fields. Version and units are null while nothing is attached.
/// </summary>
public sealed record EtabsInfo(
    bool IsAttached,
    int AttachedPid,
    string? OapiVersion,
    bool IsLocked,
    string? PresentUnits,
    string? DatabaseUnits,
    bool DestructiveOperationsEnabled,
    int PointCount,
    int FrameCount,
    int AreaCount);

/// <summary>
///     What a Civil 3D script needs to know beyond the AutoCAD facts (which the Civil 3D bridge reports too):
///     which product hosts the bridge, whether the active drawing is a Civil document, the Civil drawing unit
///     (Meters or Feet — the unit every Civil coordinate, station and elevation is expressed in), the coordinate
///     system code when a zone is assigned, whether INSUNITS disagrees with the Civil unit, and coarse object
///     counts so the AI can pick a tool before querying.
/// </summary>
public sealed record Civil3dInfo(
    string Product,
    bool IsCivilDocument,
    string? DrawingUnit,
    string? CoordinateSystemCode,
    bool InsunitsMismatch,
    int AlignmentCount,
    int SurfaceCount,
    int CorridorCount,
    int PipeNetworkCount,
    int PressureNetworkCount,
    int CogoPointCount);

/// <summary>
///     What a SAP2000 script needs to know that has no counterpart elsewhere. The bridge is a separate desktop
///     app attached over COM to one running SAP2000: whether it is attached and to which process, the OAPI
///     wrapper version, whether the model is locked, present and database units, whether the user allowed
///     destructive operations, and coarse object counts.
/// </summary>
public sealed record Sap2000Info(
    bool IsAttached,
    int AttachedPid,
    string? OapiVersion,
    bool IsLocked,
    string? PresentUnits,
    string? DatabaseUnits,
    bool DestructiveOperationsEnabled,
    int PointCount,
    int FrameCount,
    int AreaCount);

/// <summary>
///     What a Power BI script needs to know: connection state, attached PID/local port, database name,
///     compatibility level, whether mutation is enabled on the bridge, and coarse model object counts.
/// </summary>
public sealed record PowerBiInfo(
    bool IsConnected,
    int? AttachedPid,
    int? LocalPort,
    string? DatabaseName,
    string? CompatibilityLevel,
    bool MutationEnabled,
    int TableCount,
    int MeasureCount,
    int RelationshipCount);

/// <summary>
///     What an Excel script needs to know: whether attached to a live running Excel process,
///     attached PID, Excel version, active workbook name, active worksheet name, selection address,
///     whether write operations are enabled, whether destructive operations are enabled,
///     open workbook count, worksheet count, and whether there is an active workbook.
/// </summary>
public sealed record ExcelInfo(
    bool IsAttached,
    int? AttachedPid,
    string? ExcelVersion,
    string? ActiveWorkbookName,
    string? ActiveWorksheetName,
    string? SelectionAddress,
    bool WriteEnabled,
    bool DestructiveEnabled,
    int OpenWorkbookCount,
    int WorksheetCount,
    bool HasActiveWorkbook);

/// <summary>
///     What a Robot Structural Analysis script needs to know that has no counterpart elsewhere. The bridge is a
///     separate desktop app attached over COM to Robot Structural Analysis Professional: whether attached,
///     attached PID, Robot version, project structure type, whether model is calculated,
///     heavy operations enabled, and coarse structural object counts (nodes, bars, panels, load cases).
/// </summary>
public sealed record RobotInfo(
    bool IsAttached,
    int? AttachedPid,
    string? RobotVersion,
    string? StructureType,
    bool IsCalculated,
    bool HeavyOperationsEnabled,
    int NodeCount,
    int BarCount,
    int PanelCount,
    int LoadCaseCount);

/// <summary>
///     What a Tekla Structures script needs to know: connection state, active model name and folder path,
///     project name, Tekla major version, whether heavy/destructive operations are enabled, and coarse
///     object counts (parts, rebar, drawings).
/// </summary>
public sealed record TeklaInfo(
    bool IsConnected,
    string? ModelName,
    string? ModelPath,
    string? ProjectName,
    string? TeklaVersion,
    bool HeavyOperationsEnabled,
    int PartCount,
    int RebarCount,
    int DrawingCount);

public sealed record ViewInfo(long Id, string Name, string Type);

public sealed record ElementInfo(long Id, string? Category, string? Name);

/// <summary>Parameters of `revit.inspect`: reflect over a Revit API type so the AI can discover members instead of guessing.</summary>
public sealed record InspectRequest(string TypeName, string? MemberFilter = null, int MaxMembers = 100);

public sealed class InspectResult
{
    public string TypeName { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? BaseType { get; set; }

    public IReadOnlyList<MemberSignature> Members { get; set; } = System.Array.Empty<MemberSignature>();

    public bool Truncated { get; set; }

    /// <summary>Set when the type was not found; lists close matches when there are any.</summary>
    public string? Message { get; set; }
}

/// <summary>Kind is `property` / `method` / `field` / `event`; signature is C#-like text.</summary>
public sealed record MemberSignature(string Kind, string Signature);

public sealed record CancelResult(bool Cancelled, bool WasRunning);

public sealed record BridgePingResult(bool Pong, string RevitVersion, bool ExecutionEnabled, bool Busy);

/// <summary>Payload of the `revit.progress` notification; <see cref="Id"/> is the request it belongs to.</summary>
public sealed record ProgressParams(long Id, int Progress, int? Total, string? Message);

/// <summary>Payload of the `revit.status` notification, sent whenever the bridge's state changes.</summary>
public sealed record StatusParams(bool Listening, bool ExecutionEnabled, bool Busy, string? DocTitle, string RevitVersion);

/// <summary>Payload of the `revit.log` notification: a bridge log line the server forwards to stderr.</summary>
public sealed record LogParams(string Level, string Message);
