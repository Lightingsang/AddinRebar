using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Autodesk.Navisworks.Api.Plugins;
using Xunit;

namespace HPNavis.McpBridge.Tests;

/// <summary>
///     The Ribbon is declared three times over — attributes on <see cref="HPNavisRibbonPlugin"/>, the layout XAML
///     and the strings file — and Navisworks silently drops a tab whose ids disagree. These tests pin the three
///     to each other and to the shipped PNGs, reading the files from the source tree (same walk as the seeds).
/// </summary>
public sealed class RibbonPluginTests
{
    private static readonly Type Plugin = typeof(HPNavisRibbonPlugin);
    private static readonly string RibbonRoot = FindRibbonRoot();
    private static readonly string LayoutPath = Path.Combine(RibbonRoot, "en-US", "HPNavisRibbon.xaml");
    private static readonly string StringsPath = Path.Combine(RibbonRoot, "en-US", "HPNavisRibbon.name");

    private static readonly XNamespace AdWindows = "clr-namespace:Autodesk.Windows;assembly=AdWindows";
    private static readonly XNamespace Roamer = "clr-namespace:Autodesk.Navisworks.Gui.Roamer.AIRLook;assembly=navisworks.gui.roamer";

    [Fact]
    public void Three_plugins_share_one_developer_id_and_have_distinct_names()
    {
        var plugins = Plugin.Assembly.GetTypes()
            .Select(t => t.GetCustomAttribute<PluginAttribute>())
            .Where(a => a is not null)
            .Select(a => a!)
            .ToArray();

        Assert.Equal(["HPNavis.McpBridge", "HPNavis.McpBridge.Ribbon", "HPNavis.McpBridge.Window"], plugins.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(plugins, p => Assert.Equal("HPNV", p.DeveloperId));
    }

    [Fact]
    public void Ribbon_plugin_declares_layout_and_strings_files_that_exist_in_en_US()
    {
        Assert.Equal("HPNavisRibbon.xaml", Plugin.GetCustomAttribute<RibbonLayoutAttribute>()!.Xaml);
        Assert.Equal("HPNavisRibbon.name", Plugin.GetCustomAttribute<StringsAttribute>()!.FileName);
        Assert.True(File.Exists(LayoutPath), LayoutPath);
        Assert.True(File.Exists(StringsPath), StringsPath);
    }

    [Fact]
    public void Ribbon_tab_attribute_matches_the_single_xaml_tab()
    {
        var tab = Plugin.GetCustomAttribute<RibbonTabAttribute>()!;
        var xamlTab = Assert.Single(Layout().Descendants(AdWindows + "RibbonTab"));

        Assert.Equal(HPNavisRibbonPlugin.TabId, tab.Name);
        Assert.Equal(tab.Name, (string?)xamlTab.Attribute("Id"));
        Assert.Equal("HPNavis", (string?)xamlTab.Attribute("Title"));
        Assert.Equal(tab.DisplayName, (string?)xamlTab.Attribute("Title"));
    }

    [Fact]
    public void Command_attribute_matches_the_single_xaml_button()
    {
        var command = Assert.Single(Plugin.GetCustomAttributes<CommandAttribute>());
        var button = Assert.Single(Layout().Descendants(Roamer + "NWRibbonButton"));
        var panel = Assert.Single(Layout().Descendants(AdWindows + "RibbonPanelSource"));

        Assert.Equal(HPNavisRibbonPlugin.McpBridgeCommandId, command.Name);
        Assert.Equal(command.Name, (string?)button.Attribute("Id"));
        Assert.Equal("MCP Bridge", command.DisplayName);
        Assert.Equal(CallCanExecute.Always, command.CallCanExecute);
        Assert.Equal("MCP", (string?)panel.Attribute("Title"));
        Assert.Equal("Large", (string?)button.Attribute("Size"));
    }

    [Fact]
    public void Xaml_carries_the_mandatory_navisworks_header()
    {
        var root = Layout().Root!;

        Assert.Equal(AdWindows + "RibbonControl", root.Name);
        Assert.Equal("http://schemas.microsoft.com/winfx/2006/xaml", root.GetNamespaceOfPrefix("x")?.NamespaceName);
        Assert.Equal("http://schemas.microsoft.com/winfx/2006/xaml/presentation", root.GetNamespaceOfPrefix("wpf")?.NamespaceName);
        Assert.Equal("clr-namespace:Autodesk.Internal.Windows;assembly=AdWindows", root.GetNamespaceOfPrefix("adwi")?.NamespaceName);
        Assert.Equal("clr-namespace:System;assembly=mscorlib", root.GetNamespaceOfPrefix("system")?.NamespaceName);
        Assert.Equal(Roamer.NamespaceName, root.GetNamespaceOfPrefix("local")?.NamespaceName);

        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var uids = Layout().Descendants().Select(e => (string?)e.Attribute(x + "Uid")).Where(u => u is not null).ToArray();
        Assert.Equal(uids.Length, uids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Xaml_image_paths_and_command_icons_name_the_shipped_pngs()
    {
        var command = Assert.Single(Plugin.GetCustomAttributes<CommandAttribute>());
        var button = Assert.Single(Layout().Descendants(Roamer + "NWRibbonButton"));

        var small = ResolveFromLayout((string)button.Attribute("Image")!);
        var large = ResolveFromLayout((string)button.Attribute("LargeImage")!);
        Assert.True(File.Exists(small), small);
        Assert.True(File.Exists(large), large);
        Assert.Equal(command.Icon, Path.GetFileName(small));
        Assert.Equal(command.LargeIcon, Path.GetFileName(large));
    }

    [Theory]
    [InlineData("McpBridge_16.png", 16)]
    [InlineData("McpBridge_32.png", 32)]
    public void Icons_are_square_rgba_pngs_of_the_documented_size(string file, int expected)
    {
        var bytes = File.ReadAllBytes(Path.Combine(RibbonRoot, "Images", file));

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, bytes.Take(8));
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));
        Assert.Equal(expected, ReadBigEndian(bytes, 16));
        Assert.Equal(expected, ReadBigEndian(bytes, 20));
        Assert.Equal(8, bytes[24]);   // bit depth
        Assert.Equal(6, bytes[25]);   // colour type: truecolour with alpha
    }

    [Fact]
    public void Strings_file_has_the_utf8_header_and_a_value_for_every_id()
    {
        var lines = File.ReadAllLines(StringsPath);
        var keyed = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length - 1; i++)
        {
            if (lines[i].EndsWith("=", StringComparison.Ordinal) && !lines[i].StartsWith("#", StringComparison.Ordinal))
                keyed[lines[i].TrimEnd('=')] = lines[i + 1];
        }

        Assert.Equal("$utf8", lines.First(l => !l.StartsWith("#", StringComparison.Ordinal) && l.Length > 0));
        Assert.All(new[] { "DisplayName", HPNavisRibbonPlugin.TabId + ".DisplayName", HPNavisRibbonPlugin.McpBridgeCommandId + ".DisplayName", HPNavisRibbonPlugin.McpBridgeCommandId + ".ToolTip", HPNavisRibbonPlugin.McpBridgeCommandId + ".ExtendedToolTip" },
            key => Assert.False(string.IsNullOrWhiteSpace(keyed[key]), key));
        var command = Assert.Single(Plugin.GetCustomAttributes<CommandAttribute>());
        Assert.Equal(command.DisplayName, keyed[HPNavisRibbonPlugin.McpBridgeCommandId + ".DisplayName"]);
        Assert.Equal(command.ToolTip, keyed[HPNavisRibbonPlugin.McpBridgeCommandId + ".ToolTip"]);
        Assert.Equal(command.ExtendedToolTip, keyed[HPNavisRibbonPlugin.McpBridgeCommandId + ".ExtendedToolTip"]);
        Assert.Equal("HPNavis", keyed[HPNavisRibbonPlugin.TabId + ".DisplayName"]);
        Assert.All(File.ReadAllBytes(StringsPath), b => Assert.True(b < 128, "the strings file is meant to stay ASCII (no BOM either)"));
    }

    [Fact]
    public void Window_add_in_is_hidden_from_the_add_ins_menu()
    {
        var location = typeof(HPNavisWindowPlugin).GetCustomAttribute<AddInPluginAttribute>()!.Location;

        Assert.Equal(AddInLocation.None, location);
    }

    [Fact]
    public void Ribbon_files_are_copied_into_en_US_and_Images_beside_the_assembly()
    {
        // The referenced project's None items flow into this test's output the way they flow into the plugin folder:
        // en-US\ and Images\ subfolders, the XAML verbatim (a markup-compiled Page would not be here).
        var output = AppContext.BaseDirectory;

        Assert.True(File.Exists(Path.Combine(output, "en-US", "HPNavisRibbon.xaml")));
        Assert.True(File.Exists(Path.Combine(output, "en-US", "HPNavisRibbon.name")));
        Assert.True(File.Exists(Path.Combine(output, "Images", "McpBridge_16.png")));
        Assert.True(File.Exists(Path.Combine(output, "Images", "McpBridge_32.png")));
        Assert.Equal(File.ReadAllText(LayoutPath), File.ReadAllText(Path.Combine(output, "en-US", "HPNavisRibbon.xaml")));
    }

    private static XDocument Layout() => XDocument.Load(LayoutPath);

    private static string ResolveFromLayout(string relative) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(LayoutPath)!, relative.Replace('/', Path.DirectorySeparatorChar)));

    private static int ReadBigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    private static string FindRibbonRoot()
    {
        // bin/Debug/net48 → HPNavis.McpBridge.Tests → HPNavis → HPNavis.McpBridge/Ribbon
        for (var probe = new DirectoryInfo(AppContext.BaseDirectory); probe is not null; probe = probe.Parent)
        {
            var candidate = Path.Combine(probe.FullName, "HPNavis.McpBridge", "Ribbon");
            if (Directory.Exists(candidate)) return candidate;
        }

        throw new DirectoryNotFoundException("HPNavis.McpBridge/Ribbon not found above " + AppContext.BaseDirectory);
    }
}
