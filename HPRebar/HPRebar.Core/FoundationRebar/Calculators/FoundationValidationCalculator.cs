using System;
using System.Collections.Generic;
using HPRebar.Core.FoundationRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.Core.FoundationRebar.Calculators;

/// <summary>
/// Pre-flight engineering and geometric validator for foundation rebar parameters.
/// </summary>
public static class FoundationValidationCalculator
{

    /// <summary>
    /// Performs complete engineering and geometric validation on the foundation snapshot and rebar specification.
    /// </summary>
    public static FoundationValidationResult Validate(FoundationGeometrySnapshot snapshot, FoundationRebarSpec spec)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        var errors = new List<string>();

        // 1. Spacing and Diameters checks (non-positive s <= 0 -> error)
        if (spec.SpacingBottomX <= 0)
            errors.Add($"Bottom X spacing must be positive. Received: {spec.SpacingBottomX:F1} mm.");
        if (spec.SpacingBottomY <= 0)
            errors.Add($"Bottom Y spacing must be positive. Received: {spec.SpacingBottomY:F1} mm.");
        if (spec.DiameterBottomX <= 0)
            errors.Add($"Bottom X diameter must be positive. Received: {spec.DiameterBottomX:F1} mm.");
        if (spec.DiameterBottomY <= 0)
            errors.Add($"Bottom Y diameter must be positive. Received: {spec.DiameterBottomY:F1} mm.");

        if (spec.IsTopMatEnabled)
        {
            if (spec.SpacingTopX <= 0)
                errors.Add($"Top X spacing must be positive. Received: {spec.SpacingTopX:F1} mm.");
            if (spec.SpacingTopY <= 0)
                errors.Add($"Top Y spacing must be positive. Received: {spec.SpacingTopY:F1} mm.");
            if (spec.DiameterTopX <= 0)
                errors.Add($"Top X diameter must be positive. Received: {spec.DiameterTopX:F1} mm.");
            if (spec.DiameterTopY <= 0)
                errors.Add($"Top Y diameter must be positive. Received: {spec.DiameterTopY:F1} mm.");
        }

        // 2. Concrete covers non-negative checks
        if (spec.CoverTop < 0)
            errors.Add($"Top cover cannot be negative. Received: {spec.CoverTop:F1} mm.");
        if (spec.CoverBottom < 0)
            errors.Add($"Bottom cover cannot be negative. Received: {spec.CoverBottom:F1} mm.");
        if (spec.CoverSide < 0)
            errors.Add($"Side cover cannot be negative. Received: {spec.CoverSide:F1} mm.");

        // 3. Boundary checks (L <= 2 * c_side or W <= 2 * c_side)
        if (spec.CoverSide >= 0)
        {
            if (snapshot.Length <= 2 * spec.CoverSide)
                errors.Add($"Foundation Length ({snapshot.Length:F1} mm) must be greater than 2 * Side Cover ({2 * spec.CoverSide:F1} mm).");
            if (snapshot.Width <= 2 * spec.CoverSide)
                errors.Add($"Foundation Width ({snapshot.Width:F1} mm) must be greater than 2 * Side Cover ({2 * spec.CoverSide:F1} mm).");
        }

        // 4. Insufficient slab thickness check (H < c_bot + c_top + sum(d_active))
        double minRequiredThickness = spec.CoverBottom + spec.CoverTop + spec.DiameterBottomX + spec.DiameterBottomY;
        if (spec.IsTopMatEnabled)
        {
            minRequiredThickness += spec.DiameterTopX + spec.DiameterTopY;
        }

        if (snapshot.Thickness < minRequiredThickness)
        {
            errors.Add(
                $"Foundation thickness ({snapshot.Thickness:F1} mm) is insufficient. " +
                $"Minimum required thickness is {minRequiredThickness:F1} mm to accommodate covers and rebar layers " +
                $"(Covers: {spec.CoverBottom + spec.CoverTop:F1} mm, Rebar diameters: {minRequiredThickness - spec.CoverBottom - spec.CoverTop:F1} mm).");
        }

        // 5. Excessive bar count check (> 1002 per layer)
        if (spec.CoverSide >= 0 && snapshot.Length > 2 * spec.CoverSide && snapshot.Width > 2 * spec.CoverSide)
        {
            double effLength = snapshot.Length - 2 * spec.CoverSide;
            double effWidth = snapshot.Width - 2 * spec.CoverSide;

            // Bottom X bars distributed along Width (effWidth)
            if (spec.SpacingBottomX > 0)
            {
                int countBx = (int)Math.Floor(effWidth / spec.SpacingBottomX) + 1;
                if (countBx > RevitRebarLimits.MaxBarPositions)
                    errors.Add($"Excessive bar count for Bottom X layer ({countBx} bars). Maximum allowed is {RevitRebarLimits.MaxBarPositions}.");
            }

            // Bottom Y bars distributed along Length (effLength)
            if (spec.SpacingBottomY > 0)
            {
                int countBy = (int)Math.Floor(effLength / spec.SpacingBottomY) + 1;
                if (countBy > RevitRebarLimits.MaxBarPositions)
                    errors.Add($"Excessive bar count for Bottom Y layer ({countBy} bars). Maximum allowed is {RevitRebarLimits.MaxBarPositions}.");
            }

            if (spec.IsTopMatEnabled)
            {
                // Top X bars distributed along Width (effWidth)
                if (spec.SpacingTopX > 0)
                {
                    int countTx = (int)Math.Floor(effWidth / spec.SpacingTopX) + 1;
                    if (countTx > RevitRebarLimits.MaxBarPositions)
                        errors.Add($"Excessive bar count for Top X layer ({countTx} bars). Maximum allowed is {RevitRebarLimits.MaxBarPositions}.");
                }

                // Top Y bars distributed along Length (effLength)
                if (spec.SpacingTopY > 0)
                {
                    int countTy = (int)Math.Floor(effLength / spec.SpacingTopY) + 1;
                    if (countTy > RevitRebarLimits.MaxBarPositions)
                        errors.Add($"Excessive bar count for Top Y layer ({countTy} bars). Maximum allowed is {RevitRebarLimits.MaxBarPositions}.");
                }
            }
        }

        return errors.Count == 0
            ? FoundationValidationResult.Success()
            : FoundationValidationResult.Failure(errors);
    }
}
