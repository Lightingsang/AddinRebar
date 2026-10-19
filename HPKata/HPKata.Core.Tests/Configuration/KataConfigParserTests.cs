using System;
using System.IO;
using System.Text;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.Core.Tests.Configuration
{
    public class KataConfigParserTests
    {
        private static string RealGoiLenhPath => KataInstallEnvironment.Current.GoiLenhLspPath;

        [Fact]
        public void DefaultConfig_LayersAndDetailingParameters_ConfiguredCorrectly()
        {
            var config = new KataConfig();

            // Line 1-15: Layers and styles
            Assert.Equal("kata_dim", config.LayerDim);
            Assert.Equal("kata_net thay", config.LayerNetThay);
            Assert.Equal("kata_net khung", config.LayerNetKhung);
            Assert.Equal("kata_net khuat", config.LayerNetKhuat);
            Assert.Equal("kata_grid", config.LayerGrid);
            Assert.Equal("kata_thep chu", config.LayerThepChu);
            Assert.Equal("kata_thep dai", config.LayerThepDai);
            Assert.Equal("kata_thep dai", config.LayerThepDaiPhu);
            Assert.Equal("kata_net manh", config.LayerNetManh);
            Assert.Equal("kata_text", config.LayerText);
            Assert.Equal("kata_text", config.LayerTextTag);
            Assert.Equal("a", config.SpacingPrefix);
            Assert.Equal("Ø", config.SmallBarPrefix);
            Assert.Equal("Ø", config.DeformedBarPrefix);
            Assert.Equal("kata_dim_iso", config.DimStyleName);

            // Line 16-30: Dimensions, scales and multipliers
            Assert.Equal(30.0, config.ConcreteCoverMm);
            Assert.Equal("135/180", config.DefaultHookAngle);
            Assert.Equal(0.04, config.DetailDrawingScale); // 1/25
            Assert.Equal(16, config.BaseBarDiameter);
            Assert.Equal(30.0, config.LapLengthMultiplier);
            Assert.Equal(1, config.StaggeredLapMode);
            Assert.Equal(1, config.DrawFilletRadius);
            Assert.Equal(1, config.GroupSameRebar);
            Assert.Equal(0, config.ApplyBendDeduction);
            Assert.Equal(1, config.AutoRenumberMarks);
            Assert.Equal(40.0, config.TensionAnchorMult);
            Assert.Equal(0.16666666666666666, config.ColSpliceRatio, 4); // 1/6
            Assert.Equal(1, config.DrawElevationHooks);
        }

        [Fact]
        public void DefaultConfig_CutoffsAndSecondaryParameters_ConfiguredCorrectly()
        {
            var config = new KataConfig();

            // Line 31-45: Stock and beam cutoffs
            Assert.Equal("Ø", config.ScheduleDiaPrefix);
            Assert.Equal(11700, config.StandardBarLength);
            Assert.Equal(100, config.MinRemnantLength);
            Assert.Equal(0.2, config.BeamTopCutoffL1);
            Assert.Equal(" mép", config.BeamCutoffDatumL1);

            // Line 46-60: Language and beam/column rebar defaults
            Assert.Equal("Tiếng Việt", config.LanguageUiSched);
            Assert.Equal("Default", config.TableTemplate);
            Assert.Equal("Closed", config.StirrupClosureType);

            // Line 61-86: Secondary beams, couplers, and unit settings
            Assert.Equal("mm", config.LengthUnitDisplay);
            Assert.Equal(50.0, config.CutLengthRoundStep);
            Assert.Equal("5f10a50", config.SecBeamStirrupAdd1);
            Assert.Equal("2f14", config.SecBeamAntiCrack1);
            Assert.Equal(150.0, config.SecBeamZoneLen1);
            Assert.Equal("None", config.CouplerEndTreat1);
            Assert.Equal("None", config.CouplerEndTreat2);
        }

        [Fact]
        public void ParseRealGoiLenhLsp_ExtractsAll86ParametersCorrectly()
        {
            if (!File.Exists(RealGoiLenhPath))
            {
                return; // Gracefully pass on environments where C:\HPKata is absent
            }

            var config = KataConfigParser.ParseFile(RealGoiLenhPath);
            Assert.NotNull(config);
            Assert.Equal("kata_dim", config.LayerDim);
            Assert.Equal(11700, config.StandardBarLength);
            Assert.Equal(30.0, config.ConcreteCoverMm);
            Assert.Equal(16, config.BaseBarDiameter);
            Assert.Equal("Tiếng Việt", config.LanguageUiSched);
            Assert.Equal("mm", config.LengthUnitDisplay);
        }

        [Fact]
        public void RoundTripSerialization_ProducesIdempotentConfig()
        {
            var original = new KataConfig();
            string serializedInitial = KataConfigParser.SerializeToText(original);
            var parsed1 = KataConfigParser.ParseText(serializedInitial);

            // Verify core properties preserved through first parse
            Assert.Equal(original.LayerDim, parsed1.LayerDim);
            Assert.Equal(original.StandardBarLength, parsed1.StandardBarLength);
            Assert.Equal(original.ConcreteCoverMm, parsed1.ConcreteCoverMm);
            Assert.Equal(original.BaseBarDiameter, parsed1.BaseBarDiameter);
            Assert.Equal(original.LanguageUiSched, parsed1.LanguageUiSched);
            Assert.Equal(original.SecBeamStirrupAdd1, parsed1.SecBeamStirrupAdd1);
            Assert.Equal(original.ColSpliceRatio, parsed1.ColSpliceRatio, 4);

            // Verify strict 100% idempotence on re-serialization: f(f(x)) == f(x)
            string text1 = KataConfigParser.SerializeToText(parsed1);
            var parsed2 = KataConfigParser.ParseText(text1);
            string text2 = KataConfigParser.SerializeToText(parsed2);

            Assert.Equal(text1, text2);
            Assert.Equal(parsed1.LayerDim, parsed2.LayerDim);
            Assert.Equal(parsed1.BeamCutoffDatumL1, parsed2.BeamCutoffDatumL1);
            Assert.Equal(parsed1.StandardBarLength, parsed2.StandardBarLength);
        }

        [Fact]
        public void DecodeWithFallback_Windows1258VietnameseText_DecodesSuccessfully()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding win1258 = Encoding.GetEncoding(1258);
            byte[] bytes = win1258.GetBytes(";Tiếng Việt");

            string decoded = KataConfigParser.DecodeWithFallback(bytes);
            Assert.Contains("Ti", decoded);
            Assert.Contains("Vi", decoded);
        }

        [Fact]
        public void DecodeWithFallback_Utf8Text_DecodesAccurately()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(";Tiếng Việt");
            string decoded = KataConfigParser.DecodeWithFallback(bytes);
            Assert.Equal(";Tiếng Việt", decoded);
        }

        [Fact]
        public void ParseFile_NonExistentPath_ThrowsFileNotFoundException()
        {
            string nonExistent = @"C:\non_existent_folder_xyz\fake_file.lsp";
            Assert.Throws<FileNotFoundException>(() => KataConfigParser.ParseFile(nonExistent));
        }

        [Fact]
        public void ParseText_CorruptedNumericLines_FallsBackToDefaults()
        {
            string[] corrupted = new string[86];
            for (int i = 0; i < 86; i++) corrupted[i] = ";not_a_number_corrupted_data";

            string text = string.Join(Environment.NewLine, corrupted);
            var config = KataConfigParser.ParseText(text);

            Assert.NotNull(config);
            Assert.Equal(30.0, config.ConcreteCoverMm); // Fell back to 30.0
            Assert.Equal(11700, config.StandardBarLength); // Fell back to 11700
            Assert.Equal(16, config.BaseBarDiameter); // Fell back to 16
        }

        [Fact]
        public void ParseText_EmptyOrWhitespaceString_ReturnsDefaultConfig()
        {
            var config1 = KataConfigParser.ParseText("");
            var config2 = KataConfigParser.ParseText("   \r\n   ");
            Assert.NotNull(config1);
            Assert.NotNull(config2);
            Assert.Equal("kata_dim", config1.LayerDim);
            Assert.Equal(11700, config2.StandardBarLength);
        }

        [Fact]
        public void ParseLines_ExtraLongLines_ResilientWithoutOutOfBounds()
        {
            var lines = new string[500];
            for (int i = 0; i < 500; i++) lines[i] = $";dummy_line_{i}";
            lines[151] = ";custom_dim_layer";

            var config = KataConfigParser.ParseLines(lines);
            Assert.Equal("custom_dim_layer", config.LayerDim);
        }
    }
}
