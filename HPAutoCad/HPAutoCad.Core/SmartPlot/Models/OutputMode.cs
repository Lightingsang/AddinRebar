namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Specifies how plotted PDF pages are output.
/// </summary>
public enum OutputMode
{
    /// <summary>Each frame is exported to an individual PDF file.</summary>
    SingleFiles,

    /// <summary>All plotted frames are merged into a single multi-page PDF document.</summary>
    MergedPdf
}
