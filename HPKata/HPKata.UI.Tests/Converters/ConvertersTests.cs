using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using HPKata.Core.Optimization;
using HPKata.UI.Converters;
using Xunit;

namespace HPKata.UI.Tests.Converters
{
    public class ConvertersTests
    {
        [Fact]
        public void BoolToVisibilityConverter_ConvertsAccurately()
        {
            var conv = new BoolToVisibilityConverter();
            Assert.Equal(Visibility.Visible, conv.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, conv.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture));

            conv.Invert = true;
            Assert.Equal(Visibility.Collapsed, conv.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Visible, conv.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void NullToVisibilityConverter_ConvertsAccurately()
        {
            var conv = new NullToVisibilityConverter();
            Assert.Equal(Visibility.Collapsed, conv.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, conv.Convert("", typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Visible, conv.Convert("Sample", typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Visible, conv.Convert(123, typeof(Visibility), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void EnumDescriptionConverter_FormatsHeuristics()
        {
            var conv = new EnumDescriptionConverter();
            var autoStr = conv.Convert(SolverStrategy.Auto, typeof(string), null, CultureInfo.InvariantCulture) as string;
            var duBang0Str = conv.Convert(SolverStrategy.DuBang0, typeof(string), null, CultureInfo.InvariantCulture) as string;

            Assert.Contains("Auto", autoStr);
            Assert.Contains("DuBang0", duBang0Str);
        }

        [Fact]
        public void NumberAndPercentageConverters_FormatCorrectly()
        {
            var numConv = new NumberFormatConverter { Format = "N0" };
            Assert.Equal("11,700", numConv.Convert(11700.0, typeof(string), null, CultureInfo.InvariantCulture));

            var pctConv = new PercentageConverter();
            Assert.Equal("2.45%", pctConv.Convert(2.451, typeof(string), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void CutsFormattingConverter_JoinsCutsWithPlus()
        {
            var conv = new CutsFormattingConverter();
            var cuts = new List<double> { 5850.0, 3900.0, 1950.0 };
            var result = conv.Convert(cuts, typeof(string), null, CultureInfo.InvariantCulture) as string;

            Assert.Equal("5850 + 3900 + 1950", result);
        }
    }
}
