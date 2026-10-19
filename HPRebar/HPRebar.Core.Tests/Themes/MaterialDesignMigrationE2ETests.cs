using System.Text.RegularExpressions;
using Xunit;

namespace HPRebar.Core.Tests.Themes;

/// <summary>
///     E2E integration test suite validating MaterialDesignInXamlToolkit v5.3.2 migration contracts
///     across Tiers 1-4 for HPRebar and HPRebar.McpBridge (pure file & AST scanning without Revit API).
/// </summary>
public sealed class MaterialDesignMigrationE2ETests
{
    private static readonly Regex Definition = new(@"x:Key=""(?<key>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex Comment = new(@"<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex HexColor = new(@"^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.Compiled);
    private static readonly Regex PackUriSource = new(@"Source=""pack://application:,,,/(?<component>[^;]+);component/(?<path>[^""]+)""", RegexOptions.Compiled);

    // ==========================================
    // Tier 1: Feature Coverage
    // ==========================================

    [Fact]
    public void Tier1_FeatureCoverage_AllRequiredColorAndBrushTokensExistInDarkAndLightPalettes()
    {
        var themesDir = Path.Combine(AddInDirectory(), "Resources", "Themes");
        var darkText = File.ReadAllText(Path.Combine(themesDir, "ThemeDark.xaml"));
        var lightText = File.ReadAllText(Path.Combine(themesDir, "ThemeLight.xaml"));

        var requiredTokenBases = new[]
        {
            "Background", "Surface", "SurfaceElevated", "SurfaceHover", "SurfacePressed",
            "Foreground.Primary", "Foreground.Secondary", "Foreground.Tertiary", "Foreground.Disabled", "Foreground.OnAccent",
            "Accent", "Accent.Hover", "Accent.Pressed",
            "Border", "Border.Focus", "Border.Disabled", "GridLine",
            "Success", "Warning", "Danger", "Info",
            "Canvas.Fill", "Canvas.MainBar", "Canvas.MainBar.Selected", "Canvas.Stirrup", "Canvas.Bound", "Canvas.Tag"
        };

        var darkKeys = Keys(Definition, darkText);
        var lightKeys = Keys(Definition, lightText);

        foreach (var token in requiredTokenBases)
        {
            Assert.Contains($"Color.{token}", darkKeys);
            Assert.Contains($"Brush.{token}", darkKeys);
            Assert.Contains($"Color.{token}", lightKeys);
            Assert.Contains($"Brush.{token}", lightKeys);
        }

        foreach (Match match in Regex.Matches(darkText, @"<Color\s+x:Key=""[^""]+"">(?<val>[^<]+)</Color>"))
            Assert.Matches(HexColor, match.Groups["val"].Value.Trim());
        foreach (Match match in Regex.Matches(lightText, @"<Color\s+x:Key=""[^""]+"">(?<val>[^<]+)</Color>"))
            Assert.Matches(HexColor, match.Groups["val"].Value.Trim());
    }

    [Fact]
    public void Tier1_FeatureCoverage_MaterialBridgeMergesMaterialDesign2Defaults()
    {
        var bridgeText = File.ReadAllText(Path.Combine(AddInDirectory(), "Resources", "Themes", "MaterialBridge.xaml"));
        Assert.Contains("MaterialDesign2.Defaults.xaml", bridgeText);
        Assert.Contains("BaseTheme=\"Dark\"", bridgeText);
        Assert.Contains("PrimaryColor=\"#0696D7\"", bridgeText);
        Assert.Contains("SecondaryColor=\"#E0641E\"", bridgeText);
    }

    [Fact]
    public void Tier1_FeatureCoverage_SegoeUiFontOverrideIsActive()
    {
        var bridgeText = Comment.Replace(File.ReadAllText(Path.Combine(AddInDirectory(), "Resources", "Themes", "MaterialBridge.xaml")), "");
        Assert.Contains(@"<FontFamily x:Key=""MaterialDesignFont"">Segoe UI</FontFamily>", bridgeText);
        Assert.DoesNotContain("{md:MaterialDesignFont}", bridgeText);
    }

    [Fact]
    public void Tier1_FeatureCoverage_ImplicitStylesCoverStandardControls()
    {
        var themesDir = Path.Combine(AddInDirectory(), "Resources", "Themes");
        var themeText = File.ReadAllText(Path.Combine(themesDir, "Theme.xaml"));
        var bridgeText = File.ReadAllText(Path.Combine(themesDir, "MaterialBridge.xaml"));

        Assert.Contains("MaterialBridge.xaml", themeText);
        Assert.Contains("MaterialDesign2.Defaults.xaml", bridgeText);

        var implicitControls = new[] { "TextBox", "ComboBox", "CheckBox", "RadioButton", "Button", "DataGrid", "TabControl" };
        foreach (var control in implicitControls)
        {
            bool inTheme = Regex.IsMatch(themeText, $@"<Style\s+TargetType=[""']{control}[""']");
            bool inDefaults = bridgeText.Contains("MaterialDesign2.Defaults.xaml");
            Assert.True(inTheme || inDefaults, $"Implicit style for {control} must be provided.");

            if (inTheme)
            {
                var match = Regex.Match(themeText, $@"<Style\s+TargetType=[""']{control}[""']\s+BasedOn=[""']\{{StaticResource\s+(?<based>[^}}]+)\}}[""']");
                Assert.True(match.Success, $"Explicit style for {control} must have valid BasedOn.");
            }
        }
    }

    // ==========================================
    // Tier 2: Boundary & Corner Cases
    // ==========================================

    [Fact]
    public void Tier2_BoundaryAndCorner_ThemeDarkAndLightTokensAreSymmetric()
    {
        var themesDir = Path.Combine(AddInDirectory(), "Resources", "Themes");
        var darkKeys = Keys(Definition, File.ReadAllText(Path.Combine(themesDir, "ThemeDark.xaml")));
        var lightKeys = Keys(Definition, File.ReadAllText(Path.Combine(themesDir, "ThemeLight.xaml")));

        var missingInLight = darkKeys.Except(lightKeys).ToList();
        var missingInDark = lightKeys.Except(darkKeys).ToList();

        Assert.Empty(missingInLight);
        Assert.Empty(missingInDark);
        Assert.Equal(darkKeys.OrderBy(k => k), lightKeys.OrderBy(k => k));
    }

    [Fact]
    public void Tier2_BoundaryAndCorner_AllReferencedDictionariesUseValidPackUris()
    {
        var addInDir = AddInDirectory();
        var themeText = Comment.Replace(File.ReadAllText(Path.Combine(addInDir, "Resources", "Themes", "Theme.xaml")), "");
        var matches = PackUriSource.Matches(themeText);

        Assert.NotEmpty(matches);
        foreach (Match match in matches)
        {
            var component = match.Groups["component"].Value;
            var relativePath = match.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar);

            if (component == "HPRebar")
            {
                var targetFile = Path.Combine(addInDir, relativePath);
                Assert.True(File.Exists(targetFile), $"Referenced dictionary missing on disk: {targetFile}");
            }
            else
            {
                Assert.Equal("MaterialDesignThemes.Wpf", component);
            }
        }
    }

    // ==========================================
    // Tier 3: Cross-Module Combinations
    // ==========================================

    [Fact]
    public void Tier3_CrossModule_McpBridgeThemeResolvesAllComponentsProperly()
    {
        var rootDir = RootDirectory();
        var mcpBridgeDir = Path.Combine(rootDir, "HPRebar.McpBridge");
        var mcpThemeFile = Path.Combine(mcpBridgeDir, "Resources", "Themes", "Theme.xaml");
        Assert.True(File.Exists(mcpThemeFile), "HPRebar.McpBridge Theme.xaml must exist.");

        var mcpCsproj = File.ReadAllText(Path.Combine(mcpBridgeDir, "HPRebar.McpBridge.csproj"));
        Assert.Contains(@"<Page Include=""..\HPRebar\Resources\Themes\*.xaml""", mcpCsproj);
        Assert.Contains(@"<Compile Include=""..\HPRebar\Resources\Themes\MaterialThemeBridge.cs""", mcpCsproj);

        var mcpThemeText = Comment.Replace(File.ReadAllText(mcpThemeFile), "");
        var matches = PackUriSource.Matches(mcpThemeText);
        Assert.NotEmpty(matches);

        foreach (Match match in matches)
        {
            var component = match.Groups["component"].Value;
            var relPath = match.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar);
            Assert.Equal("HPRebar.McpBridge", component);

            var directFile = Path.Combine(mcpBridgeDir, relPath);
            var linkedFile = Path.Combine(rootDir, "HPRebar", relPath);
            Assert.True(File.Exists(directFile) || File.Exists(linkedFile), $"McpBridge dictionary target missing: {relPath}");
        }
    }

    // ==========================================
    // Tier 4: Real-World Packaging Contracts
    // ==========================================

    [Fact]
    public void Tier4_PackagingContract_ZeroRobotoPackUrisAcrossAllXamlFiles()
    {
        var rootDir = RootDirectory();
        var xamlFiles = Directory.EnumerateFiles(Path.Combine(rootDir, "HPRebar"), "*.xaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(rootDir, "HPRebar.McpBridge"), "*.xaml", SearchOption.AllDirectories));

        var violations = new List<string>();
        foreach (var xaml in xamlFiles)
        {
            var activeContent = Comment.Replace(File.ReadAllText(xaml), "");
            if (activeContent.Contains("Roboto", StringComparison.OrdinalIgnoreCase))
                violations.Add($"Roboto reference in {Path.GetFileName(xaml)}");
            if (activeContent.Contains("{md:MaterialDesignFont}", StringComparison.Ordinal))
                violations.Add($"MaterialDesignFont markup in {Path.GetFileName(xaml)}");
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Tier4_PackagingContract_PaletteHelperIsNowhereInSourceCode()
    {
        var rootDir = RootDirectory();
        var codeFiles = Directory.EnumerateFiles(Path.Combine(rootDir, "HPRebar"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(rootDir, "HPRebar.McpBridge"), "*.cs", SearchOption.AllDirectories));

        var violations = new List<string>();
        foreach (var cs in codeFiles)
        {
            var lines = File.ReadAllLines(cs);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("///") || line.StartsWith("//")) continue;
                if (line.Contains("PaletteHelper", StringComparison.Ordinal))
                    violations.Add($"{Path.GetFileName(cs)}:{i + 1}: {line}");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Tier4_PackagingContract_ILRepackAndPackagingIsolationAreConfigured()
    {
        var rootDir = RootDirectory();
        var rebarCsproj = File.ReadAllText(Path.Combine(rootDir, "HPRebar", "HPRebar.csproj"));
        var bridgeCsproj = File.ReadAllText(Path.Combine(rootDir, "HPRebar.McpBridge", "HPRebar.McpBridge.csproj"));

        Assert.Contains("<IsRepackable>true</IsRepackable>", rebarCsproj);
        Assert.Contains("<Target Name=\"RepackMaterialDesign\"", bridgeCsproj);
        Assert.Contains("MaterialDesignThemes.Wpf.dll", bridgeCsproj);
        Assert.Contains("MaterialDesignColors.dll", bridgeCsproj);
        Assert.Contains("Microsoft.Xaml.Behaviors.dll", bridgeCsproj);

        var themeInfoPath = Path.Combine(rootDir, "HPRebar", "Resources", "Themes", "ThemeInfo.cs");
        Assert.True(File.Exists(themeInfoPath), "ThemeInfo.cs must exist for BAML packaging.");
        var themeInfoText = File.ReadAllText(themeInfoPath);
        Assert.Contains("ThemeInfo", themeInfoText);
        Assert.Contains("ResourceDictionaryLocation.SourceAssembly", themeInfoText);
    }

    // ==========================================
    // Helpers
    // ==========================================

    private static HashSet<string> Keys(Regex pattern, string text)
        => new(pattern.Matches(text).Select(m => m.Groups["key"].Value), StringComparer.Ordinal);

    private static string RootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HPRebar.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return directory.FullName;
    }

    private static string AddInDirectory() => Path.Combine(RootDirectory(), "HPRebar");
}
