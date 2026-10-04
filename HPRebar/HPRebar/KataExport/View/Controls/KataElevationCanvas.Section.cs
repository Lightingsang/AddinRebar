using System.Windows;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// The section panel over Kata's elevation: a floating column down the right of the canvas, opened only by a click on a
/// section flag and closed by its ✕ or Esc; picking a column leaves it as it is. It zooms and pans on its own
/// (<see cref="KataSectionView"/>); the elevation keeps the whole width of the canvas.
/// </summary>
public sealed partial class KataElevationCanvas
{
    /// <summary>
    /// Width of the floating panel (it runs the canvas's whole height), its gap to the canvas edges, and the canvas size
    /// under which it is not shown.
    /// </summary>
    private const double SectionPanelWidth = 350.0;

    private const double SectionPanelGap = 8.0;
    private const double SectionPanelMinWidth = 240.0;
    private const double SectionPanelMinHeight = 200.0;

    /// <summary>The ✕ in the panel's top right corner.</summary>
    private const double CloseButtonSize = 18.0;

    private const double CloseButtonInset = 4.0;

    // The section of the flag clicked last and where it cuts; null: no panel. It stays open while the plan is edited
    // (bars removed) as long as the drawing still cuts there, whatever number that cut now has.
    private int? _flagSection;
    private double _flagX;
    private KataSectionView _sectionView = KataSectionView.Fitted;

    /// <summary>Whether a section was opened by a flag and the drawing shown still has it.</summary>
    private bool SectionOpen => Drawing() is { } drawing && ShownCut(drawing) is not null;

    /// <summary>Where the section panel sits, null when it is not shown.</summary>
    private Rect? SectionPanel()
    {
        if (!ShowSection || !KataMode || !SectionOpen || ActualWidth < SectionPanelMinWidth || ActualHeight < SectionPanelMinHeight) return null;
        double width = Math.Min(SectionPanelWidth, ActualWidth - 2.0 * SectionPanelGap);
        double height = ActualHeight - 2.0 * SectionPanelGap;
        return new Rect(ActualWidth - SectionPanelGap - width, SectionPanelGap, width, height);
    }

    /// <summary>The ✕ that closes <paramref name="panel"/>.</summary>
    private static Rect CloseButton(Rect panel) =>
        new(panel.Right - CloseButtonInset - CloseButtonSize, panel.Top + CloseButtonInset, CloseButtonSize, CloseButtonSize);

    /// <summary>The section shown: the one of the flag clicked.</summary>
    private KataSectionCut? ShownCut(KataRebarDrawing drawing)
    {
        if (_flagSection is not { } number) return null;
        return drawing.Cuts.FirstOrDefault(c => Math.Abs(c.X - _flagX) < 1.0) ?? drawing.Cuts.FirstOrDefault(c => c.Number == number);
    }

    private void OpenSection(KataDrawingFlag flag)
    {
        if (_flagSection != flag.Number) _sectionView = KataSectionView.Fitted;
        _flagSection = flag.Number;
        _flagX = flag.X;
        InvalidateVisual();
    }

    /// <summary>A section that could not be drawn (logged once) still shows its panel, a line saying so and its close button.</summary>
    private void PaintSectionFailure(KataDrawPrimitives draw, KataCanvasPalette palette, Rect panel, int number)
    {
        draw.Box(palette.Fill, KataCadPens.Lineweight(palette.KataGrey, KataCadPens.ThinPx, draw.PixelsPerDip, 1.0), panel.Left, panel.Top, panel.Right, panel.Bottom);
        var text = draw.Text($"Không vẽ được mặt cắt {number}-{number} (xem log).", palette.MutedText, KataDrawPrimitives.SmallTextSize);
        draw.AtWidth(text, panel.Left + 12.0, panel.Top + 32.0, panel.Width - 24.0);
    }

    private void CloseSection()
    {
        _flagSection = null;
        _sectionView = KataSectionView.Fitted;
        InvalidateVisual();
    }

    /// <summary>The ✕ over the panel, drawn after the section so the drawing never hides it.</summary>
    private void PaintCloseButton(KataDrawPrimitives draw, KataCanvasPalette palette, Rect panel)
    {
        var box = CloseButton(panel);
        var pen = KataCadPens.Lineweight(palette.Text, KataCadPens.OutlinePx, draw.PixelsPerDip, 1.0);
        draw.Box(palette.Fill, null, box.Left, box.Top, box.Right, box.Bottom);
        const double inset = 5.0;
        draw.Line(pen, box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset);
        draw.Line(pen, box.Left + inset, box.Bottom - inset, box.Right - inset, box.Top + inset);
    }

    /// <summary>The section flag under <paramref name="at"/>, null when none is.</summary>
    private KataDrawingFlag? FlagAt(KataElevation elevation, KataElevationViewport viewport, Point at)
    {
        if (!ShowSection || !KataMode || Drawing() is not { } drawing) return null;
        var scene = Scene(elevation, viewport);
        var map = RebarStationMap ?? KataStationMap.Identity;
        Point P(double x, double z) => new(scene.X(map.ToStation(x)), scene.Y(elevation.TopMm + z));
        foreach (var flag in drawing.Elevation.Flags)
            if (KataElevationCadPainter.FlagHit(flag, at, P, viewport.Scale)) return flag;
        return null;
    }
}
