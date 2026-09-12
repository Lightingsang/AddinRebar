using System;
using System.Windows.Media;
using HPRebar.BeamRebar.ViewModel;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;

// Alias WPF types against Revit SDK implicit usings
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Brush = System.Windows.Media.Brush;
using FormattedText = System.Windows.Media.FormattedText;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// Rendering engine for the beam cross-section cut view.
/// Displays outer concrete section, closed stirrup hoop, top/bottom longitudinal layers, side skin bars, and cross-ties.
/// </summary>
internal sealed class BeamSectionPainter
{
    private readonly CanvasPalette _palette;
    private readonly BeamCanvasTransform _transform;
    private readonly BeamRebarSession _session;
    private readonly BeamSpan _span;
    private readonly bool _isSupportSection;

    public BeamSectionPainter(
        CanvasPalette palette,
        BeamCanvasTransform transform,
        BeamRebarSession session,
        BeamSpan span,
        bool isSupportSection = false)
    {
        _palette = palette;
        _transform = transform;
        _session = session;
        _span = span;
        _isSupportSection = isSupportSection;
    }

    public void Paint(DrawingContext dc)
    {
        double b = _span.Width;
        double h = _span.Height;
        double coverMm = _session.Cover;
        double stirrupDMm = _session.StirrupBarType?.DiameterMm ?? 8.0;

        double xLeft = _transform.ToScreenX(-b / 2.0);
        double xRight = _transform.ToScreenX(b / 2.0);
        double yTop = _transform.ToScreenY(h);
        double yBot = _transform.ToScreenY(0.0);

        double wBox = xRight - xLeft;
        double hBox = yBot - yTop;

        // 1. Concrete Outline
        BeamDrawPrimitives.Box(dc, _palette.Outline, xLeft, yTop, wBox, hBox);

        // 2. Closed Outer Stirrup
        double coverPx = coverMm * _transform.Scale;
        double stirrupLeft = xLeft + coverPx;
        double stirrupRight = xRight - coverPx;
        double stirrupTop = yTop + coverPx;
        double stirrupBot = yBot - coverPx;

        double sWidth = Math.Max(4.0, stirrupRight - stirrupLeft);
        double sHeight = Math.Max(4.0, stirrupBot - stirrupTop);
        BeamDrawPrimitives.Box(dc, _palette.Stirrup, stirrupLeft, stirrupTop, sWidth, sHeight);

        // 3. Main Top Bars
        double topD = _session.TopBarType?.DiameterMm ?? 20.0;
        double topRadiusPx = Math.Max(2.5, (topD / 2.0) * _transform.Scale);
        double topBarY = stirrupTop + (stirrupDMm * _transform.Scale) + topRadiusPx;

        int topCount = Math.Max(2, _session.TopBarCount);
        double topStartX = stirrupLeft + (stirrupDMm * _transform.Scale) + topRadiusPx;
        double topEndX = stirrupRight - (stirrupDMm * _transform.Scale) - topRadiusPx;
        if (topEndX < topStartX)
        {
            topStartX = (stirrupLeft + stirrupRight) / 2.0;
            topEndX = topStartX;
        }
        double topStep = topCount > 1 ? (topEndX - topStartX) / (topCount - 1) : 0;

        for (int i = 0; i < topCount; i++)
        {
            double barX = topStartX + (i * topStep);
            BeamDrawPrimitives.Circle(dc, _palette.MainBar.Brush, null, barX, topBarY, topRadiusPx);
        }

        // Additional Top Bars (if at support)
        if (_isSupportSection && _session.SelectedSupportEditor is { } suppEditor && suppEditor.EnableLayer2)
        {
            int layer2Count = Math.Max(1, suppEditor.Layer2Count);
            double layer2Y = topBarY + (suppEditor.LayerGap * _transform.Scale);
            double layer2Step = layer2Count > 1 ? (topEndX - topStartX) / (layer2Count - 1) : 0;

            for (int i = 0; i < layer2Count; i++)
            {
                double barX = layer2Count == 1 ? (topStartX + topEndX) / 2.0 : topStartX + (i * layer2Step);
                BeamDrawPrimitives.Circle(dc, _palette.AddTopBar.Brush, null, barX, layer2Y, topRadiusPx * 0.9);
            }
        }

        // 4. Main Bottom Bars
        double botD = _session.BottomBarType?.DiameterMm ?? 20.0;
        double botRadiusPx = Math.Max(2.5, (botD / 2.0) * _transform.Scale);
        double botBarY = stirrupBot - (stirrupDMm * _transform.Scale) - botRadiusPx;

        int botCount = Math.Max(2, _session.BottomBarCount);
        double botStartX = stirrupLeft + (stirrupDMm * _transform.Scale) + botRadiusPx;
        double botEndX = stirrupRight - (stirrupDMm * _transform.Scale) - botRadiusPx;
        if (botEndX < botStartX)
        {
            botStartX = (stirrupLeft + stirrupRight) / 2.0;
            botEndX = botStartX;
        }
        double botStep = botCount > 1 ? (botEndX - botStartX) / (botCount - 1) : 0;

        for (int i = 0; i < botCount; i++)
        {
            double barX = botStartX + (i * botStep);
            BeamDrawPrimitives.Circle(dc, _palette.MainBar.Brush, null, barX, botBarY, botRadiusPx);
        }

        // Additional Bottom Bars (if at midspan)
        if (!_isSupportSection && _session.SelectedSpanEditor is { } spanEditor && spanEditor.EnableLayer2)
        {
            int layer2Count = Math.Max(1, spanEditor.Layer2Count);
            double layer2Y = botBarY - (spanEditor.LayerGap * _transform.Scale);
            double layer2Step = layer2Count > 1 ? (botEndX - botStartX) / (layer2Count - 1) : 0;

            for (int i = 0; i < layer2Count; i++)
            {
                double barX = layer2Count == 1 ? (botStartX + botEndX) / 2.0 : botStartX + (i * layer2Step);
                BeamDrawPrimitives.Circle(dc, _palette.AddBottomBar.Brush, null, barX, layer2Y, botRadiusPx * 0.9);
            }
        }

        // 5. Side Skin Bars & Cross-Ties (Deep Beams)
        if (_session.AutoSkinBars && h >= _session.DepthThreshold)
        {
            double sideD = _session.SideBarType?.DiameterMm ?? 12.0;
            double sideRadiusPx = Math.Max(2.0, (sideD / 2.0) * _transform.Scale);
            double innerH = (botBarY - topBarY);
            int sidePairs = (int)Math.Floor(h / Math.Max(100.0, _session.MaxVerticalSpacing));

            if (sidePairs > 0)
            {
                double vStep = innerH / (sidePairs + 1);
                double sideLeftX = stirrupLeft + (stirrupDMm * _transform.Scale) + sideRadiusPx;
                double sideRightX = stirrupRight - (stirrupDMm * _transform.Scale) - sideRadiusPx;

                for (int i = 1; i <= sidePairs; i++)
                {
                    double ySide = topBarY + (i * vStep);
                    BeamDrawPrimitives.Circle(dc, _palette.SideBar.Brush, null, sideLeftX, ySide, sideRadiusPx);
                    BeamDrawPrimitives.Circle(dc, _palette.SideBar.Brush, null, sideRightX, ySide, sideRadiusPx);

                    // Cross-Tie connecting opposite bars
                    if (_session.IncludeCrossTies)
                    {
                        BeamDrawPrimitives.Line(dc, _palette.SideBar, sideLeftX + sideRadiusPx, ySide, sideRightX - sideRadiusPx, ySide);
                    }
                }
            }
        }

        // 6. Section Dimensions (b & h)
        BeamDrawPrimitives.DimensionHorizontal(dc, _palette.Dimension, _palette.Text, xLeft, yBot + 14.0, wBox, b, "b=");
        BeamDrawPrimitives.DimensionVertical(dc, _palette.Dimension, _palette.Text, xLeft - 14.0, yTop, hBox, h, "h=");
    }
}
