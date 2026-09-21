using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using HPPowerBi.McpBridge.Resources.Themes;
using HPPowerBi.McpBridge.Views;
using MaterialDesignThemes.Wpf;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class Milestone2EmpiricalChallengeTests
{
    private static readonly Regex Definition = new(@"x:Key=""(?<key>[^""]+)""", RegexOptions.Compiled);
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

    #region 1. Theming System Challenge

    [Fact]
    public void Challenge_ThemeDarkAndLight_ParityAndColorValidity()
    {
        var bridgeDir = GetMcpBridgeDirectory();
        var themesDir = Path.Combine(bridgeDir, "Resources", "Themes");

        var darkText = File.ReadAllText(Path.Combine(themesDir, "ThemeDark.xaml"));
        var lightText = File.ReadAllText(Path.Combine(themesDir, "ThemeLight.xaml"));

        var darkKeys = Definition.Matches(darkText).Select(m => m.Groups["key"].Value).OrderBy(k => k).ToList();
        var lightKeys = Definition.Matches(lightText).Select(m => m.Groups["key"].Value).OrderBy(k => k).ToList();

        // 1. Exact Key Set Parity
        Assert.Equal(darkKeys, lightKeys);

        // 2. Exact Tag Type Parity (Color vs SolidColorBrush)
        var typePattern = new Regex(@"<(?<tag>Color|SolidColorBrush)\s+[^>]*x:Key=""(?<key>[^""]+)""|<(?<tag>Color|SolidColorBrush)\s+x:Key=""(?<key>[^""]+)""", RegexOptions.Compiled);
        var darkTypes = typePattern.Matches(darkText).ToDictionary(m => m.Groups["key"].Value, m => m.Groups["tag"].Value);
        var lightTypes = typePattern.Matches(lightText).ToDictionary(m => m.Groups["key"].Value, m => m.Groups["tag"].Value);

        Assert.Equal(darkTypes.Count, darkKeys.Count);
        Assert.Equal(lightTypes.Count, lightKeys.Count);

        foreach (var key in darkKeys)
        {
            Assert.True(darkTypes.ContainsKey(key), $"Dark missing type for {key}");
            Assert.True(lightTypes.ContainsKey(key), $"Light missing type for {key}");
            Assert.Equal(darkTypes[key], lightTypes[key]);
        }

        // 3. Hex code validity
        var hexPattern = new Regex(@"#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\b", RegexOptions.Compiled);
        var darkHexMatches = hexPattern.Matches(darkText);
        var lightHexMatches = hexPattern.Matches(lightText);

        Assert.NotEmpty(darkHexMatches);
        Assert.NotEmpty(lightHexMatches);
        Assert.Equal(darkKeys.Count, darkHexMatches.Count);
        Assert.Equal(lightKeys.Count, lightHexMatches.Count);
    }

    [Theory]
    [InlineData("dark", true)]
    [InlineData("DARK", true)]
    [InlineData("Dark", true)]
    [InlineData("light", false)]
    [InlineData("LIGHT", false)]
    [InlineData("Light", false)]
    public void Challenge_WindowsHostTheme_EnvironmentOverrides_ExactAndCasing(string envValue, bool expectedDark)
    {
        var prevEnv = Environment.GetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME");
        try
        {
            Environment.SetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME", envValue);
            Assert.Equal(expectedDark, WindowsHostTheme.Instance.IsDark);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME", prevEnv);
        }
    }

    [Theory]
    [InlineData("invalid_theme")]
    [InlineData("12345")]
    [InlineData("")]
    [InlineData("   ")]
    public void Challenge_WindowsHostTheme_InvalidEnvironment_DoesNotThrow(string envValue)
    {
        var prevEnv = Environment.GetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME");
        try
        {
            Environment.SetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME", envValue);
            var exception = Record.Exception(() =>
            {
                var isDark = WindowsHostTheme.Instance.IsDark;
            });
            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HPPOWERBI_MCP_BRIDGE_THEME", prevEnv);
        }
    }

    [Fact]
    public void Challenge_MaterialThemeBridge_OverlayGeneration_OnWindowWithThemeDictionary()
    {
        // Must execute on STA thread for WPF Window and ResourceDictionary
        var thread = new Thread(() =>
        {
            try
            {
                var window = new Window();
                var bridgeDir = GetMcpBridgeDirectory();
                var themesDir = Path.Combine(bridgeDir, "Resources", "Themes");

                // If window has MaterialBridge.xaml merged, FindThemeDictionary finds it
                var materialBridgeDict = new ResourceDictionary
                {
                    Source = new Uri(Path.Combine(themesDir, "MaterialBridge.xaml"), UriKind.Absolute)
                };
                window.Resources.MergedDictionaries.Add(materialBridgeDict);

                // Apply Dark theme
                MaterialThemeBridge.Apply(window, dark: true);

                // Verify overlay is added at top level
                Assert.NotEmpty(window.Resources.MergedDictionaries);
                var overlay = window.Resources.MergedDictionaries.FirstOrDefault(d => d.Contains("HPPowerBi.ThemeOverlay"));
                Assert.NotNull(overlay);

                // Verify overlay has palette merged
                Assert.NotEmpty(overlay.MergedDictionaries);

                // Verify overlay has MaterialDesign theme set
                var theme = overlay.GetTheme();
                Assert.NotNull(theme);
                Assert.Equal(BaseTheme.Dark, theme.GetBaseTheme());

                // Now Apply Light theme and verify swap
                MaterialThemeBridge.Apply(window, dark: false);
                var lightOverlay = window.Resources.MergedDictionaries.FirstOrDefault(d => d.Contains("HPPowerBi.ThemeOverlay"));
                Assert.NotNull(lightOverlay);
                var lightTheme = lightOverlay.GetTheme();
                Assert.NotNull(lightTheme);
                Assert.Equal(BaseTheme.Light, lightTheme.GetBaseTheme());
            }
            catch (Exception ex)
            {
                Assert.Fail($"STA Thread Exception: {ex}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Challenge_MaterialThemeBridge_StatusWindow_HasNoThemeDictionaryInWindowResources()
    {
        // When StatusWindow is instantiated, Window.Resources does NOT contain IMaterialDesignThemeDictionary.
        // Therefore FindThemeDictionary(window.Resources) returns null, and overlay.SetTheme(theme) is bypassed.
        var thread = new Thread(() =>
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_Challenge_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                var vm = new ViewModels.StatusViewModel(
                    new Tabular.PbiConnectionManager(),
                    new Safety.PbiSafetyGuard(),
                    new Safety.PbiSnapshotManager(tempDir),
                    host: null,
                    cloudClient: null,
                    logDirectory: tempDir,
                    onUiThread: action => action());

                // When Application.Current does not exist or has not loaded App.xaml:
                // StatusWindow.xaml fails to initialize because of StaticResource Caption/BodyStrong!
                // Let's verify that when App is initialized, StatusWindow can load:
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var statusWindow = new StatusWindow(vm);
                
                // Let's inspect statusWindow.Resources
                var themeDict = typeof(MaterialThemeBridge)
                    .GetMethod("FindThemeDictionary", BindingFlags.NonPublic | BindingFlags.Static)?
                    .Invoke(null, new object[] { statusWindow.Resources });

                // EMPIRICAL PROOF: In StatusWindow.Resources, FindThemeDictionary is NULL!
                Assert.Null(themeDict);
            }
            catch (Exception ex)
            {
                Assert.Fail($"STA Thread Exception: {ex}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Challenge_MaterialThemeBridge_WhenSeedProvidedInApp_CanBeFoundInApplicationResources()
    {
        var thread = new Thread(() =>
        {
            try
            {
                // Simulate App.xaml resources
                var appResources = new ResourceDictionary();
                var bridgeDir = GetMcpBridgeDirectory();
                var themesDir = Path.Combine(bridgeDir, "Resources", "Themes");
                var materialBridgeDict = new ResourceDictionary
                {
                    Source = new Uri(Path.Combine(themesDir, "MaterialBridge.xaml"), UriKind.Absolute)
                };
                appResources.MergedDictionaries.Add(materialBridgeDict);

                var themeDict = typeof(MaterialThemeBridge)
                    .GetMethod("FindThemeDictionary", BindingFlags.NonPublic | BindingFlags.Static)?
                    .Invoke(null, new object[] { appResources });

                // Finding in App.xaml resources succeeds!
                Assert.NotNull(themeDict);
            }
            catch (Exception ex)
            {
                Assert.Fail($"STA Thread Exception: {ex}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(5));
    }

    #endregion

    #region 2. XAML Resource Tokens Challenge

    [Fact]
    public void Challenge_StatusWindow_ComprehensiveResourceResolution()
    {
        var bridgeDir = GetMcpBridgeDirectory();
        var themesDir = Path.Combine(bridgeDir, "Resources", "Themes");
        var statusWindowPath = Path.Combine(bridgeDir, "Views", "StatusWindow.xaml");
        var windowText = File.ReadAllText(statusWindowPath);

        // Collect all definitions
        var defined = new HashSet<string>(StringComparer.Ordinal);

        // 1. Local definitions in StatusWindow.xaml
        foreach (Match m in Definition.Matches(windowText))
        {
            defined.Add(m.Groups["key"].Value);
        }

        // 2. Definitions from all theme dictionaries
        foreach (var themeFile in new[] { "ThemeDark.xaml", "ThemeLight.xaml", "PowerBiTheme.xaml", "MaterialBridge.xaml" })
        {
            var text = File.ReadAllText(Path.Combine(themesDir, themeFile));
            foreach (Match m in Definition.Matches(text))
            {
                defined.Add(m.Groups["key"].Value);
            }
        }

        // Known MaterialDesignTheme resource keys provided by MaterialDesign2.Defaults.xaml
        var knownMaterialDesignKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "MaterialDesignRaisedButton",
            "MaterialDesignOutlinedButton",
            "MaterialDesignFlatButton",
            "MaterialDesignOutlinedTextBox",
            "MaterialDesignFont",
            "MaterialDesignTheme"
        };

        // Find ALL {DynamicResource ...} and {StaticResource ...}
        var dynamicPattern = new Regex(@"\{DynamicResource\s+(?<key>[^}\s,]+)", RegexOptions.Compiled);
        var staticPattern = new Regex(@"\{StaticResource\s+(?<key>[^}\s,]+)", RegexOptions.Compiled);

        var dynamicMatches = dynamicPattern.Matches(windowText).Select(m => m.Groups["key"].Value).ToList();
        var staticMatches = staticPattern.Matches(windowText).Select(m => m.Groups["key"].Value).ToList();

        Assert.NotEmpty(dynamicMatches);
        Assert.NotEmpty(staticMatches);

        var unresolvedDynamic = new List<string>();
        foreach (var key in dynamicMatches)
        {
            if (!defined.Contains(key) && !knownMaterialDesignKeys.Contains(key))
            {
                unresolvedDynamic.Add(key);
            }
        }

        var unresolvedStatic = new List<string>();
        foreach (var key in staticMatches)
        {
            if (!defined.Contains(key) && !knownMaterialDesignKeys.Contains(key))
            {
                unresolvedStatic.Add(key);
            }
        }

        Assert.Empty(unresolvedDynamic);
        Assert.Empty(unresolvedStatic);

        // Verify that every DynamicResource used in StatusWindow is present in BOTH ThemeDark and ThemeLight if it's a theme token
        var darkText = File.ReadAllText(Path.Combine(themesDir, "ThemeDark.xaml"));
        var lightText = File.ReadAllText(Path.Combine(themesDir, "ThemeLight.xaml"));
        var darkKeys = Definition.Matches(darkText).Select(m => m.Groups["key"].Value).ToHashSet();
        var lightKeys = Definition.Matches(lightText).Select(m => m.Groups["key"].Value).ToHashSet();

        foreach (var key in dynamicMatches.Where(k => k.StartsWith("Brush.") || k.StartsWith("Color.")))
        {
            Assert.True(darkKeys.Contains(key), $"Token {key} missing in ThemeDark.xaml");
            Assert.True(lightKeys.Contains(key), $"Token {key} missing in ThemeLight.xaml");
        }
    }

    #endregion
}
