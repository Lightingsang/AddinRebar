using System;
using System.Collections.Generic;
using System.Linq;
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
/// Rendering engine for the continuous beam elevation view.
/// Coordinates with <see cref="BeamCanvasTransformCalculator"/> in HPRebar.Core for aspect-preserving projection.
/// </summary>
internal sealed class BeamElevationPainter
{
    private readonly CanvasPalette _palette;
    private readonly BeamCanvasTransform _transform;
    private readonly BeamRebarSession _session;

    public BeamElevationPainter(CanvasPalette palette, BeamCanvasTransform transform, BeamRebarSession session)
    {
        _palette = palette;
        _transform = transform;
        _session = session;
    }

    public void Paint(DrawingContext dc)
    {
        var stack = _session.Stack;
        if (stack.Spans.Count == 0) return;

        PaintSupports(dc);
        PaintContinuousConcreteOutlines(dc);
        PaintStirrups(dc);
        PaintMainLongitudinalBars(dc);
        PaintAdditionalTopBars(dc);
        PaintAdditionalBottomBars(dc);
        PaintSideSkinBars(dc);
        PaintSecondaryHangingStirrups(dc);
        PaintDimensions(dc);
    }

    private void PaintSupports(DrawingContext dc)
    {
        var stack = _session.Stack;
        for (int i = 0; i < stack.Supports.Count; i++)
        {
            var node = stack.Supports[i];
            double xLeft = _transform.ToScreenX(node.LeftFaceX);
            double xRight = _transform.ToScreenX(node.RightFaceX);
            double width = Math.Max(8.0, xRight - xLeft);

            // Determine local beam top and bottom elevations
            double zBot = stack.Spans.Where(s => Math.Abs(s.StartX - node.CenterX) < node.Width || Math.Abs(s.EndX - node.CenterX) < node.Width)
                                     .Select(s => s.BottomElevation)
                                     .DefaultIfEmpty(stack.Spans[0].BottomElevation)
                                     .Min();

            double zTop = stack.Spans.Where(s => Math.Abs(s.StartX - node.CenterX) < node.Width || Math.Abs(s.EndX - node.CenterX) < node.Width)
                                     .Select(s => s.TopElevation)
                                     .DefaultIfEmpty(stack.Spans[0].TopElevation)
                                     .Max();

            double yBot = _transform.ToScreenY(zBot);
            double yTop = _transform.ToScreenY(zTop);

            // Draw lower column / wall stub
            BeamDrawPrimitives.FilledBox(dc, _palette.SupportFill, xLeft, yBot, width, 36.0);
            BeamDrawPrimitives.Box(dc, _palette.Outline, xLeft, yBot, width, 36.0);

            // Draw upper column stub
            BeamDrawPrimitives.FilledBox(dc, _palette.SupportFill, xLeft, yTop - 26.0, width, 26.0);
            BeamDrawPrimitives.Box(dc, _palette.Outline, xLeft, yTop - 26.0, width, 26.0);

            // Support Centerline
            double xCenter = _transform.ToScreenX(node.CenterX);
            var dashedPen = _palette.DashedDimension;
            BeamDrawPrimitives.Line(dc, dashedPen, xCenter, yTop - 32.0, xCenter, yBot + 42.0);

            // Support Name / Label
            string label = !string.IsNullOrEmpty(node.Name) ? node.Name : $"S{i + 1}";
            BeamDrawPrimitives.Caption(dc, _palette.Text, label, xCenter, yBot + 38.0, center: true);
        }
    }

    private void PaintContinuousConcreteOutlines(DrawingContext dc)
    {
        var stack = _session.Stack;
        for (int i = 0; i < stack.Spans.Count; i++)
        {
            var span = stack.Spans[i];
            double xStart = _transform.ToScreenX(span.StartX);
            double xEnd = _transform.ToScreenX(span.EndX);
            double yTop = _transform.ToScreenY(span.TopElevation);
            double yBot = _transform.ToScreenY(span.BottomElevation);
            double w = xEnd - xStart;
            double h = yBot - yTop;

            // Highlight selected span
            if (i == _session.SelectedSpanIndex)
            {
                BeamDrawPrimitives.FilledBox(dc, _palette.Highlight, xStart, yTop, w, h);
            }

            // Top and bottom horizontal edges
            BeamDrawPrimitives.Line(dc, _palette.Outline, xStart, yTop, xEnd, yTop);
            BeamDrawPrimitives.Line(dc, _palette.Outline, xStart, yBot, xEnd, yBot);

            // Vertical outer boundaries
            if (i == 0)
            {
                BeamDrawPrimitives.Line(dc, _palette.Outline, xStart, yTop, xStart, yBot);
            }

            if (i == stack.Spans.Count - 1)
            {
                BeamDrawPrimitives.Line(dc, _palette.Outline, xEnd, yTop, xEnd, yBot);
            }

            // Step transition at interior supports
            if (i > 0)
            {
                var prev = stack.Spans[i - 1];
                double prevYTop = _transform.ToScreenY(prev.TopElevation);
                double prevYBot = _transform.ToScreenY(prev.BottomElevation);

                if (Math.Abs(prevYTop - yTop) > 0.5)
                {
                    BeamDrawPrimitives.Line(dc, _palette.Outline, xStart, Math.Min(prevYTop, yTop), xStart, Math.Max(prevYTop, yTop));
                }

                if (Math.Abs(prevYBot - yBot) > 0.5)
                {
                    BeamDrawPrimitives.Line(dc, _palette.Outline, xStart, Math.Min(prevYBot, yBot), xStart, Math.Max(prevYBot, yBot));
                }
            }
        }
    }

    private void PaintStirrups(DrawingContext dc)
    {
        var stack = _session.Stack;
        double coverMm = _session.Cover;
        double stirrupDMm = _session.StirrupBarType?.DiameterMm ?? 8.0;

        for (int i = 0; i < stack.Spans.Count; i++)
        {
            var span = stack.Spans[i];
            double leftFaceX = span.StartX;
            double rightFaceX = span.EndX;
            double ln = span.LengthClear;
            if (ln <= 0) continue;

            double zTop = span.TopElevation - coverMm;
            double zBot = span.BottomElevation + coverMm;
            double yTop = _transform.ToScreenY(zTop);
            double yBot = _transform.ToScreenY(zBot);

            // Calculate zone boundary positions
            double s1 = _session.StirrupSpacingDense;
            double s2 = _session.StirrupSpacingSparse;
            var layout = _session.StirrupLayout;

            double z1End, z2End;
            if (layout == StirrupLayout.ThreeZoneL3)
            {
                z1End = leftFaceX + (ln / 3.0);
                z2End = leftFaceX + (2.0 * ln / 3.0);
            }
            else if (layout == StirrupLayout.ThreeZoneL4)
            {
                z1End = leftFaceX + (ln / 4.0);
                z2End = leftFaceX + (3.0 * ln / 4.0);
            }
            else // Uniform
            {
                z1End = rightFaceX;
                z2End = rightFaceX;
                s2 = s1;
            }

            // Render sample stirrup vertical tick lines (capped for high performance)
            double stepPx = Math.Max(4.0, s1 * _transform.Scale);
            double xCur = leftFaceX + _session.StirrupStartOffset;

            while (xCur < rightFaceX)
            {
                double spacing = (xCur < z1End || xCur > z2End) ? s1 : s2;
                double screenX = _transform.ToScreenX(xCur);

                BeamDrawPrimitives.Line(dc, _palette.Stirrup, screenX, yTop, screenX, yBot);

                double advanceMm = Math.Max(spacing, 12.0 / _transform.Scale); // Min pixel density threshold
                xCur += advanceMm;
            }
        }
    }

    private void PaintMainLongitudinalBars(DrawingContext dc)
    {
        var stack = _session.Stack;
        double coverMm = _session.Cover;
        double stirrupDMm = _session.StirrupBarType?.DiameterMm ?? 8.0;
        double topDMm = _session.TopBarType?.DiameterMm ?? 20.0;
        double botDMm = _session.BottomBarType?.DiameterMm ?? 20.0;

        double overallStartX = stack.ContinuousStack.OverallStartX;
        double overallEndX = stack.ContinuousStack.OverallEndX;

        // Top Continuous Bars
        double xStart = _transform.ToScreenX(overallStartX + coverMm);
        double xEnd = _transform.ToScreenX(overallEndX - coverMm);

        double zTop = stack.Spans.Max(s => s.TopElevation) - coverMm - stirrupDMm - (topDMm / 2.0);
        double yTopBar = _transform.ToScreenY(zTop);

        // Continuous horizontal top bar
        BeamDrawPrimitives.Line(dc, _palette.MainBar, xStart, yTopBar, xEnd, yTopBar);

        // Exterior Start Anchorage (Hook 90 Down)
        if (_session.TopStartAnchorage == EndAnchorageType.Hook90Down)
        {
            double hookLenPx = Math.Min(30.0, 400.0 * _transform.Scale);
            BeamDrawPrimitives.Line(dc, _palette.MainBar, xStart, yTopBar, xStart, yTopBar + hookLenPx);
        }

        // Exterior End Anchorage (Hook 90 Down)
        if (_session.TopEndAnchorage == EndAnchorageType.Hook90Down)
        {
            double hookLenPx = Math.Min(30.0, 400.0 * _transform.Scale);
            BeamDrawPrimitives.Line(dc, _palette.MainBar, xEnd, yTopBar, xEnd, yTopBar + hookLenPx);
        }

        // Bottom Continuous Bars
        double zBot = stack.Spans.Min(s => s.BottomElevation) + coverMm + stirrupDMm + (botDMm / 2.0);
        double yBotBar = _transform.ToScreenY(zBot);

        // Continuous horizontal bottom bar
        BeamDrawPrimitives.Line(dc, _palette.MainBar, xStart, yBotBar, xEnd, yBotBar);

        // Exterior Start Anchorage (Hook 90 Up)
        if (_session.BottomStartAnchorage == EndAnchorageType.Hook90Up)
        {
            double hookLenPx = Math.Min(30.0, 400.0 * _transform.Scale);
            BeamDrawPrimitives.Line(dc, _palette.MainBar, xStart, yBotBar, xStart, yBotBar - hookLenPx);
        }

        // Exterior End Anchorage (Hook 90 Up)
        if (_session.BottomEndAnchorage == EndAnchorageType.Hook90Up)
        {
            double hookLenPx = Math.Min(30.0, 400.0 * _transform.Scale);
            BeamDrawPrimitives.Line(dc, _palette.MainBar, xEnd, yBotBar, xEnd, yBotBar - hookLenPx);
        }

        // Lap splice indication if total length > 11.7m
        if (stack.ContinuousStack.TotalLength > _session.MaxStockLength && stack.Spans.Count > 1)
        {
            var midSpan = stack.Spans[stack.Spans.Count / 2];
            double midX = (midSpan.StartX + midSpan.EndX) / 2.0;
            double lapScreenX = _transform.ToScreenX(midX);
            double lapWidthPx = Math.Max(12.0, (_session.LapFactor * topDMm) * _transform.Scale);
            double beamHeightPx = Math.Max(2.0, midSpan.Height * _transform.Scale);
            double lapOffset = Math.Min(3.0, beamHeightPx * 0.15);

            // Staggered top lap indicator
            BeamDrawPrimitives.Line(dc, _palette.SelectedMainBar, lapScreenX - (lapWidthPx / 2.0), yTopBar - lapOffset, lapScreenX + (lapWidthPx / 2.0), yTopBar - lapOffset);
        }
    }

    private void PaintAdditionalTopBars(DrawingContext dc)
    {
        var stack = _session.Stack;
        double coverMm = _session.Cover;
        double stirrupDMm = _session.StirrupBarType?.DiameterMm ?? 8.0;
        double topDMm = _session.TopBarType?.DiameterMm ?? 20.0;

        for (int i = 0; i < _session.SupportTopBars.Count && i < stack.Supports.Count; i++)
        {
            var editor = _session.SupportTopBars[i];
            if (editor.Layer1Count <= 0) continue;

            var node = stack.Supports[i];
            double xCenter = node.CenterX;

            // Compute adjacent clear span extensions
            double leftLn = i > 0 ? stack.Spans[i - 1].LengthClear : 0.0;
            double rightLn = i < stack.Spans.Count ? stack.Spans[i].LengthClear : 0.0;

            double extLeft = leftLn * editor.Layer1ExtensionRatio;
            double extRight = rightLn * editor.Layer1ExtensionRatio;

            double xLeft = i == 0 ? node.CenterX : node.LeftFaceX - extLeft;
            double xRight = i == stack.Supports.Count - 1 ? node.CenterX : node.RightFaceX + extRight;

            double zTop = stack.Spans[Math.Min(i, stack.Spans.Count - 1)].TopElevation - coverMm - stirrupDMm - (topDMm / 2.0);
            var spanForHeight = stack.Spans[Math.Min(i, stack.Spans.Count - 1)];
            double beamHeightPx = Math.Max(2.0, spanForHeight.Height * _transform.Scale);
            double layerOffset = Math.Min(3.0, beamHeightPx * 0.15);
            double layerGap = Math.Min(5.0, beamHeightPx * 0.20);
            double yLayer1 = _transform.ToScreenY(zTop) + layerOffset; // Dynamically scaled offset below main bar

            double sXLeft = _transform.ToScreenX(xLeft);
            double sXRight = _transform.ToScreenX(xRight);

            // Draw Layer 1
            BeamDrawPrimitives.Line(dc, _palette.AddTopBar, sXLeft, yLayer1, sXRight, yLayer1);

            // Draw Layer 2 (if enabled)
            if (editor.EnableLayer2 && editor.Layer2Count > 0)
            {
                double extLeft2 = leftLn * editor.Layer2ExtensionRatio;
                double extRight2 = rightLn * editor.Layer2ExtensionRatio;
                double xLeft2 = i == 0 ? node.CenterX : node.LeftFaceX - extLeft2;
                double xRight2 = i == stack.Supports.Count - 1 ? node.CenterX : node.RightFaceX + extRight2;

                double yLayer2 = yLayer1 + layerGap; // Dynamically scaled gap
                BeamDrawPrimitives.Line(dc, _palette.AddTopBar, _transform.ToScreenX(xLeft2), yLayer2, _transform.ToScreenX(xRight2), yLayer2);
            }
        }
    }

    private void PaintAdditionalBottomBars(DrawingContext dc)
    {
        var stack = _session.Stack;
        double coverMm = _session.Cover;
        double stirrupDMm = _session.StirrupBarType?.DiameterMm ?? 8.0;
        double botDMm = _session.BottomBarType?.DiameterMm ?? 20.0;

        for (int i = 0; i < _session.SpanBottomBars.Count && i < stack.Spans.Count; i++)
        {
            var editor = _session.SpanBottomBars[i];
            if (editor.Layer1Count <= 0) continue;

            var span = stack.Spans[i];
            double leftFaceX = span.StartX;
            double rightFaceX = span.EndX;
            double ln = span.LengthClear;
            if (ln <= 0) continue;

            double cutoffMm = ln * editor.CutoffRatio;
            double xStart = leftFaceX + cutoffMm;
            double xEnd = rightFaceX - cutoffMm;

            double zBot = span.BottomElevation + coverMm + stirrupDMm + (botDMm / 2.0);
            double beamHeightPx = Math.Max(2.0, span.Height * _transform.Scale);
            double layerOffset = Math.Min(3.0, beamHeightPx * 0.15);
            double layerGap = Math.Min(5.0, beamHeightPx * 0.20);
            double yLayer1 = _transform.ToScreenY(zBot) - layerOffset; // Dynamically scaled offset above main bar

            double sXStart = _transform.ToScreenX(xStart);
            double sXEnd = _transform.ToScreenX(xEnd);

            // Draw Layer 1
            BeamDrawPrimitives.Line(dc, _palette.AddBottomBar, sXStart, yLayer1, sXEnd, yLayer1);

            // Draw Layer 2 (if enabled)
            if (editor.EnableLayer2 && editor.Layer2Count > 0)
            {
                double yLayer2 = yLayer1 - layerGap;
                BeamDrawPrimitives.Line(dc, _palette.AddBottomBar, sXStart, yLayer2, sXEnd, yLayer2);
            }
        }
    }

    private void PaintSideSkinBars(DrawingContext dc)
    {
        if (!_session.AutoSkinBars) return;

        var stack = _session.Stack;
        for (int i = 0; i < stack.Spans.Count; i++)
        {
            var span = stack.Spans[i];
            if (span.Height < _session.DepthThreshold) continue;

            double xStart = _transform.ToScreenX(span.StartX);
            double xEnd = _transform.ToScreenX(span.EndX);
            double yTop = _transform.ToScreenY(span.TopElevation);
            double yBot = _transform.ToScreenY(span.BottomElevation);

            // Draw intermediate longitudinal side bar lines
            double midY = (yTop + yBot) / 2.0;
            var dashedPen = _palette.DashedSideBar;
            BeamDrawPrimitives.Line(dc, dashedPen, xStart, midY, xEnd, midY);
        }
    }

    private void PaintSecondaryHangingStirrups(DrawingContext dc)
    {
        if (!_session.EnableHangingStirrups) return;

        var stack = _session.Stack;
        if (stack.SecondaryIntersections.Count == 0) return;

        foreach (var inter in stack.SecondaryIntersections)
        {
            double sX = _transform.ToScreenX(inter.CenterX);
            double yTop = _transform.ToScreenY(stack.Spans[0].TopElevation);
            double yBot = _transform.ToScreenY(stack.Spans[0].BottomElevation);

            // Hanging stirrup zone tick markers
            BeamDrawPrimitives.Line(dc, _palette.SelectedMainBar, sX - 6.0, yTop, sX - 6.0, yBot);
            BeamDrawPrimitives.Line(dc, _palette.SelectedMainBar, sX, yTop, sX, yBot);
            BeamDrawPrimitives.Line(dc, _palette.SelectedMainBar, sX + 6.0, yTop, sX + 6.0, yBot);
        }
    }

    private void PaintDimensions(DrawingContext dc)
    {
        var stack = _session.Stack;
        double zMin = stack.Spans.Min(s => s.BottomElevation);
        double yBaseDim = _transform.ToScreenY(zMin) + 20.0;

        // Clear Span (Ln) Dimensions
        for (int i = 0; i < stack.Spans.Count; i++)
        {
            var span = stack.Spans[i];
            double leftFaceX = span.StartX;
            double rightFaceX = span.EndX;

            double sLeft = _transform.ToScreenX(leftFaceX);
            double sRight = _transform.ToScreenX(rightFaceX);
            double w = sRight - sLeft;

            BeamDrawPrimitives.DimensionHorizontal(dc, _palette.Dimension, _palette.Text, sLeft, yBaseDim, w, span.LengthClear, "Ln=");
        }

        // Overall Continuous Length (Ltotal) Dimension
        double sTotalStart = _transform.ToScreenX(stack.ContinuousStack.OverallStartX);
        double sTotalEnd = _transform.ToScreenX(stack.ContinuousStack.OverallEndX);
        double yTotalDim = yBaseDim + 22.0;

        BeamDrawPrimitives.DimensionHorizontal(dc, _palette.Dimension, _palette.Text, sTotalStart, yTotalDim, sTotalEnd - sTotalStart, stack.ContinuousStack.TotalLength, "L=");
    }
}
