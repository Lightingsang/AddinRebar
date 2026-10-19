using System;
using System.Collections.Generic;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using Xunit;

namespace HPKata.Core.Tests.QuantitySurveying
{
    public class QsFormworkCalculatorTests
    {
        [Fact]
        public void Evaluate_BeamEndOnColumn_DeductsContactFaceFromColumn()
        {
            var col = new QsElementModel
            {
                Id = "C1",
                Mark = "C1",
                MemberType = QsMemberType.Column,
                WidthMm = 400,
                HeightMm = 400,
                LengthMm = 3600
            };
            var beam = new QsElementModel
            {
                Id = "B1",
                Mark = "B1",
                MemberType = QsMemberType.Beam,
                WidthMm = 250,
                HeightMm = 500,
                LengthMm = 5000,
                IntersectingElementIds = new List<string> { "C1" }
            };

            var results = QsFormworkCalculator.Evaluate(new[] { col, beam });

            var colFw = results["C1"];
            Assert.True(colFw.DeductedAreaM2 > 0);
            // 0.25 * 0.5 = 0.125 m2 deducted
            Assert.Equal(0.125, Math.Round(colFw.DeductedAreaM2, 4));
        }

        [Fact]
        public void Evaluate_SecondaryBeamOnPrimaryBeam_DeductsContactFace()
        {
            var prim = new QsElementModel
            {
                Id = "P1",
                Mark = "D1",
                MemberType = QsMemberType.Beam,
                IsPrimary = true,
                WidthMm = 300,
                HeightMm = 600,
                LengthMm = 6000
            };
            var sec = new QsElementModel
            {
                Id = "S1",
                Mark = "D2",
                MemberType = QsMemberType.Beam,
                IsPrimary = false,
                WidthMm = 200,
                HeightMm = 400,
                LengthMm = 4000,
                IntersectingElementIds = new List<string> { "P1" }
            };

            var results = QsFormworkCalculator.Evaluate(new[] { prim, sec });
            var primFw = results["P1"];
            // 0.2 * 0.4 = 0.08 m2 deducted
            Assert.Equal(0.08, Math.Round(primFw.DeductedAreaM2, 4));
        }

        [Fact]
        public void Evaluate_SlabOnBeam_DeductsContactAreaFromBeamSides()
        {
            var beam = new QsElementModel
            {
                Id = "B1",
                Mark = "B1",
                MemberType = QsMemberType.Beam,
                WidthMm = 250,
                HeightMm = 500,
                LengthMm = 6000
            };
            var slab = new QsElementModel
            {
                Id = "S1",
                Mark = "S1",
                MemberType = QsMemberType.Slab,
                LengthMm = 6000,
                WidthMm = 4000,
                ThicknessMm = 120,
                IntersectingElementIds = new List<string> { "B1" }
            };

            var results = QsFormworkCalculator.Evaluate(new[] { beam, slab });
            var beamFw = results["B1"];
            // 6.0 * 0.12 = 0.72 m2 deducted
            Assert.Equal(0.72, Math.Round(beamFw.DeductedAreaM2, 4));
        }

        [Fact]
        public void Evaluate_NullElements_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => QsFormworkCalculator.Evaluate(null!));
        }

        [Fact]
        public void Evaluate_ColumnSlabContact_DeductsSlabThicknessFromColumnPerimeter()
        {
            var col = new QsElementModel
            {
                Id = "C1",
                Mark = "C1",
                MemberType = QsMemberType.Column,
                WidthMm = 400,
                HeightMm = 400,
                LengthMm = 3600
            };
            var slab = new QsElementModel
            {
                Id = "S1",
                Mark = "S1",
                MemberType = QsMemberType.Slab,
                LengthMm = 6000,
                WidthMm = 4000,
                ThicknessMm = 120,
                IntersectingElementIds = new List<string> { "C1" }
            };

            var results = QsFormworkCalculator.Evaluate(new[] { col, slab });
            var colFw = results["C1"];
            // Column perimeter: 2 * (0.4 + 0.4) = 1.6m; Contact area: 1.6 * 0.12 = 0.192 m2
            Assert.Equal(0.192, Math.Round(colFw.DeductedAreaM2, 4));
        }
    }
}
