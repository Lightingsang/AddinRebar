using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPDcePro.Core.Common;
using HPDcePro.Core.MachineLearning;
using Xunit;

namespace HPDcePro.Core.Tests
{
    [Collection("AssetPathTests")]
    public class MauHocDatabaseLiveTests
    {
        private static string RealDbPath => AssetPathResolver.ResolveMachineLearningDatabase();

        [Fact]
        public async Task MauHocDatabase_LiveRead_VerifiesExactly45020Samples()
        {
            if (!File.Exists(RealDbPath))
            {
                return;
            }

            using var reader = new MauHocDatabaseReader(RealDbPath);
            var ct = TestContext.Current.CancellationToken;
            int count = await reader.GetSampleCountAsync(ct);

            Assert.Equal(45020, count);
        }

        [Fact]
        public async Task MauHocDatabase_LiveRead_VerifiesAll9DistinctLabels()
        {
            if (!File.Exists(RealDbPath))
            {
                return;
            }

            using var reader = new MauHocDatabaseReader(RealDbPath);
            var ct = TestContext.Current.CancellationToken;
            var distinctLabels = await reader.GetDistinctLabelsAsync(ct);

            Assert.Equal(9, distinctLabels.Count);

            var expected = new HashSet<string> { "BO", "DK", "KC1", "KC2", "KC3", "KC4", "L", "SH", "SL" };
            foreach (var label in expected)
            {
                Assert.Contains(label, distinctLabels);
            }
        }

        [Fact]
        public async Task MauHocDatabase_LiveRead_VerifiesLichSuSuaAndCauHinhModels()
        {
            if (!File.Exists(RealDbPath))
            {
                return;
            }

            using var reader = new MauHocDatabaseReader(RealDbPath);
            var ct = TestContext.Current.CancellationToken;

            var history = await reader.GetLichSuSuaAsync(ct);
            Assert.NotNull(history);
            Assert.NotEmpty(history);

            var models = await reader.GetCauHinhModelsAsync(ct);
            Assert.NotNull(models);
            Assert.NotEmpty(models);
        }

        [Fact]
        public void RebarTextTokenizer_ComplexVietnameseExpression_2D20Plus1D18()
        {
            string expression = "2D20+1D18";
            var tokens = RebarTextTokenizer.Tokenize(expression);

            Assert.Equal(4, tokens.Count);

            Assert.Equal(2, tokens[0].NumericValue);
            Assert.Equal(RebarTokenType.SL, tokens[0].PredictedType);

            Assert.Equal(20, tokens[1].NumericValue);
            Assert.Equal(RebarTokenType.DK, tokens[1].PredictedType);

            Assert.Equal(1, tokens[2].NumericValue);
            Assert.Equal(RebarTokenType.SL, tokens[2].PredictedType);

            Assert.Equal(18, tokens[3].NumericValue);
            Assert.Equal(RebarTokenType.DK, tokens[3].PredictedType);
        }

        [Fact]
        public void RebarTextTokenizer_SpacingExpression_A200Slash100()
        {
            string expression = "a200/100";
            var tokens = RebarTextTokenizer.Tokenize(expression);

            Assert.Equal(2, tokens.Count);

            Assert.Equal(200, tokens[0].NumericValue);
            Assert.Equal(RebarTokenType.KC, tokens[0].PredictedType);

            Assert.Equal(100, tokens[1].NumericValue);
            Assert.Equal(RebarTokenType.KC, tokens[1].PredictedType);
        }

        [Fact]
        public void RebarTextTokenizer_SpacingExpression_Cc150()
        {
            string expression = "c/c 150";
            var tokens = RebarTextTokenizer.Tokenize(expression);

            Assert.Single(tokens);
            Assert.Equal(150, tokens[0].NumericValue);
            Assert.Equal(RebarTokenType.KC, tokens[0].PredictedType);
        }

        [Fact]
        public void RebarTextTokenizer_ComplexCallout_WithLettersAndBrackets()
        {
            string callout = "[3a] 4∅22 a150 L=6500";
            var parsed = RebarTextTokenizer.ParseCallout(callout);

            Assert.Equal("3", parsed.BarMark);
            Assert.Equal(4, parsed.Quantity);
            Assert.Equal(22, parsed.Diameter);
            Assert.Equal(150, parsed.Spacing);
            Assert.Equal(6500.0, parsed.Length);
        }
    }
}
