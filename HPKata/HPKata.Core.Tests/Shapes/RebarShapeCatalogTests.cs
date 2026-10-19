using System;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.Core.Tests.Shapes
{
    public class RebarShapeCatalogTests
    {
        [Fact]
        public void All85ShapeTypes_AreRegisteredInCatalog()
        {
            var allShapes = (RebarShapeType[])Enum.GetValues(typeof(RebarShapeType));
            Assert.Equal(85, allShapes.Length);

            foreach (var shape in allShapes)
            {
                var def = RebarShapeCatalog.GetDefinition(shape);
                Assert.NotNull(def);
                Assert.Equal(shape, def.ShapeType);
                Assert.False(string.IsNullOrWhiteSpace(def.ShapeCode));
                Assert.False(string.IsNullOrWhiteSpace(def.Name));
                Assert.False(string.IsNullOrWhiteSpace(def.RevitShapeEquivalent));
                Assert.True(def.ParameterCount >= 0);
            }
        }

        [Theory]
        [InlineData("00", RebarShapeType.Dang00)]
        [InlineData("01", RebarShapeType.Dang01)]
        [InlineData("02a", RebarShapeType.Dang02a)]
        [InlineData("08a", RebarShapeType.Dang08a)]
        [InlineData("09a", RebarShapeType.Dang09a)]
        [InlineData("12a", RebarShapeType.Dang12a)]
        [InlineData("13a", RebarShapeType.Dang13a)]
        [InlineData("23a", RebarShapeType.Dang23a)]
        [InlineData("24a", RebarShapeType.Dang24a)]
        [InlineData("35a", RebarShapeType.Dang35a)]
        [InlineData("40a", RebarShapeType.Dang40a)]
        [InlineData("43a", RebarShapeType.Dang43a)]
        [InlineData("45a", RebarShapeType.Dang45a)]
        [InlineData("49a", RebarShapeType.Dang49a)]
        [InlineData("51a", RebarShapeType.Dang51a)]
        [InlineData("53a", RebarShapeType.Dang53a)]
        [InlineData("62a", RebarShapeType.Dang62a)]
        [InlineData("65a", RebarShapeType.Dang65a)]
        [InlineData("67", RebarShapeType.Dang67)]
        [InlineData("coupler", RebarShapeType.DangCoupler)]
        public void TryParseShapeCode_StandardAndVariantCodes_ReturnsExpected(string code, RebarShapeType expected)
        {
            bool success = RebarShapeCatalog.TryParseShapeCode(code, out var shapeType);
            Assert.True(success);
            Assert.Equal(expected, shapeType);
        }

        [Theory]
        [InlineData("dang01", RebarShapeType.Dang01)]
        [InlineData("DANG22", RebarShapeType.Dang22)]
        [InlineData("shape05", RebarShapeType.Dang05)]
        [InlineData("SHAPE67", RebarShapeType.Dang67)]
        [InlineData("5", RebarShapeType.Dang05)]
        [InlineData("0", RebarShapeType.Dang00)]
        [InlineData("67", RebarShapeType.Dang67)]
        public void TryParseShapeCode_PrefixesAndIntegers_ResolvesCorrectly(string code, RebarShapeType expected)
        {
            bool success = RebarShapeCatalog.TryParseShapeCode(code, out var shapeType);
            Assert.True(success);
            Assert.Equal(expected, shapeType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unknown_shape")]
        [InlineData("999")]
        public void TryParseShapeCode_InvalidCodes_ReturnsFalseAndDefault(string? code)
        {
            bool success = RebarShapeCatalog.TryParseShapeCode(code, out var shapeType);
            Assert.False(success);
            Assert.Equal(RebarShapeType.Dang01, shapeType);
        }

        [Fact]
        public void NormalizeShapeCode_ValidAndInvalid_NormalizesExpected()
        {
            Assert.Equal(RebarShapeType.Dang22, RebarShapeCatalog.NormalizeShapeCode("dang22"));
            Assert.Equal(RebarShapeType.Dang01, RebarShapeCatalog.NormalizeShapeCode("invalid_code"));
            Assert.Equal(RebarShapeType.Dang01, RebarShapeCatalog.NormalizeShapeCode(null));
        }

        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(30.0, 0.5)]
        [InlineData(90.0, 1.0)]
        [InlineData(180.0, 0.0)]
        [InlineData(360.0, 0.0)]
        public void SafeSin_KnownAngles_ReturnsAccurateValues(double angleDeg, double expectedSin)
        {
            double sinVal = RebarShapeCatalog.SafeSin(angleDeg);
            Assert.Equal(expectedSin, sinVal, 4);
        }

        [Fact]
        public void SafeSin_DegenerateTinyAngle_ClampsToZero()
        {
            double tinyAngle = 1e-5;
            double sinVal = RebarShapeCatalog.SafeSin(tinyAngle);
            Assert.Equal(0.0, sinVal);
        }

        [Fact]
        public void GetParam_WithinBoundsAndOutOfBounds_HandlesSafely()
        {
            double[] p = { 100.0, 200.0, 300.0 };
            Assert.Equal(100.0, RebarShapeCatalog.GetParam(p, 0));
            Assert.Equal(200.0, RebarShapeCatalog.GetParam(p, 1));
            Assert.Equal(300.0, RebarShapeCatalog.GetParam(p, 2));
            Assert.Equal(0.0, RebarShapeCatalog.GetParam(p, 3));
            Assert.Equal(0.0, RebarShapeCatalog.GetParam(p, -1));
        }

        [Fact]
        public void GetDefinition_UnknownType_FallsBackToDang01()
        {
            var def = RebarShapeCatalog.GetDefinition((RebarShapeType)9999);
            Assert.NotNull(def);
            Assert.Equal(RebarShapeType.Dang01, def.ShapeType);
        }
    }
}
