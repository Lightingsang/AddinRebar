using System;
using HPKata.Core.Tagging;
using Xunit;

namespace HPKata.Core.Tests.Tagging
{
    public class QrTagGeneratorTests
    {
        [Fact]
        public void FormatPayload_PopulatedData_ReturnsPipeDelimitedKataString()
        {
            var p = new QrTagPayload
            {
                Project = "VINHOMES",
                Client = "VINGROUP",
                Order = "ORD-001",
                Component = "DAM_D1",
                Mark = "1",
                ShapeCode = "02",
                DiameterMm = 20,
                CutLengthMm = 2000.0,
                Quantity = 12,
                WeightKg = 59.3,
                SteelGrade = "CB400-V",
                SegmentsMm = new[] { 500.0, 1000.0, 0.0, 0.0, 0.0, 0.0 }
            };

            string payload = QrTagGenerator.FormatPayload(p);

            Assert.StartsWith("KATA|", payload);
            Assert.Contains("VINHOMES", payload);
            Assert.Contains("DAM_D1", payload);
            Assert.Contains("CB400-V", payload);
            Assert.Contains("500,1000,0,0,0,0", payload);

            string[] tokens = payload.Split('|');
            Assert.Equal(13, tokens.Length);
        }

        [Fact]
        public void ParsePayload_ValidPayload_ReconstructsModelFields()
        {
            string payload = "KATA|PROJECT_A|CLIENT_B|ORD_99|COT_C1|3|22|16|1200|50|18.5|CB300-T|200,400,0,0,0,0";

            var p = QrTagGenerator.ParsePayload(payload);

            Assert.Equal("PROJECT_A", p.Project);
            Assert.Equal("CLIENT_B", p.Client);
            Assert.Equal("ORD_99", p.Order);
            Assert.Equal("COT_C1", p.Component);
            Assert.Equal("3", p.Mark);
            Assert.Equal("22", p.ShapeCode);
            Assert.Equal(16, p.DiameterMm);
            Assert.Equal(1200.0, p.CutLengthMm);
            Assert.Equal(50, p.Quantity);
            Assert.Equal(18.5, p.WeightKg);
            Assert.Equal("CB300-T", p.SteelGrade);
            Assert.Equal(200.0, p.SegmentsMm[0]);
            Assert.Equal(400.0, p.SegmentsMm[1]);
        }

        [Fact]
        public void ParsePayload_CorruptedOrEmpty_ReturnsDefaultPayload()
        {
            var p1 = QrTagGenerator.ParsePayload("");
            var p2 = QrTagGenerator.ParsePayload("NOT_KATA|too|few|tokens");

            Assert.NotNull(p1);
            Assert.NotNull(p2);
            Assert.Equal(string.Empty, p1.Project);
            Assert.Equal(string.Empty, p2.Project);
        }

        [Fact]
        public void GeneratePngBytes_ValidPayload_GeneratesPngBarcode()
        {
            string payload = "KATA|PROJ|CLI|ORD|COMP|1|01|20|5000|10|123.5|CB400-V|5000,0,0,0,0,0";
            byte[] pngBytes = QrTagGenerator.GeneratePngBytes(payload);

            Assert.NotNull(pngBytes);
            Assert.True(pngBytes.Length > 64, "PNG image size must be greater than 64 bytes");

            // Verify PNG Magic Header: 89 50 4E 47 0D 0A 1A 0A
            byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngHeader.Length; i++)
            {
                Assert.Equal(pngHeader[i], pngBytes[i]);
            }
        }

        [Fact]
        public void GeneratePngBytes_NullOrEmptyPayload_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => QrTagGenerator.GeneratePngBytes(null!));
            Assert.Throws<ArgumentException>(() => QrTagGenerator.GeneratePngBytes(""));
        }
    }
}
