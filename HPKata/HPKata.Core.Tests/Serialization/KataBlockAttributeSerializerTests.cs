using System;
using System.Collections.Generic;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.Core.Tests.Serialization
{
    public class KataBlockAttributeSerializerTests
    {
        [Fact]
        public void TagBlock_RoundTripSerialization_PreservesAllFields()
        {
            var tagData = new KataTagBlockData
            {
                SH1 = "1",
                DK1 = "18",
                KC1 = "a150",
                DKKC1 = "Ø18a150",
                SH2 = "2",
                DK2 = "20",
                KC2 = "a200",
                DKKC2 = "Ø20a200"
            };

            var dict = KataBlockAttributeSerializer.WriteTagBlock(tagData);
            var restored = KataBlockAttributeSerializer.ReadTagBlock(dict);

            Assert.Equal(tagData.SH1, restored.SH1);
            Assert.Equal(tagData.DK1, restored.DK1);
            Assert.Equal(tagData.KC1, restored.KC1);
            Assert.Equal(tagData.DKKC1, restored.DKKC1);
            Assert.Equal(tagData.SH2, restored.SH2);
            Assert.Equal(tagData.DK2, restored.DK2);
            Assert.Equal(tagData.KC2, restored.KC2);
            Assert.Equal(tagData.DKKC2, restored.DKKC2);
        }

        [Fact]
        public void BeamPlanBlock_RoundTripSerialization_PreservesDimensions()
        {
            var planData = new KataBeamPlanBlockData
            {
                NameBeam = "DAM_D1",
                WidthMm = 250.0,
                HeightMm = 500.0,
                Level = "TANG_2",
                SpanMm = 6000.0
            };

            var dict = KataBlockAttributeSerializer.WriteBeamPlan(planData);
            var restored = KataBlockAttributeSerializer.ReadBeamPlan(dict);

            Assert.Equal(planData.NameBeam, restored.NameBeam);
            Assert.Equal(planData.WidthMm, restored.WidthMm);
            Assert.Equal(planData.HeightMm, restored.HeightMm);
            Assert.Equal(planData.Level, restored.Level);
            Assert.Equal(planData.SpanMm, restored.SpanMm);
        }

        [Fact]
        public void CouplerBlock_RoundTripSerialization_CalculatesTotalCount()
        {
            var couplerData = new KataCouplerBlockData
            {
                LengthMm = 90.0,
                DiameterMm = 25,
                MemberCount = 4,
                CouplersPerMember = 6,
                Notes = "Ren song song TCVN"
            };

            Assert.Equal(24, couplerData.TotalCouplers);

            var dict = KataBlockAttributeSerializer.WriteCoupler(couplerData);
            var restored = KataBlockAttributeSerializer.ReadCoupler(dict);

            Assert.Equal(couplerData.LengthMm, restored.LengthMm);
            Assert.Equal(couplerData.DiameterMm, restored.DiameterMm);
            Assert.Equal(couplerData.MemberCount, restored.MemberCount);
            Assert.Equal(couplerData.CouplersPerMember, restored.CouplersPerMember);
            Assert.Equal(24, restored.TotalCouplers);
            Assert.Equal(couplerData.Notes, restored.Notes);
        }

        [Fact]
        public void ReadMethods_NullOrEmptyDictionary_ReturnsDefaultObjects()
        {
            var tag = KataBlockAttributeSerializer.ReadTagBlock(null!);
            Assert.NotNull(tag);
            Assert.Equal(string.Empty, tag.SH1);

            var beam = KataBlockAttributeSerializer.ReadBeamPlan(new Dictionary<string, string>());
            Assert.NotNull(beam);
            Assert.Equal(0.0, beam.WidthMm);

            var coupler = KataBlockAttributeSerializer.ReadCoupler(null!);
            Assert.NotNull(coupler);
            Assert.Equal(80.0, coupler.LengthMm);
            Assert.Equal(20, coupler.DiameterMm);
        }

        [Fact]
        public void ReadMethods_CaseInsensitiveKeys_ReadsValues()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name_beam"] = "D_TEST",
                ["b"] = "300",
                ["h"] = "600",
                ["level"] = "T3",
                ["span"] = "5500"
            };

            var plan = KataBlockAttributeSerializer.ReadBeamPlan(dict);
            Assert.Equal("D_TEST", plan.NameBeam);
            Assert.Equal(300.0, plan.WidthMm);
            Assert.Equal(600.0, plan.HeightMm);
            Assert.Equal(5500.0, plan.SpanMm);
        }
    }
}
