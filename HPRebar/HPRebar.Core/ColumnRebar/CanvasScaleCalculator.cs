using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Fits a column stack onto the preview canvas. Input is millimetres, output is a divisor: a drawing
///     length is model millimetres divided by the scale.
/// </summary>
public static class CanvasScaleCalculator
{
    /// <summary>Canvas width the elevation view is laid out against.</summary>
    public const double ElevationWidth = 600d;

    /// <summary>Smallest canvas height the elevation view uses, however short the column.</summary>
    public const double MinimumElevationHeight = 850d;

    /// <summary>Vertical padding above and below the stack in the elevation view.</summary>
    public const double ElevationMargin = 80d;

    private const double ElevationWidthBudget = 200d;
    private const double ElevationHeightBudget = 4640d;
    private const double SectionBudget = 270d;
    private const double DowelsBudget = 190d;

    /// <summary>Elevation view: scale, canvas size and the baseline the stack is drawn up from.</summary>
    public static ElevationLayout Elevation(IReadOnlyList<ColumnSection> stack)
    {
        RequireStack(stack);

        var shape = stack[0].Shape;

        // The elevation shows the column beside its own section, so both plan dimensions have to fit.
        var modelWidth = shape == SectionShape.Rectangle
            ? MaxOf(stack, section => section.B) + MaxOf(stack, section => section.H)
            : 2 * MaxOf(stack, section => section.D);

        var modelHeight = stack[stack.Count - 1].TopPosition + 2 * ElevationMargin;

        var scale = Math.Max(
            FitScale(modelHeight, ElevationHeightBudget),
            FitScale(modelWidth, ElevationWidthBudget));

        var height = Math.Max(MinimumElevationHeight, modelHeight / scale + 2 * ElevationMargin);

        return new ElevationLayout
        {
            Scale = scale,
            Width = ElevationWidth,
            Height = height,
            Baseline = height - ElevationMargin
        };
    }

    /// <summary>Scale for the cross-section view.</summary>
    public static double Section(IReadOnlyList<ColumnSection> stack) => FitScale(LargestPlanDimension(stack), SectionBudget);

    /// <summary>Scale for the dowel-layout view, which is drawn a little smaller than the section view.</summary>
    public static double Dowels(IReadOnlyList<ColumnSection> stack) => FitScale(LargestPlanDimension(stack), DowelsBudget);

    /// <summary>Largest single plan dimension anywhere in the stack.</summary>
    public static double LargestPlanDimension(IReadOnlyList<ColumnSection> stack)
    {
        RequireStack(stack);

        return stack[0].Shape == SectionShape.Rectangle
            ? Math.Max(MaxOf(stack, section => section.B), MaxOf(stack, section => section.H))
            : MaxOf(stack, section => section.D);
    }

    /// <summary>Anything that already fits is drawn at 1:1; anything larger is shrunk to the budget.</summary>
    private static double FitScale(double modelSize, double budget) => modelSize > budget ? modelSize / budget : 1d;

    private static double MaxOf(IReadOnlyList<ColumnSection> stack, Func<ColumnSection, double> selector)
    {
        var max = 0d;

        foreach (var section in stack)
        {
            var value = selector(section);

            if (value > max) max = value;
        }

        return max;
    }

    private static void RequireStack(IReadOnlyList<ColumnSection> stack)
    {
        if (stack is null) throw new ArgumentNullException(nameof(stack));

        if (stack.Count == 0)
        {
            throw new ArgumentException("The stack needs at least one segment.", nameof(stack));
        }
    }
}

/// <summary>Canvas geometry for the elevation preview.</summary>
public sealed record ElevationLayout
{
    public double Scale { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }

    /// <summary>Canvas Y of the bottom of the stack; the column is drawn upward from here.</summary>
    public double Baseline { get; init; }
}
