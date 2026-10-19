using System.Text.RegularExpressions;
using Xunit;

namespace HPRebar.Core.Tests.Themes;

/// <summary>
///     Text-level contract between the add-in's XAML and its theme: every <c>{DynamicResource}</c> /
///     <c>{StaticResource}</c> key a view or dictionary uses must be defined by a dictionary that
///     <c>Theme.xaml</c> merges (or by the toolkit, or locally in the same file). Pure file reading — no WPF —
///     so it runs wherever the Core tests run and catches a retired key before Revit does.
/// </summary>
public sealed class ThemeTokenCoverageTests
{
    private static readonly Regex Definition = new(@"x:Key=""(?<key>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex Usage = new(@"\{(?:DynamicResource|StaticResource)\s+(?<key>[^}\s,]+)", RegexOptions.Compiled);
    private static readonly Regex Comment = new(@"<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ComponentPath = new(@"/(?<assembly>HPRebar(?:\.McpBridge)?);component/(?<path>[^""#\s]+)", RegexOptions.Compiled);
    private static readonly Regex MergedSource = new(@"component/Resources/Themes/(?<file>[A-Za-z]+\.xaml)", RegexOptions.Compiled);

    [Fact]
    public void EveryResourceKeyUsedByTheAddInXamlIsDefined()
    {
        var addIn = AddInDirectory();
        var mcpBridge = McpBridgeDirectory();
        var themes = Path.Combine(addIn, "Resources", "Themes");
        var themeXaml = File.ReadAllText(Path.Combine(themes, "Theme.xaml"));

        // Keys defined by the dictionaries Theme.xaml merges — ThemeLight mirrors ThemeDark, both must define the same set.
        var mergedFiles = MergedSource.Matches(themeXaml).Select(m => m.Groups["file"].Value).ToList();
        Assert.Contains("MaterialBridge.xaml", mergedFiles);
        Assert.Contains("ThemeDark.xaml", mergedFiles);
        var defined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in mergedFiles.Append("ThemeLight.xaml"))
            defined.UnionWith(Keys(Definition, File.ReadAllText(Path.Combine(themes, file))));

        var darkKeys = Keys(Definition, File.ReadAllText(Path.Combine(themes, "ThemeDark.xaml")));
        var lightKeys = Keys(Definition, File.ReadAllText(Path.Combine(themes, "ThemeLight.xaml")));
        Assert.Equal(darkKeys.OrderBy(k => k), lightKeys.OrderBy(k => k));

        var xamlFiles = Directory.EnumerateFiles(addIn, "*.xaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(mcpBridge, "*.xaml", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        var missing = new List<string>();
        foreach (var xaml in xamlFiles)
        {
            var text = File.ReadAllText(xaml);
            var local = Keys(Definition, text);
            foreach (var key in Keys(Usage, text))
            {
                if (local.Contains(key) || defined.Contains(key) || key.StartsWith("MaterialDesign", StringComparison.Ordinal) || key.StartsWith("MahApps.", StringComparison.Ordinal) || key.StartsWith("{", StringComparison.Ordinal)) continue;
                missing.Add($"{Path.GetFileName(xaml)}: {key}");
            }
        }

        Assert.True(missing.Count == 0, "Resource keys without a definition:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void LegacyStyleKeysAreRebasedOnTheToolkitNotRedefinedBesideIt()
    {
        var themes = Path.Combine(AddInDirectory(), "Resources", "Themes");
        var themeXaml = File.ReadAllText(Path.Combine(themes, "Theme.xaml"));
        var bridge = Comment.Replace(File.ReadAllText(Path.Combine(themes, "MaterialBridge.xaml")), "");

        // MaterialBridge.xaml must come first so the palette and the legacy dictionaries keep the last word.
        Assert.True(themeXaml.IndexOf("MaterialBridge.xaml", StringComparison.Ordinal) < themeXaml.IndexOf("ThemeDark.xaml", StringComparison.Ordinal));
        // The old hand-written control dictionaries would shadow the re-based keys if they were still merged.
        Assert.DoesNotContain("Buttons.xaml", themeXaml);
        Assert.DoesNotContain("TextBoxes.xaml", themeXaml);
        foreach (var key in new[] { "StandardComboBox", "PrimaryButton", "SecondaryButton", "DangerButton", "IconButton", "LinkButton", "StandardTextBox", "NumberTextBox", "SearchTextBox" })
            Assert.Contains($"x:Key=\"{key}\"", bridge);
        // Segoe UI stays the product font; the toolkit's own font markup cannot load once the toolkit is ILRepack-merged.
        Assert.Contains("x:Key=\"MaterialDesignFont\"", bridge);
        Assert.Contains("x:Key=\"MahApps.Fonts.Family.Control\"", bridge);
        Assert.Contains("/MahApps.Metro;component/Styles/Controls.xaml", bridge);
        Assert.DoesNotContain("{md:MaterialDesignFont}", bridge);
    }

    [Fact]
    public void EveryComponentPathReferencedByXamlExistsOnDisk()
    {
        var addIn = AddInDirectory();
        var mcpBridge = McpBridgeDirectory();
        var xamlFiles = Directory.EnumerateFiles(addIn, "*.xaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(mcpBridge, "*.xaml", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        var missing = new List<string>();
        foreach (var xaml in xamlFiles)
        {
            var text = Comment.Replace(File.ReadAllText(xaml), "");
            foreach (Match match in ComponentPath.Matches(text))
            {
                var assembly = match.Groups["assembly"].Value;
                var relative = match.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar);
                var targetDir = assembly == "HPRebar.McpBridge" ? mcpBridge : addIn;
                var resolved = File.Exists(Path.Combine(targetDir, relative)) ||
                               (assembly == "HPRebar.McpBridge" && File.Exists(Path.Combine(addIn, relative)));
                if (!resolved)
                    missing.Add($"{Path.GetFileName(xaml)}: /{assembly};component/{match.Groups["path"].Value}");
            }
        }

        Assert.True(missing.Count == 0, "Component paths without a file:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    private static HashSet<string> Keys(Regex pattern, string text)
        => new(pattern.Matches(text).Select(m => m.Groups["key"].Value), StringComparer.Ordinal);

    private static string SolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HPRebar.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        return directory.FullName;
    }

    private static string AddInDirectory() => Path.Combine(SolutionDirectory(), "HPRebar");
    private static string McpBridgeDirectory() => Path.Combine(SolutionDirectory(), "HPRebar.McpBridge");
}
