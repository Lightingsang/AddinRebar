using System;
using System.Collections.Generic;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using Xunit;

namespace HPKata.Core.Tests.QuantitySurveying
{
    public class QsDeductionEngineTests
    {
        [Fact]
        public void Evaluate_ColumnFootingOverlap_DeductsEmbeddedFootingFromColumn()
        {
            var footing = new QsElementModel
            {
                Id = "F1",
                Mark = "F1",
                MemberType = QsMemberType.Footing,
                LengthMm = 2000,
                WidthMm = 2000,
                HeightMm = 800
            };
            var col = new QsElementModel
            {
                Id = "C1",
                Mark = "C1",
                MemberType = QsMemberType.Column,
                WidthMm = 400,
                HeightMm = 400,
                LengthMm = 3600,
                EmbedmentDepthMm = 300,
                IntersectingElementIds = new List<string> { "F1" }
            };

            var results = QsIntersectionDeductionEngine.Evaluate(new[] { footing, col });

            Assert.True(results.ContainsKey("C1"));
            var colRes = results["C1"];
            Assert.True(colRes.Deductions.Count > 0);
            Assert.True(colRes.NetVolumeM3 < colRes.GrossVolumeM3);
        }

        [Fact]
        public void Evaluate_ColumnBeamOverlap_BeamDeductsColumnJointOverlap()
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

            var results = QsIntersectionDeductionEngine.Evaluate(new[] { col, beam });

            // Column maintains gross volume through joint
            Assert.Equal(results["C1"].GrossVolumeM3, results["C1"].NetVolumeM3);

            // Beam deducts joint volume
            var beamRes = results["B1"];
            Assert.True(beamRes.Deductions.Count > 0);
            Assert.True(beamRes.NetVolumeM3 < beamRes.GrossVolumeM3);
        }

        [Fact]
        public void Evaluate_PrimarySecondaryBeamOverlap_SecondaryDeductsIntersection()
        {
            var primary = new QsElementModel
            {
                Id = "PB1",
                Mark = "D1",
                MemberType = QsMemberType.Beam,
                IsPrimary = true,
                WidthMm = 300,
                HeightMm = 600,
                LengthMm = 6000
            };
            var secondary = new QsElementModel
            {
                Id = "SB1",
                Mark = "D1A",
                MemberType = QsMemberType.Beam,
                IsPrimary = false,
                WidthMm = 200,
                HeightMm = 400,
                LengthMm = 4000,
                IntersectingElementIds = new List<string> { "PB1" }
            };

            var results = QsIntersectionDeductionEngine.Evaluate(new[] { primary, secondary });

            // Primary maintains gross volume
            Assert.Equal(results["PB1"].GrossVolumeM3, results["PB1"].NetVolumeM3);

            // Secondary deducts 0.2 * 0.4 * 0.3 = 0.024 m3
            var secRes = results["SB1"];
            Assert.Equal(0.024, Math.Round(secRes.DeductedVolumeM3, 4));
        }

        [Fact]
        public void Evaluate_BeamSlabOverlap_SlabDeductsBeamStem()
        {
            var beam = new QsElementModel
            {
                Id = "B1",
                Mark = "D1",
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

            var results = QsIntersectionDeductionEngine.Evaluate(new[] { beam, slab });

            var slabRes = results["S1"];
            // Stem: 0.25 * 6.0 * 0.12 = 0.18 m3 deducted
            Assert.Equal(0.18, Math.Round(slabRes.DeductedVolumeM3, 4));
        }

        [Fact]
        public void Evaluate_FootingPiles_DeductsEmbeddedPileVolume()
        {
            var footing = new QsElementModel
            {
                Id = "F1",
                Mark = "DC1",
                MemberType = QsMemberType.Footing,
                LengthMm = 1800,
                WidthMm = 1800,
                HeightMm = 900,
                EmbeddedPileCount = 4,
                EmbeddedPileAreaMm2 = 125663.7, // D400 circle
                EmbedmentDepthMm = 100
            };

            var results = QsIntersectionDeductionEngine.Evaluate(new[] { footing });
            var res = results["F1"];
            Assert.True(res.DeductedVolumeM3 > 0.0);
            Assert.True(res.NetVolumeM3 < res.GrossVolumeM3);
        }

        [Fact]
        public void Evaluate_NonIntersectingElements_NoDeductions()
        {
            var b1 = new QsElementModel { Id = "B1", MemberType = QsMemberType.Beam, LengthMm = 5000, WidthMm = 200, HeightMm = 400 };
            var b2 = new QsElementModel { Id = "B2", MemberType = QsMemberType.Beam, LengthMm = 5000, WidthMm = 200, HeightMm = 400 };

            var results = QsIntersectionDeductionEngine.Evaluate(new[] { b1, b2 });
            Assert.Equal(0.0, results["B1"].DeductedVolumeM3);
            Assert.Equal(0.0, results["B2"].DeductedVolumeM3);
        }

        [Fact]
        public void Evaluate_NullElements_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => QsIntersectionDeductionEngine.Evaluate(null!));
        }
    }
}
