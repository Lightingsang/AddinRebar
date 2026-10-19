using System;
using System.Collections.Generic;
using HPKata.Core.DrawingLayout.Calculators;
using Xunit;

namespace HPKata.Core.Tests.DrawingLayout
{
    public class GridAxisGeneratorTests
    {
        [Fact]
        public void GetIsoLabel_StandardIndices_SkipsForbiddenLetters()
        {
            Assert.Equal("A", GridAxisGenerator.GetIsoLabel(0));
            Assert.Equal("H", GridAxisGenerator.GetIsoLabel(7));
            Assert.Equal("J", GridAxisGenerator.GetIsoLabel(8)); // Skips I
            Assert.Equal("P", GridAxisGenerator.GetIsoLabel(13)); // Skips O
            Assert.Equal("R", GridAxisGenerator.GetIsoLabel(14)); // Skips Q
        }

        [Fact]
        public void GenerateOrthogonal_CreatesCorrectAxesCountAndSpans()
        {
            var xSpans = new List<double> { 4000, 6000, 5000 };
            var ySpans = new List<double> { 5000, 6000 };

            var system = GridAxisGenerator.GenerateOrthogonal(xSpans, ySpans, 100.0);

            // 3 spans -> 4 vertical axes (1, 2, 3, 4)
            Assert.Equal(4, system.YAxes.Count);
            Assert.Equal("1", system.YAxes[0].Label);
            Assert.Equal("4", system.YAxes[3].Label);

            // 2 spans -> 3 horizontal axes (A, B, C)
            Assert.Equal(3, system.XAxes.Count);
            Assert.Equal("A", system.XAxes[0].Label);
            Assert.Equal("C", system.XAxes[2].Label);
        }

        [Fact]
        public void GenerateOrthogonal_ExtensionLengths_DerivedFromScale()
        {
            var xSpans = new List<double> { 5000 };
            var ySpans = new List<double> { 5000 };

            var sys100 = GridAxisGenerator.GenerateOrthogonal(xSpans, ySpans, 100.0);
            Assert.Equal(1500.0, sys100.ExtensionLengthMm);

            var sys50 = GridAxisGenerator.GenerateOrthogonal(xSpans, ySpans, 50.0);
            Assert.Equal(750.0, sys50.ExtensionLengthMm);
        }

        [Fact]
        public void GenerateRadialRay_CalculatesTrigCoordinates()
        {
            double angle = Math.PI / 4.0; // 45 deg
            var ray = GridAxisGenerator.GenerateRadialRay(0, 0, 1000, 5000, angle, "R1");

            Assert.Equal("R1", ray.Label);
            Assert.True(ray.IsCustomAngle);
            Assert.True(ray.StartX > 0);
            Assert.True(ray.StartY > 0);
            Assert.True(ray.EndX > ray.StartX);
            Assert.True(ray.EndY > ray.StartY);
        }

        [Fact]
        public void GenerateOrthogonal_NullSpans_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => GridAxisGenerator.GenerateOrthogonal(null!, new[] { 1000.0 }));
            Assert.Throws<ArgumentNullException>(() => GridAxisGenerator.GenerateOrthogonal(new[] { 1000.0 }, null!));
        }
    }
}
