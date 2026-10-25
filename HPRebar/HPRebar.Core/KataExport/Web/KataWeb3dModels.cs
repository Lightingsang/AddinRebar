using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataExport.Web;

// The rebar plan as the page's 3D view draws it, in the layout's local millimetres: X along the beam from the outer
// face of its first support, Y across it from its centre line, Z up from its top. Built when the page opens the view.

public sealed record KataWebPoint3(double X, double Y, double Z);

/// <param name="Groups">Bars of one shape: each drawn once per offset.</param>
/// <param name="Concrete">The beam per span (split at top steps) and the columns it runs into.</param>
/// <param name="BarCount">Bars of the plan; the sum of the groups' offsets.</param>
public sealed record KataWeb3dModel(
    IReadOnlyList<KataWeb3dGroup> Groups,
    IReadOnlyList<KataWeb3dBox> Concrete,
    double Length,
    int BarCount);

/// <summary>
/// One bar shape laid at each of <paramref name="Offsets" /> along X: a longitudinal bar once, a stirrup zone or a set
/// of ties at its stations. Hooks are not drawn.
/// </summary>
/// <param name="Key">What a click selects and Delete removes (<see cref="Calculators.KataLayoutRemoval" />); empty
/// for bars removed only with their zone (inner U / C stirrups, C ties).</param>
public sealed record KataWeb3dGroup(
    string Key,
    KataBarRole Role,
    double Diameter,
    int Number,
    IReadOnlyList<KataWebPoint3> Points,
    bool Closed,
    IReadOnlyList<double> Offsets);

public sealed record KataWeb3dBox(double X1, double X2, double Y1, double Y2, double Z1, double Z2, KataWeb3dBoxKind Kind);

public enum KataWeb3dBoxKind
{
    Beam,
    Column
}
