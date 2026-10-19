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
        Action onStep = () => progress?.Report(++done);
        var stirrups = new List<Rebar>();
        var mainBars = new List<Rebar>();
        var additionalBars = new List<Rebar>();
        var sideBars = new List<Rebar>();
        var specialBars = new List<Rebar>();

        using (var tg = new TransactionGroup(document, "Beam Rebar"))
        {
            tg.Start();
            try
            {
                CreateStirrupsPhase(document, stack, spec, shapes, catalog, onStep, stirrups);
                CreateMainBarsPhase(document, stack, spec, catalog, onStep, mainBars);
                CreateAdditionalBarsPhase(document, stack, spec, catalog, onStep, additionalBars);
                CreateSideBarsPhase(document, stack, spec, catalog, onStep, sideBars);
                CreateSpecialBarsPhase(document, stack, spec, shapes, catalog, onStep, specialBars);
                tg.Assimilate();
            }
            catch (Exception)
            {
                if (tg.HasStarted())
                {
                    tg.RollBack();
                }
                throw;
            }
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

    private static void CreateStirrupsPhase(
        Document document,
        BeamStack stack,
        BeamRebarSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        Action onStep,
        List<Rebar> stirrups)
    {
        using var t = new Transaction(document, "Create Stirrups");
        t.Start();
        RebarFailureHandling.Apply(t);
        stirrups.AddRange(BeamStirrupCreator.Create(
            document, stack, spec.Stirrups, shapes, catalog, spec.PartitionName, onStep));
        t.Commit();
    }

    private static void CreateMainBarsPhase(
        Document document,
        BeamStack stack,
        BeamRebarSpec spec,
        RebarTypeCatalog catalog,
        Action onStep,
        List<Rebar> mainBars)
    {
        using var t = new Transaction(document, "Create Main Bars");
        t.Start();
        RebarFailureHandling.Apply(t);
        mainBars.AddRange(BeamMainBarCreator.Create(
            document, stack, spec.MainBars, spec.Stirrups.Diameter, catalog, spec.PartitionName, onStep));
        t.Commit();
    }

    private static void CreateAdditionalBarsPhase(
        Document document,
        BeamStack stack,
        BeamRebarSpec spec,
        RebarTypeCatalog catalog,
        Action onStep,
        List<Rebar> additionalBars)
    {
        using var t = new Transaction(document, "Create Additional Bars");
        t.Start();
        RebarFailureHandling.Apply(t);
        additionalBars.AddRange(BeamAdditionalBarCreator.Create(
            document, stack, spec.AdditionalBars, spec.Stirrups.Diameter, catalog, spec.PartitionName, onStep));
        t.Commit();
    }

    private static void CreateSideBarsPhase(
        Document document,
        BeamStack stack,
        BeamRebarSpec spec,
        RebarTypeCatalog catalog,
        Action onStep,
        List<Rebar> sideBars)
    {
        using var t = new Transaction(document, "Create Side Bars");
        t.Start();
        RebarFailureHandling.Apply(t);
        sideBars.AddRange(BeamSideBarCreator.Create(
            document, stack, spec.SideBars, spec.Stirrups.Diameter, spec.MainBars.BottomDiameter, catalog, spec.PartitionName, onStep));
        t.Commit();
    }

    private static void CreateSpecialBarsPhase(
        Document document,
        BeamStack stack,
        BeamRebarSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        Action onStep,
        List<Rebar> specialBars)
    {
        using var t = new Transaction(document, "Create Special Bars");
        t.Start();
        RebarFailureHandling.Apply(t);
        specialBars.AddRange(BeamSpecialBarCreator.Create(
            document, stack, spec.SpecialBars, shapes, catalog, spec.PartitionName, onStep));
        t.Commit();
    }
}
