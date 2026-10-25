using HPRebar.Core.KataExport.Web;
using HPRebar.KataExport.ViewModel;

namespace HPRebar.KataExport.View.Web;

/// <summary>Commands the Kata Export page sends (protocol v1 of HPWebHost), mirrored in the page's kataContracts.ts.</summary>
public static class KataWebCommands
{
    public const string GetState = "kata.getState";

    /// <summary>Changes what the user can edit on the window: parameters, direction, selected column, layers.</summary>
    public const string Update = "kata.update";

    /// <summary>Starts one of <see cref="KataWebActions" />; the outcome arrives as <see cref="KataWebEvents.State" />.</summary>
    public const string Run = "kata.run";

    /// <summary>The size of the page's drawing area: the elevation is painted to fit it.</summary>
    public const string Viewport = "kata.viewport";

    /// <summary>A gesture on the drawing (<see cref="KataCanvasInput.Kind" />), handled by the WPF canvas itself.</summary>
    public const string Canvas = "kata.canvas";

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

    /// <summary>The elevation as SVG, sent whenever the canvas paints something different.</summary>
    public const string Drawing = "kata.drawing";

    /// <summary>What the page draws itself (<see cref="KataSceneFrame" />), sent when the elevation, plan or station map change.</summary>
    public const string Scene = "kata.scene";

    /// <summary>The view model asks the drawing to frame a span or the whole run (<see cref="KataFocusEvent" />).</summary>
    public const string Focus = "kata.focus";
}

/// <summary>What <see cref="KataWebCommands.Run" /> can start; the same commands the WPF window's buttons run.</summary>
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

/// <summary>Kinds of <see cref="KataCanvasInput" />.</summary>
public static class KataCanvasGestures
{
    public const string Zoom = "zoom";
    public const string Pan = "pan";
    public const string Click = "click";
    public const string ZoomExtents = "zoomExtents";
    public const string Key = "key";
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

/// <summary>Size of the drawing area in CSS pixels (= WPF device-independent pixels).</summary>
public sealed record KataViewportRequest(double Width = 0, double Height = 0);

/// <param name="Kind">One of <see cref="KataCanvasGestures" />.</param>
/// <param name="X">Where the gesture happened, in drawing pixels (a pan's start point).</param>
/// <param name="Dx">A pan's whole movement.</param>
/// <param name="Notches">Wheel notches, + zooms in.</param>
/// <param name="Key">For <see cref="KataCanvasGestures.Key" />: <c>Delete</c>, <c>Escape</c> or <c>z</c>.</param>
public sealed record KataCanvasInput(
    string Kind = "",
    double X = 0,
    double Y = 0,
    double Dx = 0,
    double Dy = 0,
    double Notches = 0,
    string? Key = null,
    bool Ctrl = false);

public sealed record KataCanvasResult(bool Handled);

/// <param name="Mirror">Seen from the other side; null = as the elevation shows the run (mirrored when it is drawn backwards).</param>
public sealed record KataSectionRequest(int Number = 0, bool? Mirror = null);

/// <param name="Keys">Bar or stirrup-zone keys of the lines picked (<see cref="KataWebLine.Keys" />).</param>
public sealed record KataRemoveBarsRequest(IReadOnlyList<string>? Keys = null);

/// <param name="Revision">Grows with every scene sent; the page drops an older one arriving late.</param>
public sealed record KataSceneFrame(int Revision, KataWebScene? Scene);

/// <param name="ColumnIndex">The span to frame with its two supports; null frames the whole run.</param>
/// <param name="Serial">Makes a repeated request a new one.</param>
public sealed record KataFocusEvent(int? ColumnIndex, int Serial);

/// <param name="Revision">Grows with every drawing sent; the page drops an older one arriving late.</param>
/// <param name="HasBarSelection">A bar group is selected: Delete removes it.</param>
public sealed record KataDrawingFrame(int Revision, double Width, double Height, string Svg, bool HasBarSelection);
