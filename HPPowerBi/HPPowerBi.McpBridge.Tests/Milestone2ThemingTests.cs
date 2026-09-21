using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HPPowerBi.McpBridge.Resources.Themes;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class Milestone2ThemingTests
{
    private static readonly Regex Definition = new("x:Key=\"(?<key>[^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex Usage = new(@"\{(?:DynamicResource|StaticResource)\s+(?<key>[^}\s,]+)", RegexOptions.Compiled);

    private static string GetMcpBridgeDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HPPowerBi.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir.FullName, "HPPowerBi.McpBridge");
    }

    [Fact]
    public void ThemeDarkAndThemeLight_DefineIdenticalKeySets()
    {
        var bridgeDir = GetMcpBridgeDirectory();
        var themesDir = Path.Combine(bridgeDir, "Resources", "Themes");

        var darkText = File.ReadAllText(Path.Combine(themesDir, "ThemeDark.xaml"));
        var lightText = File.ReadAllText(Path.Combine(themesDir, "ThemeLight.xaml"));

        var darkKeys = Definition.Matches(darkText).Select(m => m.Groups["key"].Value).OrderBy(k => k).ToList();
        var lightKeys = Definition.Matches(lightText).Select(m => m.Groups["key"].Value).OrderBy(k => k).ToList();

        Assert.NotEmpty(darkKeys);
        Assert.Equal(darkKeys, lightKeys);

        // Verify mandatory standard brush tokens
        var mandatory = new[]
        {
            "Brush.Background",
            "Brush.Surface",
            "Brush.Card",
            "Brush.Border",
            "Brush.Foreground",
            "Brush.Foreground.Primary",
            "Brush.Foreground.Secondary",
            "Brush.Foreground.Tertiary",
            "Brush.Accent",
            "Brush.Accent.Foreground",
            "Brush.Info",
            "Brush.Success",
            "Brush.Warning",
            "Brush.Danger"
        };

        foreach (var m in mandatory)
        {
            Assert.Contains(m, darkKeys);
        }
    }

    [Fact]
    public void MaterialBridge_ContainsRequiredPowerBiPaletteAndStyles()
    {
        var bridgeDir = GetMcpBridgeDirectory();
        var materialBridgePath = Path.Combine(bridgeDir, "Resources", "Themes", "MaterialBridge.xaml");
        var xaml = File.ReadAllText(materialBridgePath);

        // Power BI brand colors
        Assert.Contains("PrimaryColor=\"#F2C811\"", xaml);
        Assert.Contains("SecondaryColor=\"#E6AD00\"", xaml);

        // Font
        Assert.Contains("x:Key=\"MaterialDesignFont\"", xaml);
        Assert.Contains("Segoe UI", xaml);

        // Control styles
        Assert.Contains("x:Key=\"PrimaryButton\"", xaml);
        Assert.Contains("x:Key=\"SecondaryButton\"", xaml);
        Assert.Contains("x:Key=\"LinkButton\"", xaml);
        Assert.Contains("x:Key=\"StandardTextBox\"", xaml);
    }

    [Fact]
    public void PowerBiTheme_DefinesTypographySpacingAndCardStyles()
    {
        var bridgeDir = GetMcpBridgeDirectory();
        var themePath = Path.Combine(bridgeDir, "Resources", "Themes", "PowerBiTheme.xaml");
        var xaml = File.ReadAllText(themePath);

        var keys = Definition.Matches(xaml).Select(m => m.Groups["key"].Value).ToHashSet();

        Assert.Contains("Font.Family.Default", keys);
        Assert.Contains("Font.Family.Mono", keys);
        Assert.Contains("Font.Size.Caption", keys);
        Assert.Contains("Font.Size.Body", keys);
        Assert.Contains("Font.Size.Subheading", keys);
        Assert.Contains("Font.Size.Heading", keys);

        Assert.Contains("Spacing.Small", keys);
        Assert.Contains("Spacing.Medium", keys);
        Assert.Contains("Radius.Card", keys);

        Assert.Contains("Caption", keys);
        Assert.Contains("BodyStrong", keys);
        Assert.Contains("Subheading", keys);
        Assert.Contains("Heading", keys);
        Assert.Contains("Card", keys);
    }

    [Fact]
    public void StatusWindowXaml_AllResourceKeysAreDefined()
    {
        var bridgeDir = GetMcpBridgeDirectory();
        var themesDir = Path.Combine(bridgeDir, "Resources", "Themes");
        var statusWindowPath = Path.Combine(bridgeDir, "Views", "StatusWindow.xaml");
        var windowText = File.ReadAllText(statusWindowPath);

        var defined = new HashSet<string>(StringComparer.Ordinal);

        // Add local definitions from StatusWindow.xaml
        foreach (Match m in Definition.Matches(windowText))
        {
            defined.Add(m.Groups["key"].Value);
        }

        // Add definitions from themes
        foreach (var themeFile in new[] { "ThemeDark.xaml", "ThemeLight.xaml", "PowerBiTheme.xaml", "MaterialBridge.xaml" })
        {
            var text = File.ReadAllText(Path.Combine(themesDir, themeFile));
            foreach (Match m in Definition.Matches(text))
            {
                defined.Add(m.Groups["key"].Value);
            }
        }

        var missing = new List<string>();
        foreach (Match m in Usage.Matches(windowText))
        {
            var key = m.Groups["key"].Value;
            if (!defined.Contains(key) && !key.StartsWith("MaterialDesign", StringComparison.Ordinal))
            {
                missing.Add(key);
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void WindowsHostTheme_RespondsToEnvironmentVariable()
    {
        var prevEnv = Environment.GetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME");
        try
        {
            Environment.SetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME", "dark");
            Assert.True(WindowsHostTheme.Instance.IsDark);

            Environment.SetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME", "light");
            Assert.False(WindowsHostTheme.Instance.IsDark);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME", prevEnv);
        }
    }
}
