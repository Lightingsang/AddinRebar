using HPAutoCad.SmartPlot.UI;

namespace HPAutoCad.SmartPlot.Commands;

/// <summary>
/// Entry point command invoked by the AutoCAD loader delegate ["smartplot"]
/// to display the modeless Smart Plot Pro window.
/// </summary>
public static class SmartPlotCommand
{
    public static void Run()
    {
        SmartPlotWindow.ShowWindow();
    }
}
