namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Specifies plot page orientation mode.
/// </summary>
public enum OrientationMode
{
    /// <summary>Automatically detect Portrait or Landscape based on the frame aspect ratio (width vs height).</summary>
    Auto,

    /// <summary>Force Portrait orientation.</summary>
    Portrait,

    /// <summary>Force Landscape orientation.</summary>
    Landscape
}
