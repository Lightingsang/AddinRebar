using System;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Immutable transform container mapping between continuous beam local model coordinates (mm)
/// and WPF Canvas pixel coordinates with aspect-ratio preservation and Z-up to Y-down inversion.
/// </summary>
public sealed record BeamCanvasTransform
{
    public double Scale { get; init; }
    public double OffsetX { get; init; }
    public double OffsetY { get; init; }
    public double Baseline { get; init; }
    public double CanvasWidth { get; init; }
    public double CanvasHeight { get; init; }
    public double XMin { get; init; }
    public double ZMin { get; init; }
    public double Margin { get; init; }

    /// <summary>Transforms a point from beam model space (mm) to WPF canvas space (pixels).</summary>
    public (double ScreenX, double ScreenY) ToScreen(double modelX, double modelZ) =>
        (OffsetX + ((modelX - XMin) * Scale), Baseline - ((modelZ - ZMin) * Scale));

    /// <summary>Transforms a point from WPF canvas space (pixels) back to beam model space (mm).</summary>
    public (double ModelX, double ModelZ) ToModel(double screenX, double screenY) =>
        (XMin + ((screenX - OffsetX) / Scale), ZMin + ((Baseline - screenY) / Scale));

    public double ToScreenX(double modelX) => OffsetX + ((modelX - XMin) * Scale);
    public double ToScreenY(double modelZ) => Baseline - ((modelZ - ZMin) * Scale);
    public double ToModelX(double screenX) => XMin + ((screenX - OffsetX) / Scale);
    public double ToModelZ(double screenY) => ZMin + ((Baseline - screenY) / Scale);
}

/// <summary>
/// Calculates coordinate transformations for rendering continuous beam elevations and cross-sections on WPF Canvases.
/// </summary>
public static class BeamCanvasTransformCalculator
{
    public const double DefaultMarginPx = 40.0;

    /// <summary>
    /// Computes uniform scaling and centering transform for beam elevation view.
    /// </summary>
    public static BeamCanvasTransform ComputeElevationTransform(
        double xMinMm,
        double xMaxMm,
        double zMinMm,
        double zMaxMm,
        double canvasWidthPx,
        double canvasHeightPx,
        double marginPx = DefaultMarginPx)
    {
        if (xMaxMm <= xMinMm)
            throw new ArgumentException("Model X max must be strictly greater than X min.");

        if (zMaxMm <= zMinMm)
            throw new ArgumentException("Model Z max must be strictly greater than Z min.");

        if (canvasWidthPx <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(canvasWidthPx), "Canvas width must be strictly positive.");

        if (canvasHeightPx <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(canvasHeightPx), "Canvas height must be strictly positive.");

        if (marginPx < 0.0 || (2.0 * marginPx >= canvasWidthPx) || (2.0 * marginPx >= canvasHeightPx))
            throw new ArgumentOutOfRangeException(nameof(marginPx), "Margin padding cannot exceed canvas dimensions.");

        double wDraw = canvasWidthPx - (2.0 * marginPx);
        double hDraw = canvasHeightPx - (2.0 * marginPx);

        double lModel = xMaxMm - xMinMm;
        double hModel = zMaxMm - zMinMm;

        double scale = Math.Min(wDraw / lModel, hDraw / hModel);

        double wContent = lModel * scale;
        double hContent = hModel * scale;

        double offsetX = marginPx + ((wDraw - wContent) / 2.0);
        double offsetY = marginPx + ((hDraw - hContent) / 2.0);
        double baseline = canvasHeightPx - offsetY;

        return new BeamCanvasTransform
        {
            Scale = scale,
            OffsetX = offsetX,
            OffsetY = offsetY,
            Baseline = baseline,
            CanvasWidth = canvasWidthPx,
            CanvasHeight = canvasHeightPx,
            XMin = xMinMm,
            ZMin = zMinMm,
            Margin = marginPx
        };
    }

    /// <summary>
    /// Computes elevation view transform from a BeamContinuousStack.
    /// </summary>
    public static BeamCanvasTransform ComputeElevationTransform(
        BeamContinuousStack stack,
        double canvasWidthPx,
        double canvasHeightPx,
        double marginPx = DefaultMarginPx)
    {
        if (stack == null || stack.Spans.Count == 0)
            throw new ArgumentException("Beam stack must contain at least one span.");

        double xMin = stack.OverallStartX;
        double xMax = stack.OverallEndX;

        double zMin = stack.Spans[0].BottomElevation;
        double zMax = stack.Spans[0].TopElevation;
        for (int i = 1; i < stack.Spans.Count; i++)
        {
            if (stack.Spans[i].BottomElevation < zMin) zMin = stack.Spans[i].BottomElevation;
            if (stack.Spans[i].TopElevation > zMax) zMax = stack.Spans[i].TopElevation;
        }

        return ComputeElevationTransform(xMin, xMax, zMin, zMax, canvasWidthPx, canvasHeightPx, marginPx);
    }

    /// <summary>
    /// Computes uniform scaling and centering transform for a beam cross-section view.
    /// </summary>
    public static BeamCanvasTransform ComputeSectionTransform(
        double widthMm,
        double heightMm,
        double canvasWidthPx,
        double canvasHeightPx,
        double marginPx = DefaultMarginPx)
    {
        if (widthMm <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(widthMm), "Cross section width must be strictly positive.");

        if (heightMm <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(heightMm), "Cross section height must be strictly positive.");

        if (canvasWidthPx <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(canvasWidthPx), "Canvas width must be strictly positive.");

        if (canvasHeightPx <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(canvasHeightPx), "Canvas height must be strictly positive.");

        if (marginPx < 0.0 || (2.0 * marginPx >= canvasWidthPx) || (2.0 * marginPx >= canvasHeightPx))
            throw new ArgumentOutOfRangeException(nameof(marginPx), "Margin padding cannot exceed canvas dimensions.");

        double wDraw = canvasWidthPx - (2.0 * marginPx);
        double hDraw = canvasHeightPx - (2.0 * marginPx);

        double scale = Math.Min(wDraw / widthMm, hDraw / heightMm);

        double wContent = widthMm * scale;
        double hContent = heightMm * scale;

        double offsetX = marginPx + ((wDraw - wContent) / 2.0);
        double offsetY = marginPx + ((hDraw - hContent) / 2.0);
        double baseline = canvasHeightPx - offsetY;

        return new BeamCanvasTransform
        {
            Scale = scale,
            OffsetX = offsetX,
            OffsetY = offsetY,
            Baseline = baseline,
            CanvasWidth = canvasWidthPx,
            CanvasHeight = canvasHeightPx,
            XMin = -widthMm / 2.0,
            ZMin = 0.0,
            Margin = marginPx
        };
    }
}
