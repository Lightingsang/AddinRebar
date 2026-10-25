using System.Collections.Generic;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataExport.Web;

// What the Kata Export page draws, as plain records in model millimetres: built once per elevation or rebar plan, so
// zooming and panning never come back to Revit. Tuples become {x, z} objects and computed members become fields,
// because the page reads JSON (camelCase, enums as camelCase strings).

/// <summary>A point of a drawing: X along the beam (or across it in a section), Z up, mm.</summary>
public sealed record KataWebPoint(double X, double Z);

/// <summary>A range of stations, mm.</summary>
public sealed record KataWebRange(double Start, double End);

/// <param name="StationMap">Places the rebar layout's local X on the elevation's stations.</param>
public sealed record KataWebScene(KataWebElevation Elevation, KataWebRebar? Rebar, KataWebStationMap StationMap);

public sealed record KataWebStationMap(double Origin, int Direction);

/// <summary>The beam run as the plain elevation draws it (<see cref="KataElevation" />).</summary>
public sealed record KataWebElevation(
    IReadOnlyList<KataWebColumn> Columns,
    IReadOnlyList<KataWebBeam> Beams,
    IReadOnlyList<KataWebSupport> Supports,
    IReadOnlyList<KataWebGrid> Grids,
    IReadOnlyList<KataWebDimension> GridDimensions,
    KataWebRange Bounds,
    double TopMm,
    double BottomMm,
    double MaxBeamHeightMm);

public sealed record KataWebColumn(
    int Index,
    string Letter,
    KataColumnKind Kind,
    KataWebRange Extent,
    string Row11,
    string Row19,
    string Row20,
    string Row21,
    string Row22,
    string Row23,
    int? SpanNumber,
    string? SpanSection);

public sealed record KataWebBeam(KataWebRange Extent, double TopMm, double BottomMm, double HeightMm);

public sealed record KataWebUpper(KataWebRange Extent, string Text);

public sealed record KataWebSupport(
    int ColumnIndex,
    KataSupportKind Kind,
    KataWebRange Extent,
    double? SectionHeightMm,
    KataWebUpper? Upper,
    double? CrossingBeamX);

public sealed record KataWebGrid(string Name, double X, string? OffsetText);

public sealed record KataWebDimension(KataWebRange Extent, string Text);

/// <summary>
/// Kata's elevation of the rebar plan (<see cref="KataElevationDrawing" />), its bar tags and section cuts, in the
/// layout's local X (map it with <see cref="KataWebScene.StationMap" />).
/// </summary>
public sealed record KataWebRebar(
    IReadOnlyList<KataWebLine> Lines,
    IReadOnlyList<KataWebDim> Dims,
    IReadOnlyList<KataWebFlag> Flags,
    IReadOnlyList<KataWebBubble> Bubbles,
    KataWebLevel? Level,
    KataWebTitle? Title,
    double MinX,
    double MaxX,
    double Top,
    double Bottom,
    IReadOnlyList<KataWebTag> Tags,
    IReadOnlyList<KataWebCut> Cuts,
    int BarCount,
    double SteelWeightKg);

/// <param name="Keys">Bars or stirrup zones the line stands for: what a click selects and Delete removes.</param>
public sealed record KataWebLine(KataDrawingPen Pen, IReadOnlyList<KataWebPoint> Points, IReadOnlyList<string> Keys);

/// <param name="Text">The measured value as Kata writes it; empty for a stirrup run.</param>
public sealed record KataWebDim(double X1, double Z1, double X2, double Z2, bool Vertical, double LineAt, KataDimStyle Style, string Text);

public sealed record KataWebFlag(double X, double Z, int Number, bool Below);

public sealed record KataWebBubble(double X, double Z, string Name);

public sealed record KataWebLevel(double X, double Z, string Text);

public sealed record KataWebTitle(double X, double Z, string Name, string Scale);

/// <summary>One elevation tag (<see cref="KataBarTag" />), with its insertion point resolved.</summary>
public sealed record KataWebTag(
    IReadOnlyList<int> Numbers,
    string Text,
    double X,
    double RowZ,
    bool Above,
    bool PointsLeft,
    IReadOnlyList<double> FootZ,
    double LeaderLength,
    double InsertX,
    KataTagKind Kind);

public sealed record KataWebCut(int Number, double X, int SpanIndex);

/// <summary>Kata's section n-n (<see cref="KataSectionDrawing" />): X across the beam from its centre line, Z up from its top.</summary>
public sealed record KataWebSection(
    int Number,
    bool Mirror,
    IReadOnlyList<KataWebPolyline> Lines,
    IReadOnlyList<KataWebSectionBar> Bars,
    IReadOnlyList<KataWebLeader> Leaders,
    IReadOnlyList<KataWebMark> Marks,
    IReadOnlyList<KataWebSectionTag> Tags,
    IReadOnlyList<KataWebDim> Dims,
    KataWebTitle Title,
    double MinX,
    double MaxX,
    double Top,
    double Bottom);

/// <summary>A polyline vertex; a non-zero bulge turns the segment to the next vertex into an arc (AutoCAD's bulge).</summary>
public sealed record KataWebVertex(double X, double Z, double Bulge);

public sealed record KataWebPolyline(KataDrawingPen Pen, IReadOnlyList<KataWebVertex> Vertices);

public sealed record KataWebSectionBar(double X, double Z, double Diameter, int Number);

public sealed record KataWebLeader(IReadOnlyList<KataWebPoint> Points, KataLeaderArrow Arrow, double ArrowSize);

public sealed record KataWebMark(double X, double Z, double Radius);

public sealed record KataWebSectionTag(
    double X,
    double Z,
    bool PointsRight,
    string Text,
    IReadOnlyList<int> Numbers,
    string Spacing,
    KataTagLayout Layout,
    string BlockState);
