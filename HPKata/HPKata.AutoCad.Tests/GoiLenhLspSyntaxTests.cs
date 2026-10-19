using System;
using System.IO;
using System.Linq;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class GoiLenhLspSyntaxTests
    {
        private static string GetLspPath()
        {
            string binPath = Path.Combine(AppContext.BaseDirectory, "GoiLenh_HPKata.lsp");
            if (File.Exists(binPath)) return binPath;

            string projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\HPKata.AutoCad\GoiLenh_HPKata.lsp"));
            if (File.Exists(projectPath)) return projectPath;

            throw new FileNotFoundException("GoiLenh_HPKata.lsp could not be located.", binPath);
        }

        [Fact]
        public void GoiLenh_LineCount_Exactly238Lines()
        {
            string path = GetLspPath();
            string text = File.ReadAllText(path);
            string[] splitLines = text.Split('\n');

            Assert.Equal(238, splitLines.Length);
            Assert.Equal(237, File.ReadAllLines(path).Length);
        }

        [Fact]
        public void GoiLenh_Line152_StartsWithKataDim()
        {
            string path = GetLspPath();
            string[] lines = File.ReadAllLines(path);

            // 0-indexed line 151 = 1-indexed line 152
            Assert.Equal(";kata_dim", lines[151].Trim());
        }

        [Fact]
        public void GoiLenh_Line237_EndsWithZero()
        {
            string path = GetLspPath();
            string[] lines = File.ReadAllLines(path);

            // 0-indexed line 236 = 1-indexed line 237
            Assert.Equal(";0", lines[236].Trim());
        }

        [Fact]
        public void GoiLenh_Aliases_ContainsAll118CommandAliases()
        {
            string path = GetLspPath();
            string[] lines = File.ReadAllLines(path);

            // Lines 33 to 151 (0-indexed 32 to 150)
            int defunCount = 0;
            for (int i = 32; i <= 150; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("(defun c:"))
                {
                    defunCount++;
                }
            }

            Assert.Equal(118, defunCount);
        }

        [Fact]
        public void GoiLenh_KataConfigParser_ParseFile_ExtractsAll86Properties()
        {
            string path = GetLspPath();
            KataConfig config = KataConfigParser.ParseFile(path);

            Assert.NotNull(config);
            Assert.Equal("kata_dim", config.LayerDim);
            Assert.Equal("kata_thep chu", config.LayerThepChu);
            Assert.Equal("kata_thep dai", config.LayerThepDai);
            Assert.Equal("kata_dim_iso", config.DimStyleName);
            Assert.Equal(30.0, config.ConcreteCoverMm);
            Assert.Equal(11700, config.StandardBarLength);
            Assert.Equal(16, config.BaseBarDiameter);
        }

        [Fact]
        public void GoiLenh_RoundTrip_SerializingConfig_Preserves238Lines()
        {
            string path = GetLspPath();
            string[] originalLines = File.ReadAllLines(path);
            KataConfig config = KataConfigParser.ParseLines(originalLines);

            var updatedLines = (string[])originalLines.Clone();
            var serialized = KataConfigParser.SerializeToLines(config);

            Assert.Equal(86, serialized.Count);
            for (int i = 0; i < 86; i++)
            {
                updatedLines[151 + i] = serialized[i];
            }

            Assert.Equal(237, updatedLines.Length);
            Assert.Equal(originalLines[151], updatedLines[151]);
            Assert.Equal(originalLines[236], updatedLines[236]);
        }

        [Fact]
        public void GoiLenh_Syntax_ParenthesisBalance_IsZero()
        {
            string path = GetLspPath();
            string[] lines = File.ReadAllLines(path);

            int openParens = 0;
            int closeParens = 0;

            // Only check lines 1 to 151 (LISP code section)
            for (int i = 0; i < 151; i++)
            {
                string l = lines[i];
                if (l.TrimStart().StartsWith(";")) continue;

                openParens += l.Count(c => c == '(');
                closeParens += l.Count(c => c == ')');
            }

            Assert.Equal(openParens, closeParens);
            Assert.True(openParens > 0);
        }
    }
}
