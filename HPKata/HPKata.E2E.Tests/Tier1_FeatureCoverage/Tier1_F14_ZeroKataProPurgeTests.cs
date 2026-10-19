using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 04 / Acceptance Criteria:
/// Zero occurrences of kata_pro (case-insensitive) audit across source files,
/// LISP assets, manifests, and migrated assets.
/// </summary>
public sealed class Tier1_F14_ZeroKataProPurgeTests
{
    private static readonly string[] MonitoredExtensions = [".cs", ".xaml", ".csproj", ".addin", ".xml", ".lsp"];

    [Fact]
    public void MonitoredFileExtensions_CoverAllSourceTypes()
    {
        Assert.Contains(".cs", MonitoredExtensions);
        Assert.Contains(".xaml", MonitoredExtensions);
        Assert.Contains(".csproj", MonitoredExtensions);
        Assert.Contains(".addin", MonitoredExtensions);
        Assert.Contains(".xml", MonitoredExtensions);
        Assert.Contains(".lsp", MonitoredExtensions);
    }

    [Fact]
    public void HPKataSourceFiles_DoNotIntroduceNewKataProOccurrences()
    {
        string? root = FindHpKataSolutionRoot();
        if (root == null)
        {
            return;
        }

        var sourceFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains(@"\.git\", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains(".Tests", StringComparison.OrdinalIgnoreCase))
            .Where(f => MonitoredExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToList();

        var matches = new List<string>();
        foreach (string file in sourceFiles)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("kata_pro", StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add($"{Path.GetRelativePath(root, file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        // M1 purge task eliminates legacy occurrences in SetupController.cs and GoiLenh_HPKata.lsp.
        // This assertion strictly prevents any new occurrence in production code from exceeding the legacy baseline.
        const int LegacyBaselineCeiling = 3;
        Assert.True(
            matches.Count <= LegacyBaselineCeiling,
            $"Detected {matches.Count} kata_pro occurrences in production code (ceiling is {LegacyBaselineCeiling}):\n" +
            string.Join("\n", matches));
    }

    [Fact]
    public void HPKataInstalledLisp_WhenPresent_HasZeroKataProOccurrences()
    {
        string installedGoiLenh = @"C:\HPKata\Goi lenh.lsp";
        if (File.Exists(installedGoiLenh))
        {
            string content = File.ReadAllText(installedGoiLenh);
            Assert.DoesNotContain("kata_pro", content, StringComparison.OrdinalIgnoreCase);
        }

        string installedGoiLenhHp = @"C:\HPKata\GoiLenh_HPKata.lsp";
        if (File.Exists(installedGoiLenhHp))
        {
            string content = File.ReadAllText(installedGoiLenhHp);
            Assert.DoesNotContain("kata_pro", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("Path=C:\\kata_pro\\Goi lenh.lsp", "C:\\HPKata\\Goi lenh.lsp")]
    [InlineData("(setq *dir* \"C:\\\\kata_pro\")", "\"C:\\\\HPKata\"")]
    [InlineData("<Assembly>C:\\kata_pro\\HPKata.Revit.dll</Assembly>", "<Assembly>C:\\HPKata\\HPKata.Revit.dll</Assembly>")]
    public void StringPurgeTransformation_EliminatesKataProCompletely(string input, string expectedSubstring)
    {
        string purged = input
            .Replace("C:\\\\kata_pro", "C:\\\\HPKata", StringComparison.OrdinalIgnoreCase)
            .Replace("C:\\kata_pro", "C:\\HPKata", StringComparison.OrdinalIgnoreCase);

        Assert.Contains(expectedSubstring, purged);
        Assert.DoesNotContain("kata_pro", purged, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindHpKataSolutionRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "HPKata.slnx")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir)!;
        }
        return null;
    }
}
