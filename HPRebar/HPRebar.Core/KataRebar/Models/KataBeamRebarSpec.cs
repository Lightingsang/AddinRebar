using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Master domain model containing full structural reinforcement and geometric specification
/// parsed from sheet 'Dam' of Kata.xlsm.
/// </summary>
public sealed record KataBeamRebarSpec
{
    /// <summary>Beam identifier or mark (sheet Dam cell B3, e.g. "B01").</summary>
    public string BeamName { get; init; } = "";

    /// <summary>Number of identical beam instances (sheet Dam cell B4, default 1).</summary>
    public int BeamCount { get; init; } = 1;

    /// <summary>Nominal beam cross-section width b in mm (sheet Dam cell B6).</summary>
    public double Width { get; init; }

    /// <summary>Nominal beam cross-section height h in mm (sheet Dam cell B5).</summary>
    public double Height { get; init; }

    /// <summary>Adjacent slab thickness hs in mm (sheet Dam cell B7).</summary>
    public double SlabThickness { get; init; }

    /// <summary>Name of grid line running along beam longitudinal axis (sheet Dam cell B8).</summary>
    public string AxisGridName { get; init; } = "";

    /// <summary>Offset from beam centerline to longitudinal grid line in mm (sheet Dam cell B9).</summary>
    public double AxisOffset { get; init; }

    /// <summary>Reference floor elevation string (sheet Dam cell B10, e.g. "+3.300").</summary>
    public string LevelElevation { get; init; } = "";

    /// <summary>Tension anchorage and lap length multiplier in bar diameters (sheet Dam cell G2, default 40d).</summary>
    public double TensionLapMultiplier { get; init; } = 40.0;

    /// <summary>Compression anchorage length multiplier in bar diameters (sheet Dam cell G3, default 30d).</summary>
    public double CompressionLapMultiplier { get; init; } = 30.0;

    /// <summary>Cutoff extension ratio for top extra bars - Layer 1 (sheet Dam cell H5, default 0.25 = L/4).</summary>
    public double TopCutoffRatioLayer1 { get; init; } = 0.25;

    /// <summary>Cutoff extension ratio for top extra bars - Layer 2 (sheet Dam cell H3, default 0.20 = L/5).</summary>
    public double TopCutoffRatioLayer2 { get; init; } = 0.20;

    /// <summary>Origin for measuring Layer 1 cutoff distance (sheet Dam cell I5, default FromColumnFace).</summary>
    public KataCutoffOrigin CutoffOriginLayer1 { get; init; } = KataCutoffOrigin.FromColumnFace;

    /// <summary>Origin for measuring Layer 2 cutoff distance (sheet Dam cell I3, default FromColumnCenter).</summary>
    public KataCutoffOrigin CutoffOriginLayer2 { get; init; } = KataCutoffOrigin.FromColumnCenter;

    /// <summary>
    /// First number of cell J9: distance from the concrete face to the CENTRE of the main bars in mm.
    /// 0 when J9 does not give it; the detailing rules then derive it from the stirrup cover.
    /// </summary>
    public double CoverMain { get; init; }

    /// <summary>
    /// Second number of cell J9: clear cover to the outer face of the stirrups in mm.
    /// 0 when J9 does not give it; the detailing rules then derive it from <see cref="CoverMain"/> or use 25 mm.
    /// </summary>
    public double CoverStirrup { get; init; }

    /// <summary>Top continuous longitudinal bars across all spans (sheet Dam cell B11, e.g. 6f25).</summary>
    public KataBarItem TopContinuous { get; init; } = KataBarItem.Empty;

    /// <summary>Bottom continuous longitudinal bars across all spans (sheet Dam cell B12, e.g. 6f25).</summary>
    public KataBarItem BottomContinuous { get; init; } = KataBarItem.Empty;

    /// <summary>Every bar group written in B11 (a cell such as "2f20;2f16" holds more than one).</summary>
    public IReadOnlyList<KataBarItem> TopMainItems { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Every bar group written in B12.</summary>
    public IReadOnlyList<KataBarItem> BottomMainItems { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>
    /// Detailing cells that have no field of their own (reinforcement stirrup spacing, row 24 markers,
    /// inner stirrups of rows 25-44), kept with their address so unsupported input can be reported.
    /// </summary>
    public IReadOnlyList<KataCellNote> DetailingNotes { get; init; } = Array.Empty<KataCellNote>();

    /// <summary>Global stirrup specification for the beam run (sheet Dam cells G6:G9, I8, rows 25-28).</summary>
    public KataStirrupSpec GlobalStirrup { get; init; } = new();

    /// <summary>Global web skin / side bars configured for deep beams (sheet Dam cells G4, G5).</summary>
    public IReadOnlyList<KataBarItem> GlobalSideBars { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Ordered list of supports along the continuous beam (odd columns C, E, G...).</summary>
    public IReadOnlyList<KataSupportRebarSpec> Supports { get; init; } = Array.Empty<KataSupportRebarSpec>();

    /// <summary>Ordered list of clear spans along the continuous beam (even columns D, F, H...).</summary>
    public IReadOnlyList<KataSpanRebarSpec> Spans { get; init; } = Array.Empty<KataSpanRebarSpec>();

    /// <summary>Calculates total continuous beam length in mm (sum of clear spans + column widths).</summary>
    public double CalculateTotalLengthMm()
    {
        double total = 0.0;
        foreach (var supp in Supports) total += supp.ColumnWidth;
        foreach (var span in Spans) total += span.Length;
        return total;
    }
}
