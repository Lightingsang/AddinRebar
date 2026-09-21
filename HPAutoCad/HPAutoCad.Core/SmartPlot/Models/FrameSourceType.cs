namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Specifies the mechanism used to detect and extract plot frames from the drawing.
/// </summary>
public enum FrameSourceType
{
    /// <summary>Detect frames by block reference insertion (including dynamic blocks and attribute evaluation).</summary>
    Block,

    /// <summary>Detect frames by closed polylines residing on a specified CAD layer.</summary>
    Layer,

    /// <summary>Detect frames from PaperSpace Layout tabs according to tab order and range filters.</summary>
    Layout
}
