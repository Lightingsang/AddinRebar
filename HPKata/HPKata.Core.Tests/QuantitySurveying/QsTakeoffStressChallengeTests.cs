using System;
using System.Collections.Generic;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using Xunit;

namespace HPKata.Core.Tests.QuantitySurveying
{
    public class QsTakeoffStressChallengeTests
    {
        [Fact]
        public void Adversarial_AllNegativeDimensions_AllMemberTypes_VolumeAndFormworkStrictlyZero()
        {
            var types = new[]
            {
                QsMemberType.Column, QsMemberType.Beam, QsMemberType.Footing,
                QsMemberType.Wall, QsMemberType.Slab, QsMemberType.Stair
            };

            foreach (var type in types)
            {
                var elem = new QsElementModel
                {
                    MemberType = type,
                    WidthMm = -500.0, HeightMm = -600.0, LengthMm = -3000.0,
                    ThicknessMm = -120.0, PlanAreaMm2 = -200000.0, PerimeterMm = -4000.0
                };

                double vol = QsTakeoffCalculator.CalculateGrossVolume(elem);
                var (soffit, side) = QsTakeoffCalculator.CalculateGrossFormwork(elem);

                Assert.Equal(0.0, vol);
                Assert.Equal(0.0, soffit);
                Assert.Equal(0.0, side);
            }
        }

        [Fact]
        public void Adversarial_MixedSigns_DoubleNegative_VolumeStrictlyZero()
        {
            var testCases = new (double w, double h, double l)[]
            {
                (-500, 600, 3000), (500, -600, 3000), (500, 600, -3000),
                (-500, -600, 3000), (-500, 600, -3000), (500, -600, -3000)
            };

            foreach (var (w, h, l) in testCases)
            {
                var beam = new QsElementModel { MemberType = QsMemberType.Beam, WidthMm = w, HeightMm = h, LengthMm = l };
                var col = new QsElementModel { MemberType = QsMemberType.Column, WidthMm = w, HeightMm = h, LengthMm = l };

                Assert.Equal(0.0, QsTakeoffCalculator.CalculateGrossVolume(beam));
                Assert.Equal(0.0, QsTakeoffCalculator.CalculateGrossVolume(col));
            }
        }

        [Fact]
        public void Adversarial_ZeroDimensions_ProducesZeroVolume()
        {
            var zeroCases = new (double w, double h, double l)[]
            {
                (0.0, 600.0, 5000.0), (500.0, 0.0, 5000.0), (500.0, 600.0, 0.0)
            };

            foreach (var (w, h, l) in zeroCases)
            {
                var beam = new QsElementModel { MemberType = QsMemberType.Beam, WidthMm = w, HeightMm = h, LengthMm = l };
                Assert.Equal(0.0, QsTakeoffCalculator.CalculateGrossVolume(beam));
            }
        }

        [Fact]
        public void Adversarial_ExtremeDimensions_NoOverflowCrashes()
        {
            var hugeCol = new QsElementModel
            {
                MemberType = QsMemberType.Column, WidthMm = 1e6, HeightMm = 1e6, LengthMm = 1e7
            };

            double vol = QsTakeoffCalculator.CalculateGrossVolume(hugeCol);
            var (soffit, side) = QsTakeoffCalculator.CalculateGrossFormwork(hugeCol);

            Assert.Equal(1e10, vol);
            Assert.Equal(0.0, soffit);
            Assert.Equal(4e7, side);

            var ultraBeam = new QsElementModel
            {
                MemberType = QsMemberType.Beam, WidthMm = 1e12, HeightMm = 1e12, LengthMm = 1e12
            };
            double ultraVol = QsTakeoffCalculator.CalculateGrossVolume(ultraBeam);
            Assert.True(ultraVol > 0.0 && !double.IsNaN(ultraVol));
        }

        [Fact]
        public void DeductColumnContact_BeamAndSlab_VerifiesHelperDeductions()
        {
            var col = new QsElementModel { Id = "C1", Mark = "C1", MemberType = QsMemberType.Column, WidthMm = 400.0, HeightMm = 400.0, LengthMm = 3600.0 };
            var beam = new QsElementModel { Id = "B1", Mark = "B1", MemberType = QsMemberType.Beam, WidthMm = 250.0, HeightMm = 500.0, LengthMm = 5000.0, IntersectingElementIds = new List<string> { "C1" } };
            var slab = new QsElementModel { Id = "S1", Mark = "S1", MemberType = QsMemberType.Slab, ThicknessMm = 150.0, IntersectingElementIds = new List<string> { "C1" } };

            var results = QsFormworkCalculator.Evaluate(new[] { col, beam, slab });
            var colFw = results["C1"];

            Assert.Equal(2, colFw.Deductions.Count);
            double expectedDeduction = (0.25 * 0.50) + ((2.0 * (0.4 + 0.4)) * 0.15);
            Assert.Equal(Math.Round(expectedDeduction, 4), Math.Round(colFw.DeductedAreaM2, 4));
        }

        [Fact]
        public void DeductBeamContact_SecondaryBeamAndSlab_VerifiesHelperDeductions()
        {
            var prim = new QsElementModel { Id = "PB1", Mark = "D1", MemberType = QsMemberType.Beam, IsPrimary = true, WidthMm = 300.0, HeightMm = 600.0, LengthMm = 6000.0 };
            var sec = new QsElementModel { Id = "SB1", Mark = "D2", MemberType = QsMemberType.Beam, IsPrimary = false, WidthMm = 200.0, HeightMm = 400.0, LengthMm = 4000.0, IntersectingElementIds = new List<string> { "PB1" } };
            var slab = new QsElementModel { Id = "S1", Mark = "S1", MemberType = QsMemberType.Slab, ThicknessMm = 120.0, IntersectingElementIds = new List<string> { "PB1" } };

            var results = QsFormworkCalculator.Evaluate(new[] { prim, sec, slab });
            var primFw = results["PB1"];

            Assert.Equal(2, primFw.Deductions.Count);
            double expectedTotal = (0.20 * 0.40) + (6.0 * 0.12);
            Assert.Equal(Math.Round(expectedTotal, 4), Math.Round(primFw.DeductedAreaM2, 4));
        }

        [Fact]
        public void DeductFootingContact_BeamFramingIntoFooting_VerifiesHelperDeductions()
        {
            var footing = new QsElementModel { Id = "F1", Mark = "F1", MemberType = QsMemberType.Footing, LengthMm = 2000.0, WidthMm = 2000.0, HeightMm = 800.0 };
            var beam = new QsElementModel { Id = "B1", Mark = "B1", MemberType = QsMemberType.Beam, WidthMm = 300.0, HeightMm = 500.0, LengthMm = 4000.0, IntersectingElementIds = new List<string> { "F1" } };

            var results = QsFormworkCalculator.Evaluate(new[] { footing, beam });
            var footingFw = results["F1"];

            Assert.Single(footingFw.Deductions);
            double expectedDeduction = 0.30 * 0.50;
            Assert.Equal(Math.Round(expectedDeduction, 4), Math.Round(footingFw.DeductedAreaM2, 4));
            double expectedGross = 2.0 * (2.0 + 2.0) * 0.8;
            Assert.Equal(Math.Round(expectedGross - expectedDeduction, 4), Math.Round(footingFw.NetFormworkM2, 4));
        }

        [Fact]
        public void ContactDeductions_NeverExceedTotalSurfaceArea_MassiveDeductionClampsToZero()
        {
            var col = new QsElementModel { Id = "C_Small", Mark = "C_Small", MemberType = QsMemberType.Column, WidthMm = 400.0, HeightMm = 400.0, LengthMm = 3000.0 };
            var colossalSlab = new QsElementModel { Id = "S_Colossal", Mark = "S_Colossal", MemberType = QsMemberType.Slab, ThicknessMm = 10000.0, IntersectingElementIds = new List<string> { "C_Small" } };

            var results = QsFormworkCalculator.Evaluate(new[] { col, colossalSlab });
            var colFw = results["C_Small"];

            Assert.Equal(0.0, colFw.NetFormworkM2);
            Assert.Equal(colFw.GrossFormworkM2, colFw.DeductedAreaM2);
            Assert.True(colFw.DeductedAreaM2 <= colFw.GrossFormworkM2);
        }

        [Fact]
        public void ContactDeductions_NegativeDimensionsInContactElements_ProducesZeroDeductions()
        {
            var col = new QsElementModel { Id = "C1", Mark = "C1", MemberType = QsMemberType.Column, WidthMm = 400.0, HeightMm = 400.0, LengthMm = 3000.0 };
            var negBeam = new QsElementModel { Id = "B_Neg", Mark = "B_Neg", MemberType = QsMemberType.Beam, WidthMm = -250.0, HeightMm = -500.0, LengthMm = 4000.0, IntersectingElementIds = new List<string> { "C1" } };
            var negSlab = new QsElementModel { Id = "S_Neg", Mark = "S_Neg", MemberType = QsMemberType.Slab, ThicknessMm = -150.0, IntersectingElementIds = new List<string> { "C1" } };

            var results = QsFormworkCalculator.Evaluate(new[] { col, negBeam, negSlab });
            var colFw = results["C1"];

            Assert.Equal(colFw.GrossFormworkM2, colFw.NetFormworkM2);
            Assert.Equal(0.0, colFw.DeductedAreaM2);
        }
    }
}
