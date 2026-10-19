using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HPGrasshopper.McpBridge.UI;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;
using Xunit;
using WpfButton = System.Windows.Controls.Button;
using WpfSize = System.Windows.Size;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace HPGrasshopper.McpBridge.Tests;

public class StatusViewRenderingTests
{
    private sealed class DummyRunner : IMcpBridgeRunner
    {
        public string PipeName { get; set; } = "hpgrasshopper-mcp-2026";
        public string RevitVersion { get; set; } = "2026";
        public string HostName { get; set; } = "Grasshopper";
        public string AuditDirectory { get; set; } = @"C:\Temp\GrasshopperAudit";
        public BridgeStatus Status { get; set; } = BridgeStatus.Listening;
        public string? StatusMessage { get; set; }
        public bool HasClient { get; set; } = true;
        public bool ExecutionEnabled { get; set; } = false;
        public bool AutoStartListener { get; set; } = true;
        public LastRunInfo? LastRun { get; set; }
        public int CompiledScriptCount { get; set; }

        public event Action? StateChanged;

        public void Start() { StateChanged?.Invoke(); }
        public void Stop() { StateChanged?.Invoke(); }
        public void Restart() { StateChanged?.Invoke(); }
    }

    [Fact]
    public void TestAllButtonsHaveVisibleText()
    {
        string? failure = null;
        var staThread = new Thread(() =>
        {
            try
            {
                var runner = new DummyRunner();
                var vm = new GrasshopperBridgeStatusViewModel(runner, act => act(), _ => {});
                var window = new GrasshopperBridgeStatusView(vm);

                var root = window.Content as FrameworkElement;
                Assert.NotNull(root);

                root.Measure(new WpfSize(580, 640));
                root.Arrange(new Rect(0, 0, 580, 640));
                root.UpdateLayout();

                var buttons = new List<WpfButton>();
                FindVisualChildren(root, buttons);

                Assert.NotEmpty(buttons);
                AssertButtonsHaveText(buttons);
            }
            catch (Exception ex)
            {
                failure = ex.ToString();
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();

        if (failure != null) throw new Exception("STA thread failed: " + failure);
    }

    private static void AssertButtonsHaveText(List<WpfButton> buttons)
    {
        var expectedButtons = new Dictionary<string, string>
        {
            { "ToggleListener", "Stop listener" },
            { "RestartBridge", "Restart bridge" },
            { "CloseBridge", "Close" },
            { "CopyLastScript", "Copy script" },
            { "OpenAuditFolder", "Open audit folder" }
        };

        foreach (var (autoId, expectedText) in expectedButtons)
        {
            var btn = buttons.Find(b => System.Windows.Automation.AutomationProperties.GetAutomationId(b) == autoId);
            Assert.NotNull(btn);
            Assert.Equal(expectedText, btn.Content?.ToString());

            var textBlocks = new List<WpfTextBlock>();
            FindVisualChildren(btn, textBlocks);
            Assert.NotEmpty(textBlocks);
            Assert.Contains(textBlocks, tb => tb.Text == expectedText);
        }
    }

    private static void FindVisualChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
    {
        if (parent == null) return;
        if (parent is T match && !results.Contains(match)) results.Add(match);
        if (parent is not (Visual or Visual3D)) return;

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            FindVisualChildren(child, results);
        }
    }
}
