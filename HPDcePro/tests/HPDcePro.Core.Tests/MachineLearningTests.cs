using System;
using System.IO;
using System.Threading.Tasks;
using HPDcePro.Core.Common;
using HPDcePro.Core.MachineLearning;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class MachineLearningTests
    {
        private static string RealDbPath => AssetPathResolver.ResolveMachineLearningDatabase();

        [Fact]
        public void RebarTextTokenizer_NormalizesGlyphsCorrectly()
        {
            Assert.Equal("2∅18", RebarTextTokenizer.Normalize("2d18"));
            Assert.Equal("3∅20", RebarTextTokenizer.Normalize("3\u03a620"));
            Assert.Equal("∅8a150", RebarTextTokenizer.Normalize("%%c8a150"));
            Assert.Equal("4∅25", RebarTextTokenizer.Normalize("4\u220525"));
        }

        [Fact]
        public void RebarTextTokenizer_TokenizesAndExtractsFeatures()
        {
            string callout = "[1b] 3x∅20, L= 11700";
            var tokens = RebarTextTokenizer.Tokenize(callout);

            Assert.Equal(4, tokens.Count);

            // Token 0: 1 -> SH
            Assert.Equal(1, tokens[0].NumericValue);
            Assert.Equal(RebarTokenType.SH, tokens[0].PredictedType);
            Assert.Contains("INRING_1", tokens[0].FeatureString);

            // Token 1: 3 -> SL
            Assert.Equal(3, tokens[1].NumericValue);
            Assert.Equal(RebarTokenType.SL, tokens[1].PredictedType);
            Assert.Contains("NEXT_x", tokens[1].FeatureString);

            // Token 2: 20 -> DK
            Assert.Equal(20, tokens[2].NumericValue);
            Assert.Equal(RebarTokenType.DK, tokens[2].PredictedType);
            Assert.Contains("AFTERD_1", tokens[2].FeatureString);

            // Token 3: 11700 -> L
            Assert.Equal(11700, tokens[3].NumericValue);
            Assert.Equal(RebarTokenType.L, tokens[3].PredictedType);
            Assert.Contains("AFTERL_1", tokens[3].FeatureString);
        }

        [Fact]
        public void RebarTextTokenizer_ParseCallout_ProducesStructuredOutput()
        {
            var callout = RebarTextTokenizer.ParseCallout("[2] 4d22 L=5400");
            Assert.Equal("2", callout.BarMark);
            Assert.Equal(4, callout.Quantity);
            Assert.Equal(22, callout.Diameter);
            Assert.Equal(5400.0, callout.Length);

            var stirrup = RebarTextTokenizer.ParseCallout("d8a150");
            Assert.Equal(8, stirrup.Diameter);
            Assert.Equal(150, stirrup.Spacing);
        }

        [Fact]
        public async Task MauHocDatabaseReader_ReadsRealMauHocDb_Accurately()
        {
            if (!File.Exists(RealDbPath))
            {
                return;
            }

            using (var reader = new MauHocDatabaseReader(RealDbPath))
            {
                var ct = TestContext.Current.CancellationToken;
                int totalCount = await reader.GetSampleCountAsync(ct);
                Assert.Equal(45020, totalCount);

                var samples = await reader.GetSamplesAsync(limit: 5, cancellationToken: ct);
                Assert.Equal(5, samples.Count);
                Assert.NotEmpty(samples[0].TextGoc);
                Assert.NotEmpty(samples[0].Nhan);
                Assert.NotEmpty(samples[0].Feature);

                var fixes = await reader.GetLichSuSuaAsync(ct);
                Assert.NotNull(fixes);
                Assert.NotEmpty(fixes);

                var models = await reader.GetCauHinhModelsAsync(ct);
                Assert.NotNull(models);
                Assert.NotEmpty(models);
            }
        }
    }
}
