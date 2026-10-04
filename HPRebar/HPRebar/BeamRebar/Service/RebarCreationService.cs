using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Master coordinator of all reinforcement creation passes for continuous beams.
/// Pre-flights required shapes and runs creation in 5 staged transactions.
/// </summary>
public static class RebarCreationService
{
    public static ValidationResult CanCreate(
        RebarShapeResolver shapes,
        BeamStack stack,
        BeamRebarSpec spec)
    {
        if (shapes.MainStirrup() is null)
            return ValidationResult.Fail(20, "Rectangular stirrup shape (M_T1 or T1) is not loaded in the project. Load it before creating stirrups.");

        if (spec.SideBars.IncludeCrossTies && shapes.CrossTie() is null)
            return ValidationResult.Fail(21, "Cross-tie shape (M_T10 or T10) is not loaded in the project. Load it before creating cross-ties.");

        return ValidationResult.Ok;
    }

    public static CreatedBeamRebar Create(
        Document document,
        BeamStack stack,
        BeamRebarSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        IProgress<int>? progress = null)
    {
        // The stack was read from the model with a placeholder cover; every bar group must use the cover
        // entered in the window, the same one the preview draws with.
        stack = stack with { ContinuousStack = stack.ContinuousStack.WithCover(spec.Stirrups.Cover) };

        int done = 0;
        var stirrups = new List<Rebar>();
        var mainBars = new List<Rebar>();
        var additionalBars = new List<Rebar>();
        var sideBars = new List<Rebar>();
        var specialBars = new List<Rebar>();

        // Phase 1: Stirrups
        using (var t = new Transaction(document, "Create Stirrups"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            stirrups.AddRange(BeamStirrupCreator.Create(
                document, stack, spec.Stirrups, shapes, catalog, spec.PartitionName, () => progress?.Report(++done)));
            t.Commit();
        }

        // Phase 2: Main Longitudinal Bars
        using (var t = new Transaction(document, "Create Main Bars"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            mainBars.AddRange(BeamMainBarCreator.Create(
                document, stack, spec.MainBars, spec.Stirrups.Diameter, catalog, spec.PartitionName, () => progress?.Report(++done)));
            t.Commit();
        }

        // Phase 3: Additional Bars
        using (var t = new Transaction(document, "Create Additional Bars"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            additionalBars.AddRange(BeamAdditionalBarCreator.Create(
                document, stack, spec.AdditionalBars, spec.Stirrups.Diameter, catalog, spec.PartitionName, () => progress?.Report(++done)));
            t.Commit();
        }

        // Phase 4: Side Bars & Cross-Ties
        using (var t = new Transaction(document, "Create Side Bars"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            sideBars.AddRange(BeamSideBarCreator.Create(
                document, stack, spec.SideBars, spec.Stirrups.Diameter, spec.MainBars.BottomDiameter, catalog, spec.PartitionName, () => progress?.Report(++done)));
            t.Commit();
        }

        // Phase 5: Special Bars
        using (var t = new Transaction(document, "Create Special Bars"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            specialBars.AddRange(BeamSpecialBarCreator.Create(
                document, stack, spec.SpecialBars, shapes, catalog, spec.PartitionName, () => progress?.Report(++done)));
            t.Commit();
        }

        return new CreatedBeamRebar
        {
            Stirrups = stirrups,
            MainBars = mainBars,
            AdditionalBars = additionalBars,
            SideBars = sideBars,
            SpecialBars = specialBars
        };
    }
}
