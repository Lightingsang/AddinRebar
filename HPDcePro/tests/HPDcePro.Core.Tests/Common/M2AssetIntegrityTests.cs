using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HPDcePro.Core.Common;
using HPDcePro.Core.MachineLearning;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HPDcePro.Core.Tests.Common
{
    [Collection("AssetPathTests")]
    public class M2AssetIntegrityTests
    {
        [Fact]
        public async Task MauHocDatabase_LiveQuery_VerifiesAllTablesAndSchemasWithoutThrowing()
        {
            string dbPath = AssetPathResolver.ResolveMachineLearningDatabase();
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly
            };

            using var conn = new SqliteConnection(builder.ConnectionString);
            await conn.OpenAsync();

            // Verify tables
            var tables = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            Assert.Contains("MauHoc", tables);
            Assert.Contains("sqlite_sequence", tables);
            Assert.Contains("LichSuSua", tables);
            Assert.Contains("CauHinhModel", tables);

            // Verify counts
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM MauHoc;";
                long sampleCount = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
                Assert.Equal(45020L, sampleCount);

                cmd.CommandText = "SELECT COUNT(*) FROM LichSuSua;";
                long fixCount = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
                Assert.Equal(3L, fixCount);

                cmd.CommandText = "SELECT COUNT(*) FROM CauHinhModel;";
                long modelCount = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
                Assert.Equal(10L, modelCount);
            }
        }

        [Fact]
        public void ExcelTemplates_OpenViaClosedXml_VerifiesWorksheetsAndHeaders()
        {
            string menuPath = AssetPathResolver.ResolveExcelWorkbook("Menu DCE_2020.xlsm");
            using (var wb = new XLWorkbook(menuPath))
            {
                Assert.True(wb.Worksheets.Count >= 1, "Menu DCE_2020.xlsm must have worksheets.");
                Assert.NotNull(wb.Worksheet("Main"));
            }

            string steelLoadingPath = AssetPathResolver.ResolveExcelWorkbook("DceSteel/01 TCVN 2737 2023 Loading.xlsm");
            using (var wb = new XLWorkbook(steelLoadingPath))
            {
                Assert.True(wb.Worksheets.Count >= 3, "01 TCVN 2737 2023 Loading.xlsm must have at least 3 worksheets.");
                Assert.NotNull(wb.Worksheet("Input"));
                Assert.NotNull(wb.Worksheet("Loading"));
                Assert.NotNull(wb.Worksheet("Combo"));
            }

            string beamPath = AssetPathResolver.ResolveExcelWorkbook("File V.2020/09. DCE_Beam_2020.xlsm");
            using (var wb = new XLWorkbook(beamPath))
            {
                Assert.True(wb.Worksheets.Count >= 1, "09. DCE_Beam_2020.xlsm must load cleanly.");
            }
        }

        [Fact]
        public void StaticGrep_ZeroDceProHardcodedPaths_AcrossSourceAndAssets()
        {
            string assetsRoot = AssetPathResolver.GetAssetsRoot();
            DirectoryInfo? hpDceProDir = Directory.GetParent(assetsRoot);
            Assert.NotNull(hpDceProDir);

            string srcDir = Path.Combine(hpDceProDir.FullName, "src");
            var pattern = new Regex(@"C:(?:\\\\|\\|/)Dce Pro", RegexOptions.IgnoreCase);
            var violations = new List<string>();

            ScanDirectoryForPattern(srcDir, pattern, violations);
            ScanDirectoryForPattern(assetsRoot, pattern, violations);

            Assert.Empty(violations);
        }

        private static void ScanDirectoryForPattern(string dir, Regex pattern, List<string> violations)
        {
            if (!Directory.Exists(dir))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) ||
                    file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".cs" or ".xaml" or ".json" or ".xml" or ".config" or ".txt" or ".lsp" or ".autostr")
                {
                    int lineNo = 1;
                    foreach (string line in File.ReadLines(file))
                    {
                        if (pattern.IsMatch(line))
                        {
                            violations.Add($"{Path.GetFileName(file)}:{lineNo} -> {line.Trim()}");
                        }
                        lineNo++;
                    }
                }
            }
        }
    }
}
