namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Full parameter configuration for a plot operation or session.
/// </summary>
public sealed record PlotConfiguration
{
    public string DeviceName { get; init; } = "AutoCAD PDF (General Documentation).pc3";
    public string MediaName { get; init; } = "ISO_full_bleed_A1_(841.00_x_594.00_MM)";
    public string PlotStyle { get; init; } = "monochrome.ctb";
    public OrientationMode Orientation { get; init; } = OrientationMode.Auto;
    public OutputMode OutputMode { get; init; } = OutputMode.SingleFiles;
    public string OutputFolder { get; init; } = string.Empty;
    public string FileNamePattern { get; init; } = "{Prefix}_{Layout}_{SheetNo}_{Title}";
    public string FileNamePrefix { get; init; } = "HP";
    public string MergedFileName { get; init; } = "MergedPlot.pdf";
    public FrameSourceType FrameSource { get; init; } = FrameSourceType.Block;
    public string BlockName { get; init; } = string.Empty;
    public string SheetNumberAttribute { get; init; } = "SOHIEU";
    public string SheetTitleAttribute { get; init; } = "TENTIEUDE";
    public string LayerName { get; init; } = string.Empty;
    public string LayoutRange { get; init; } = "All";
    public bool CenterPlot { get; init; } = true;
    public bool FitToPaper { get; init; } = true;
    public double CustomScaleNumerator { get; init; } = 1.0;
    public double CustomScaleDenominator { get; init; } = 1.0;
    public double ToleranceBandYRatio { get; init; } = 0.5;
}
