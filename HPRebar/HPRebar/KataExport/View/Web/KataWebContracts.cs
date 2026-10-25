using HPRebar.Core.KataExport.Web;
using HPRebar.KataExport.ViewModel;

namespace HPRebar.KataExport.View.Web;

/// <summary>Commands the Kata Export page sends (protocol v1 of HPWebHost), mirrored in the page's kataContracts.ts.</summary>
public static class KataWebCommands
{
    public const string GetState = "kata.getState";

    /// <summary>The page has loaded (or reloaded) and listens: the host sends the scene (<see cref="KataWebEvents.Scene" />).</summary>
    public const string Ready = "kata.ready";

    /// <summary>Changes what the user can edit on the window: parameters, direction, selected column, layers.</summary>
    public const string Update = "kata.update";

    /// <summary>Starts one of <see cref="KataWebActions" />; the outcome arrives as <see cref="KataWebEvents.State" />.</summary>
    public const string Run = "kata.run";

    /// <summary>Kata's section n-n of the current plan (<see cref="KataSectionRequest" />), built when asked.</summary>
    public const string Section = "kata.section";

    /// <summary>Strikes bar groups off the plan (<see cref="KataRemoveBarsRequest" />); undone by the undoRemoveBars action.</summary>
    public const string RemoveBars = "kata.removeBars";

    /// <summary>The current plan as the page's 3D view draws it (<see cref="HPRebar.Core.KataExport.Web.KataWeb3dModel" />), built when asked.</summary>
    public const string Model3d = "kata.model3d";
}

public static class KataWebEvents
{
    /// <summary>Everything the page shows except the drawing, sent whenever it changes.</summary>
    public const string State = "kata.state";

    /// <summary>What the page draws itself (<see cref="KataSceneFrame" />), sent when the elevation, plan or station map change.</summary>
    public const string Scene = "kata.scene";

    /// <summary>The view model asks the drawing to frame a span or the whole run (<see cref="KataFocusEvent" />).</summary>
    public const string Focus = "kata.focus";
}

/// <summary>What <see cref="KataWebCommands.Run" /> can start: the view model's commands of the same names.</summary>
public static class KataWebActions
{
    public const string Export = "export";
    public const string Repick = "repick";
    public const string RefreshExcel = "refreshExcel";
    public const string LoadRebar = "loadRebar";
    public const string GenerateRebar = "generateRebar";
    public const string UndoRemoveBars = "undoRemoveBars";
    public const string PreviousSpan = "previousSpan";
    public const string NextSpan = "nextSpan";
    public const string FrameAll = "frameAll";
    public const string Close = "close";
}

public sealed record KataViewState(
    string Theme,
    KataHeaderState Header,
    IReadOnlyList<string> Parameters,
    string NameParameter,
    string CountParameter,
    bool IsReverse,
    bool IsBusy,
    string Status,
    bool HasError,
    string Workbook,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<KataPreviewColumn> Columns,
    int SelectedColumn,
    string SelectionText,
    bool ShowRebar,
    bool ShowSection,
    bool ShowBarTags,
    KataCommandStates Can);

/// <summary>The cells B3:B10 as the sheet will receive them, plus the run's element count and length.</summary>
public sealed record KataHeaderState(
    string BeamName,
    string BeamCount,
    string Height,
    string Width,
    string SlabThickness,
    string AxisGrid,
    string AxisOffset,
    string Elevation,
    string ElementCount,
    string TotalLength);

/// <summary>Which buttons are enabled, from the view model's own CanExecute.</summary>
public sealed record KataCommandStates(bool Export, bool Repick, bool LoadRebar, bool GenerateRebar, bool UndoRemoveBars);

/// <summary>Only the members given change.</summary>
public sealed record KataUpdateRequest(
    string? NameParameter = null,
    string? CountParameter = null,
    bool? Reverse = null,
    int? SelectedColumn = null,
    bool? ShowRebar = null,
    bool? ShowSection = null,
    bool? ShowBarTags = null);

public sealed record KataRunRequest(string Action = "");

public sealed record KataRunResult(bool Started);

/// <param name="Mirror">Seen from the other side; null = as the elevation shows the run (mirrored when it is drawn backwards).</param>
public sealed record KataSectionRequest(int Number = 0, bool? Mirror = null);

/// <param name="Keys">Bar or stirrup-zone keys of the lines picked (<see cref="KataWebLine.Keys" />).</param>
public sealed record KataRemoveBarsRequest(IReadOnlyList<string>? Keys = null);

/// <param name="Revision">Grows with every scene sent; the page drops an older one arriving late.</param>
/// <param name="Scene">Null when the run has no elevation (the state's status says why) or it could not be built.</param>
/// <param name="Error">Why the scene could not be built; null otherwise.</param>
public sealed record KataSceneFrame(int Revision, KataWebScene? Scene, string? Error = null);

/// <param name="ColumnIndex">The span to frame with its two supports; null frames the whole run.</param>
/// <param name="Serial">Makes a repeated request a new one.</param>
public sealed record KataFocusEvent(int? ColumnIndex, int Serial);
