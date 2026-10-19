using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class BeamShopDrawingTests
    {
        private readonly RebarXDataCodec _codec = new RebarXDataCodec();

        [Fact]
        public void CadDraftingBuffer_AddPrimitives_TracksCountAndOrder()
        {
            var buffer = new CadDraftingBuffer();
            buffer.AddLine(new Point2D(0, 0), new Point2D(100, 0), "DCE_Beam", CadColor.CyanIndex);
            buffer.AddCircle(new Point2D(50, 50), 25.0, "DCE_Rebar", CadColor.RedIndex);
            buffer.AddText("D1", new Point2D(0, 100), 20.0, "DCE_Text");
            buffer.AddDimension(new Point2D(0, 0), new Point2D(100, 0), new Point2D(50, -20), "DCE_Dim");

            Assert.Equal(4, buffer.Count);
            Assert.Equal(CadEntityType.Line, buffer.Entities[0].EntityType);
            Assert.Equal(CadEntityType.Circle, buffer.Entities[1].EntityType);
            Assert.Equal(CadEntityType.Text, buffer.Entities[2].EntityType);
            Assert.Equal(CadEntityType.Dimension, buffer.Entities[3].EntityType);
        }

        [Fact]
        public void CadDraftingBuffer_TransformBy_AppliesTranslationAndScale()
        {
            var buffer = new CadDraftingBuffer();
            buffer.AddLine(new Point2D(0, 0), new Point2D(100, 0), "0");

            buffer.TransformBy(50.0, 100.0, 2.0);

            var line = buffer.Entities[0];
            Assert.Equal(50.0, line.StartPoint.X, 1);
            Assert.Equal(100.0, line.StartPoint.Y, 1);
            Assert.Equal(250.0, line.EndPoint.X, 1);
            Assert.Equal(100.0, line.EndPoint.Y, 1);

            var (bMin, bMax) = buffer.GetBoundingBox();
            Assert.Equal(50.0, bMin.X, 1);
            Assert.Equal(250.0, bMax.X, 1);
        }

        [Fact]
        public void CadEntityPrimitive_AttachedXData_SerializesAndPreservesValues()
        {
            var model = new RebarXdataDceModel
            {
                TenCauKien = "D1",
                SoHieu = "1",
                MaThep = "CB400-V",
                DuongKinh = "20",
                SoLuong = "2",
                LoaiThep = "CTop",
                DoanA = 250.0,
                DoanB = 5500.0,
                DoanC = 250.0,
                ChieuDaiL = 6000.0,
                HinhDang = 2
            };

            var packet = _codec.Encode(model);
            var primitive = new CadEntityPrimitive { EntityType = CadEntityType.Polyline };
            primitive.AttachXData(packet);

            Assert.Single(primitive.AttachedXData);
            Assert.True(_codec.TryDecode(primitive.AttachedXData[0], out var decoded));
            Assert.NotNull(decoded);
            Assert.Equal("D1", decoded!.TenCauKien);
            Assert.Equal("20", decoded.DuongKinh);
            Assert.Equal(6000.0, decoded.ChieuDaiL);
        }

        [Fact]
        public void BeamShopDrawingGenerator_GenerateShopDrawing_PopulatesLongitudinalContourAndRebar()
        {
            var beam = CreateSampleContinuousBeam();
            var gen = new BeamShopDrawingGenerator();

            var buffer = gen.GenerateShopDrawing(beam);

            Assert.True(buffer.Count > 10);
            var (bMin, bMax) = buffer.GetBoundingBox();
            Assert.True(bMax.X >= beam.TotalLengthMm);
        }

        [Fact]
        public void BeamShopDrawingGenerator_RebarPolylines_CarryValidRebarXdataDce()
        {
            var beam = CreateSampleContinuousBeam();
            var gen = new BeamShopDrawingGenerator();

            var buffer = gen.GenerateShopDrawing(beam);

            int rebarCountWithXData = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                var ent = buffer.Entities[i];
                if (ent.EntityType == CadEntityType.Polyline && ent.AttachedXData.Count > 0)
                {
                    if (string.Equals(ent.AttachedXData[0].RegAppName, RebarXDataCodec.ApplicationName, StringComparison.OrdinalIgnoreCase))
                    {
                        rebarCountWithXData++;
                        var model = _codec.Decode(ent.AttachedXData[0]);
                        Assert.Equal(beam.BeamMark, model.TenCauKien);
                    }
                }
            }

            Assert.True(rebarCountWithXData >= 4, $"Expected at least 4 rebar polylines with XData; found {rebarCountWithXData}.");
        }

        [Fact]
        public void BeamShopDrawingGenerator_CrossSections_GeneratesSections1And2()
        {
            var beam = CreateSampleContinuousBeam();
            var gen = new BeamShopDrawingGenerator();

            var buffer = gen.GenerateShopDrawing(beam, new BeamDetailingOptions { IncludeCrossSections = true });

            bool hasSection1Title = false;
            bool hasSection2Title = false;
            for (int i = 0; i < buffer.Count; i++)
            {
                var ent = buffer.Entities[i];
                if (ent.EntityType == CadEntityType.Text)
                {
                    if (ent.TextString.Contains("MẶT CẮT 1-1")) hasSection1Title = true;
                    if (ent.TextString.Contains("MẶT CẮT 2-2")) hasSection2Title = true;
                }
            }

            Assert.True(hasSection1Title, "Drawing should contain Section 1-1 text.");
            Assert.True(hasSection2Title, "Drawing should contain Section 2-2 text.");
        }

        [Fact]
        public void BeamShopDrawingGenerator_Annotations_IncludesLevelDatumAndDimensions()
        {
            var beam = CreateSampleContinuousBeam();
            var gen = new BeamShopDrawingGenerator();

            var buffer = gen.GenerateShopDrawing(beam, new BeamDetailingOptions { FloorElevationM = 3.60 });

            bool hasLevelDatum = false;
            int dimCount = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                var ent = buffer.Entities[i];
                if (ent.EntityType == CadEntityType.Text && ent.TextString.Contains("+3.600")) hasLevelDatum = true;
                if (ent.EntityType == CadEntityType.Dimension) dimCount++;
            }

            Assert.True(hasLevelDatum, "Drawing should include floor level symbol +3.600.");
            Assert.True(dimCount >= 2, $"Expected at least 2 dimensions; found {dimCount}.");
        }

        [Fact]
        public void CadDraftingBuffer_Clear_EmptiesAllEntities()
        {
            var buffer = new CadDraftingBuffer();
            buffer.AddLine(new Point2D(0, 0), new Point2D(10, 10));
            Assert.Equal(1, buffer.Count);

            buffer.Clear();
            Assert.Equal(0, buffer.Count);
        }

        private static ContinuousBeamResult CreateSampleContinuousBeam()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B20);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var spans = new List<BeamSpanInput>
            {
                new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 600, TopMomentRightKnm = 120, BottomMomentMidKnm = 90 },
                new BeamSpanInput { SpanIndex = 1, ClearSpanMm = 6000, SectionWidthMm = 300, SectionHeightMm = 600, TopMomentLeftKnm = 120, BottomMomentMidKnm = 90 }
            };
            return ContinuousBeamCalculator.Calculate(new ContinuousBeamInput("D1", concrete, steel, spans));
        }
    }
}
