using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;
using Xunit;

namespace HPRebar.Core.Tests.Tcvn5574;

public sealed class BeamForceEnvelopeTests
{
    [Fact]
    public void BeamForceEnvelope_ComputesMaxMomentAndShearCorrectly()
    {
        var envelope = new BeamForceEnvelope
        {
            BeamMark = "2D1",
            SpanIndex = 0,
            SpanLengthMm = 6000.0,
            MomentSupportLeftKnm = -180.5,
            MomentMidSpanKnm = 145.2,
            MomentSupportRightKnm = -162.0,
            ShearSupportLeftKn = 195.4,
            ShearMidSpanKn = 25.0,
            ShearSupportRightKn = 188.0
        };

        Assert.Equal("2D1", envelope.BeamMark);
        Assert.Equal(0, envelope.SpanIndex);
        Assert.Equal(6000.0, envelope.SpanLengthMm);
        Assert.Equal(180.5, envelope.MaxMomentKnm, precision: 1);
        Assert.Equal(195.4, envelope.MaxShearKn, precision: 1);
    }

    [Fact]
    public void BeamCalculationReportModel_StatusReflectsCompliance()
    {
        var reportOk = new BeamCalculationReportModel
        {
            BeamMark = "B1",
            SpanIndex = 0,
            IsCompliant = true
        };

        var reportFail = new BeamCalculationReportModel
        {
            BeamMark = "B2",
            SpanIndex = 0,
            IsCompliant = false
        };

        Assert.Equal("ĐẠT", reportOk.Status);
        Assert.Equal("NG", reportFail.Status);
    }

    [Fact]
    public void CompleteBeamDesignWorkflow_ProducesCohesiveReportModel()
    {
        // 1. Inputs
        double b = 300.0;
        double h = 600.0;
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var longSteel = SteelMaterial.Create(SteelGrade.CB400V);
        var stirrupSteel = SteelMaterial.Create(SteelGrade.CB240T);

        var envelope = new BeamForceEnvelope
        {
            BeamMark = "D-101",
            SpanIndex = 0,
            SpanLengthMm = 5500.0,
            MomentSupportLeftKnm = -150.0,
            MomentMidSpanKnm = 120.0,
            MomentSupportRightKnm = -140.0,
            ShearSupportLeftKn = 160.0,
            ShearMidSpanKn = 30.0,
            ShearSupportRightKn = 150.0
        };

        // 2. Flexural calculations
        var flexLeft = TcvnBeamFlexureCalculator.Calculate(b, h, envelope.MomentSupportLeftKnm, concrete, longSteel);
        var flexMid = TcvnBeamFlexureCalculator.Calculate(b, h, envelope.MomentMidSpanKnm, concrete, longSteel);
        var flexRight = TcvnBeamFlexureCalculator.Calculate(b, h, envelope.MomentSupportRightKnm, concrete, longSteel);

        // 3. Shear calculations
        var shearLeft = TcvnBeamShearCalculator.Calculate(b, h, envelope.ShearSupportLeftKn, concrete, stirrupSteel, isSupportZone: true);
        var shearMid = TcvnBeamShearCalculator.Calculate(b, h, envelope.ShearMidSpanKn, concrete, stirrupSteel, isSupportZone: false);
        var shearRight = TcvnBeamShearCalculator.Calculate(b, h, envelope.ShearSupportRightKn, concrete, stirrupSteel, isSupportZone: true);

        // 4. Rebar optimizations
        var topRebar = TcvnBeamRebarOptimizer.Optimize(flexLeft.AsRequired, b, h, isTop: true);
        var botRebar = TcvnBeamRebarOptimizer.Optimize(flexMid.AsRequired, b, h, isTop: false);

        bool allCompliant = flexLeft.IsCompliant && flexMid.IsCompliant && flexRight.IsCompliant &&
                            shearLeft.IsCompliant && shearMid.IsCompliant && shearRight.IsCompliant &&
                            topRebar.IsValid && botRebar.IsValid;

        // 5. Build report
        var report = new BeamCalculationReportModel
        {
            BeamMark = envelope.BeamMark,
            SpanIndex = envelope.SpanIndex,
            WidthMm = b,
            HeightMm = h,
            LengthMm = envelope.SpanLengthMm,
            ConcreteGrade = concrete.Name,
            LongitudinalSteelGrade = longSteel.Name,
            StirrupSteelGrade = stirrupSteel.Name,
            FlexureLeft = flexLeft,
            FlexureMid = flexMid,
            FlexureRight = flexRight,
            ShearLeft = shearLeft,
            ShearMid = shearMid,
            ShearRight = shearRight,
            TopSupportRebar = topRebar,
            BottomSpanRebar = botRebar,
            StirrupDescription = $"phi8 a{shearLeft.RecommendedSpacing} / a{shearMid.RecommendedSpacing}",
            IsCompliant = allCompliant,
            Notes = "All TCVN 5574:2018 checks passed."
        };

        Assert.True(report.IsCompliant);
        Assert.Equal("ĐẠT", report.Status);
        Assert.NotNull(report.TopSupportRebar);
        Assert.NotNull(report.BottomSpanRebar);
        Assert.Contains("phi8", report.StirrupDescription);
    }
}
