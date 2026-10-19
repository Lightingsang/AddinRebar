namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///     Encapsulates unified collinear alignment rules per structural gridline/axis.
///     Ensures all beam spans along a single continuous axis are 100% collinear with zero jagged stepping.
/// </summary>
public sealed record AxisAlignmentRule(
    int YJustification,
    string YJustificationName,
    double YOffsetMm,
    bool InvertDirection);

public static class Story1BeamAlignmentHelper
{
    public static AxisAlignmentRule ResolveXAxisRule(double gy, List<CadLineItem> matchingLines)
    {
        // Strictly unified Origin (centerline) alignment along the grid/axis.
        // Guarantees 100% mathematical collinearity for all spans and prevents opposite-shift zigzagging.
        return new AxisAlignmentRule(2, "Origin", 0.0, false);
    }

    public static AxisAlignmentRule ResolveYAxisRule(double gx, List<CadLineItem> matchingLines)
    {
        // Strictly unified Origin (centerline) alignment along the grid/axis.
        // Guarantees 100% mathematical collinearity for all spans and prevents opposite-shift zigzagging.
        return new AxisAlignmentRule(2, "Origin", 0.0, false);
    }
}
