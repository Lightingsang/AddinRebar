using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Mep;

/// <summary>What an MEP run is: a pipe, a duct or a cable tray drawn as an open chain.</summary>
public static class MepRunKind
{
    public const string Pipe = "pipe";
    public const string Duct = "duct";
    public const string Tray = "tray";

    public static string? FromAecType(string aecType) => aecType switch
    {
        AecType.Pipe => Pipe,
        AecType.Duct => Duct,
        AecType.CableTray => Tray,
        _ => null,
    };
}

/// <summary>What an MEP node entity is: something runs start, end or pass through.</summary>
public static class MepNodeKind
{
    public const string Equipment = "equipment";
    public const string Fixture = "fixture";
    public const string Terminal = "terminal";
    public const string Fitting = "fitting";

    /// <summary>Nodes a run must reach: a diffuser or a fixture with no run serving it is a real problem, a lone fitting block is drafting noise.</summary>
    public static bool MustBeServed(string kind) => kind is Equipment or Fixture or Terminal;

    public static string? FromAecType(string aecType) => aecType switch
    {
        AecType.Equipment => Equipment,
        AecType.Fixture => Fixture,
        AecType.Terminal => Terminal,
        AecType.Fitting => Fitting,
        _ => null,
    };
}

/// <summary>A pipe / duct / tray run: an open chain with a system (from the caller's layer map, else its layer) and two ends.</summary>
public sealed record MepRun(string Handle, string Kind, string Layer, string System, PlanShape Shape, double LengthMm)
{
    public Pt Start => Shape.Start;
    public Pt End => Shape.End;
}

/// <summary>Equipment, a fixture, a terminal or a fitting: a shape (a block's stand-in box or a closed outline) runs connect to when they touch it.</summary>
public sealed record MepNode(string Handle, string Kind, string Layer, PlanShape Shape)
{
    public Pt CenterMm => Shape.Closed ? Shape.Centroid : Shape.Bounds.Center;
}

/// <summary>How a run end is connected.</summary>
public static class MepEndpointState
{
    /// <summary>Joined end to end with another run.</summary>
    public const string Joined = "joined";
    /// <summary>Lands on the body of another run (a branch).</summary>
    public const string Tee = "tee";
    /// <summary>Touches a node entity.</summary>
    public const string Node = "node";
    /// <summary>Connected to nothing.</summary>
    public const string Open = "open";
}

/// <summary>One end of a run: where it is, what it connects to, and — when open — the nearest run or node within the near-miss reach.</summary>
public sealed record MepEndpoint(string RunHandle, string System, int End, Pt PointMm, string State, IReadOnlyList<string> ConnectedTo, string? NearestHandle, double? NearestGapMm)
{
    public bool IsOpen => State == MepEndpointState.Open;
}

/// <summary>A connected set of runs with the nodes they touch: one system when the drawing is clean, several when runs of different systems join directly (a node never merges systems).</summary>
public sealed record MepNetwork(string Id, IReadOnlyList<MepRun> Runs, IReadOnlyList<MepNode> Nodes, IReadOnlyList<MepEndpoint> OpenEnds, IReadOnlyList<string> Systems)
{
    public string System => Systems.Count == 1 ? Systems[0] : "mixed";

    /// <summary>The runs' lengths added up — duplicates included; <see cref="DuplicateOverlapMm"/> says how much of it is drawn twice.</summary>
    public double LengthMm => Runs.Sum(r => r.LengthMm);

    public double DuplicateOverlapMm { get; init; }
    public Box BoundsMm => Box.Of(Runs.Select(r => r.Shape.Bounds).Concat(Nodes.Select(n => n.Shape.Bounds)).SelectMany(b => new[] { b.Min, b.Max }));
}

/// <summary>Two runs of one system drawn over each other along a shared stretch (collinear, or a copy a few mm off-axis); handles in ordinal order.</summary>
public sealed record MepDuplicate(string Handle, string OtherHandle, Pt FromMm, Pt ToMm, double OverlapMm);

/// <summary>What the network builder found; <see cref="DoubleLineDucts"/> counts duct runs that look like one side of a double-line duct.</summary>
public sealed record MepOutcome(IReadOnlyList<MepNetwork> Networks, IReadOnlyList<MepEndpoint> Endpoints, IReadOnlyList<MepNode> OrphanNodes, IReadOnlyList<MepDuplicate> Duplicates, int Crossings, int Runs, int Nodes, int DoubleLineDucts);
