using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace HPKata.Core.Tests.Configuration;

/// <summary>
/// Static analysis test enforcing zero occurrences of legacy branding
/// across all source code (.cs), UI markup (.xaml), AutoLISP (.lsp), project files (.csproj),
/// configuration/manifests (.xml, .addin), and test scripts (.ps1) in HPKata.
/// Satisfies Feature 4 and Acceptance Criteria R1.3.
/// </summary>
public sealed class CodebasePurgeIntegrityTests
{
    private static readonly HashSet<string> TargetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".xaml",
        ".lsp",
        ".csproj",
        ".xml",
        ".addin",
        ".ps1"
    };

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".vs",
        ".git",
        "TestResults",
        ".idea",
        "Distribution"
    };

    private static readonly string ForbiddenPattern = "kata" + "_pro";

    [Fact]
    public void ProductionCodebase_ZeroOccurrencesOfLegacyPattern_EnforcedAcrossAllSourceFiles()
    {
        string hpKataRoot = LocateHpKataRootDirectory();
        Assert.True(Directory.Exists(hpKataRoot), $"HPKata root directory not found: {hpKataRoot}");

        var violations = new List<string>();
        var files = GetSourceFiles(hpKataRoot);

        foreach (string file in files)
        {
            ScanFileForViolations(file, hpKataRoot, violations);
        }

        Assert.True(
            violations.Count == 0,
            $"Detected {violations.Count} forbidden occurrence(s) in HPKata source files:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Feature4_PurgedTargets_HaveZeroOccurrencesOfLegacyPattern()
    {
        string hpKataRoot = LocateHpKataRootDirectory();
        string[] targetFiles =
        [
            Path.Combine(hpKataRoot, "HPKata.AutoCad", "Commands", "SetupController.cs"),
            Path.Combine(hpKataRoot, "HPKata.AutoCad", "GoiLenh_HPKata.lsp"),
            Path.Combine(hpKataRoot, "HPKata.Core.Tests", "Configuration", "KataConfigParserTests.cs"),
            Path.Combine(hpKataRoot, "tools", "run_m3_empirical_challenge_tests.ps1"),
            Path.Combine(hpKataRoot, "tools", "run_m2_empirical_tests.ps1")
        ];

        var violations = new List<string>();
        foreach (string file in targetFiles)
        {
            if (File.Exists(file))
            {
                ScanFileForViolations(file, hpKataRoot, violations);
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Detected {violations.Count} forbidden occurrence(s) in Feature 4 targets:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void InstalledGoiLenhLsp_WhenPresent_HasZeroOccurrencesOfLegacyPattern()
    {
        string installedLsp = @"C:\HPKata\Goi lenh.lsp";
        if (File.Exists(installedLsp))
        {
            string content = File.ReadAllText(installedLsp);
            Assert.DoesNotContain(ForbiddenPattern, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string LocateHpKataRootDirectory()
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "HPKata.slnx")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException(
            $"Unable to locate HPKata root directory containing HPKata.slnx starting from '{AppContext.BaseDirectory}'.");
    }

    private static IEnumerable<string> GetSourceFiles(string rootDir)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        return Directory.EnumerateFiles(rootDir, "*.*", options)
            .Where(file =>
            {
                string ext = Path.GetExtension(file);
                if (!TargetExtensions.Contains(ext)) return false;

                string relative = Path.GetRelativePath(rootDir, file);
                string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (parts.Any(p => ExcludedDirectories.Contains(p) || p.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                return true;
            });
    }

    private static void ScanFileForViolations(string filePath, string rootDir, List<string> violations)
    {
        string relativePath = Path.GetRelativePath(rootDir, filePath);
        string[] lines = File.ReadAllLines(filePath);

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (line.IndexOf(ForbiddenPattern, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                violations.Add($"  -> [{relativePath}:{lineIndex + 1}] {line.Trim()}");
            }
        }
    }
}
