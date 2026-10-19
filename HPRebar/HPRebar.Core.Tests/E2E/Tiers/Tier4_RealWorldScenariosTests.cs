using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;
using Xunit;

namespace HPRebar.Core.Tests.E2E.Tiers;

/// <summary>
/// Tier 4: Real-World Engineering Scenarios Acceptance Tests.
/// Full lifecycle validation on canonical civil benchmarks:
/// D1: Typical Residential Beam (200x400)
/// D2: Commercial Heavy Transfer Beam (400x800)
/// D3: Continuous 3-Span Frame Beam (250x500)
/// D4: End-to-End 3D Rebar Geometry Integration Pipeline
/// </summary>
public sealed class Tier4_RealWorldScenariosTests
{
    [Fact]
    public void TC_T4_BENCH_01_ResidentialBeam_FullEndToEndDesign()
    {
        // 1. Inputs: 200 x 400 mm, L = 4200 mm, B20, CB300-V, CB240-T stirrups
        double b = 200.0, h = 400.0;
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B20);
        var steel = SteelMaterial.Create(SteelGrade.CB300V);
        var stirrupSteel = SteelMaterial.Create(SteelGrade.CB240T);

        // 2. Midspan Bending Design (M = 52.0 kNm)
        var flxMid = TcvnBeamFlexureCalculator.Calculate(b, h, 52.0, concrete, steel);
        Assert.True(flxMid.IsCompliant);
        Assert.InRange(flxMid.AsRequired, 590.0, 640.0);

        var optMid = TcvnBeamRebarOptimizer.Optimize(flxMid.AsRequired, b, h, isTop: false);
        Assert.True(optMid.IsValid);
        Assert.True(optMid.ProvidedArea >= flxMid.AsRequired);
        Assert.True(optMid.IsSpacingCompliant);

        // 3. Support Bending Design (M = 64.0 kNm)
        var flxSup = TcvnBeamFlexureCalculator.Calculate(b, h, 64.0, concrete, steel);
        Assert.True(flxSup.IsCompliant);
        Assert.InRange(flxSup.AsRequired, 750.0, 800.0);

        var optSup = TcvnBeamRebarOptimizer.Optimize(flxSup.AsRequired, b, h, isTop: true);
        Assert.True(optSup.IsValid);
        Assert.True(optSup.ProvidedArea >= flxSup.AsRequired);

        // 4. Shear Design (Q = 55.0 kN)
        var shear = TcvnBeamShearCalculator.Calculate(b, h, 55.0, concrete, stirrupSteel, stirrupDiameter: 8.0, legs: 2);
        Assert.True(shear.IsCompliant);
        Assert.False(shear.IsWebCrushExceeded);
        Assert.InRange(shear.RecommendedSpacing, 100.0, 150.0);
    }

    [Fact]
    public void TC_T4_BENCH_02_CommercialTransferBeam_FullEndToEndDesign()
    {
        // 1. Inputs: 400 x 800 mm, L = 7500 mm, B30, CB400-V, CB240-T stirrups
        double b = 400.0, h = 800.0;
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B30);
        var steel = SteelMaterial.Create(SteelGrade.CB400V);
        var stirrupSteel = SteelMaterial.Create(SteelGrade.CB240T);

        // 2. Heavy Bending Moment (M = 450.0 kNm)
        var flx = TcvnBeamFlexureCalculator.Calculate(b, h, 450.0, concrete, steel);
        Assert.True(flx.IsCompliant);
        Assert.InRange(flx.AsRequired, 1700.0, 2100.0);

        var opt = TcvnBeamRebarOptimizer.Optimize(flx.AsRequired, b, h, isTop: false);
        Assert.True(opt.IsValid);
        Assert.True(opt.ProvidedArea >= flx.AsRequired);

        // 3. Heavy Shear Design (Q = 360.0 kN, 4 legs stirrup)
        var shear = TcvnBeamShearCalculator.Calculate(
            b, h, 360.0, concrete, stirrupSteel, stirrupDiameter: 10.0, legs: 4, isSupportZone: true);
        Assert.True(shear.IsCompliant);
        Assert.False(shear.IsWebCrushExceeded);
        Assert.True(shear.RecommendedSpacing <= 150.0);

        // 4. Deep Beam Side Reinforcement (h = 800 >= 700 mm)
        var span = new BeamSpan(0, "Transfer", 7500.0, b, h, 0.0, 30.0, 6900.0);
        var stack = new BeamContinuousStack(new[] { span }, Array.Empty<BeamSupportNode>());
        var sideSpec = new BeamSideBarSpec(autoSkinBars: true, depthThreshold: 700.0, diameter: 14.0);
        var skinBars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, sideSpec, 10.0, 25.0);
        Assert.NotEmpty(skinBars);
    }

    [Fact]
    public void TC_T4_BENCH_03_ContinuousThreeSpanFrame_FullSpanCoordination()
    {
        // 3 Spans: 4800, 5600, 4800 mm, Section 250 x 500 mm, B25, CB400-V
        double b = 250.0, h = 500.0;
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var steel = SteelMaterial.Create(SteelGrade.CB400V);

        var spans = new List<BeamSpan>
        {
            new(0, "Span 1", 4800.0, b, h, 0.0, 25.0, 4400.0),
            new(1, "Span 2", 5600.0, b, h, 0.0, 25.0, 5200.0),
            new(2, "Span 3", 4800.0, b, h, 0.0, 25.0, 4400.0)
        };

        // Interior Support Negative Moment (M = 145 kNm)
        var flxInteriorSupport = TcvnBeamFlexureCalculator.Calculate(b, h, 145.0, concrete, steel);
        Assert.True(flxInteriorSupport.IsCompliant);
        Assert.InRange(flxInteriorSupport.AsRequired, 1000.0, 1200.0);

        var optSupport = TcvnBeamRebarOptimizer.Optimize(flxInteriorSupport.AsRequired, b, h, isTop: true);
        Assert.True(optSupport.IsValid);

        // Verify independent stirrup distribution on all 3 spans
        var stirrupSpec = new BeamStirrupSpec(
            layout: StirrupLayout.ThreeZoneL4,
            diameter: 8.0,
            cover: 25.0,
            spacingDense: 100.0,
            spacingSparse: 200.0);

        foreach (var spanItem in spans)
        {
            var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(spanItem.LengthClear, stirrupSpec);
            Assert.NotEmpty(runs);
            Assert.Equal(3, runs.Count);
        }
    }

    [Fact]
    public void TC_T4_BENCH_04_3DGeometryPipeline_ProducesBoundingBoxContainedRebars()
    {
        double b = 250.0, clearSpan = 5000.0;
        var stirrupSpec = new BeamStirrupSpec(
            layout: StirrupLayout.ThreeZoneL4,
            diameter: 8.0,
            cover: 25.0,
            spacingDense: 100.0,
            spacingSparse: 200.0);

        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(clearSpan, stirrupSpec);

        Assert.NotEmpty(runs);
        Assert.All(runs, r =>
        {
            Assert.True(r.Count > 0);
            Assert.True(r.Spacing is 100.0 or 200.0);
        });

        // Skin bars 3D coordinates
        var deepSpan = new BeamSpan(0, "Deep", clearSpan, b, 750.0, 0.0, 25.0, clearSpan);
        var stack = new BeamContinuousStack(new[] { deepSpan }, Array.Empty<BeamSupportNode>());
        var sideSpec = new BeamSideBarSpec(autoSkinBars: true, depthThreshold: 700.0, diameter: 12.0);
        var skinBars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, sideSpec, 8.0, 20.0);

        Assert.NotEmpty(skinBars);
        foreach (var bar in skinBars)
        {
            foreach (var pt in bar.Polyline.Points)
            {
                // Coordinates must be bounded within the beam physical bounds
                Assert.InRange(pt.X, 0.0, clearSpan);
                Assert.InRange(pt.Y, -b / 2.0, b / 2.0);
                Assert.InRange(pt.Z, -750.0, 750.0);
            }
        }
    }
}
