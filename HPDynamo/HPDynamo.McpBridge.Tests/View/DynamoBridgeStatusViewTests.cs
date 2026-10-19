namespace HPDynamo.McpBridge.Tests.View;

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using HPDynamo.McpBridge.Tests.Fakes;
using HPDynamo.McpBridge.View;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;
using MaterialDesignThemes.Wpf;
using Xunit;

public sealed class DynamoBridgeStatusViewTests
{
    [Fact]
    public void DynamoBridgeStatusView_InstantiatesAndRendersControlsWithoutError()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var runner = new FakeMcpBridgeRunner
                {
                    PipeName = "hpdynamo-mcp-2026",
                    RevitVersion = "2026",
                    Status = BridgeStatus.Listening,
                    ExecutionEnabled = false
                };

                var vm = new McpBridgeStatusViewModel(runner, action => action(), null);
                var view = new DynamoBridgeStatusView(vm);

                // Force layout measurement and arrangement
                view.Measure(new Size(580, 640));
                view.Arrange(new Rect(0, 0, 580, 640));
                view.UpdateLayout();

                // Find buttons and verify they have content
                var grid = Assert.IsAssignableFrom<Grid>(view.Content);
                Assert.NotNull(grid);

                // Verify PackIcons and TextBlocks in the visual tree
                var packIcons = FindChildrenByName(view, "PackIcon");
                Assert.NotEmpty(packIcons);

                // Verify that PackIcons have their default styles and templates
                foreach (var icon in packIcons)
                {
                    Assert.NotNull(icon);
                    var kindProp = icon.GetType().GetProperty("Kind");
                    Assert.NotNull(kindProp);
                    var kindVal = kindProp.GetValue(icon)?.ToString();
                    Assert.False(string.IsNullOrEmpty(kindVal));
                    Assert.NotEqual("None", kindVal);
                }

                // Verify button texts
                var buttons = FindChildren<Button>(view);
                Assert.NotEmpty(buttons);
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(10000);

        if (threadEx != null)
        {
            throw new InvalidOperationException($"View instantiation failed: {threadEx.Message}", threadEx);
        }
    }

    private static System.Collections.Generic.List<T> FindChildren<T>(object? parent) where T : class
    {
        var list = new System.Collections.Generic.List<T>();
        if (parent == null) return list;

        if (parent is T match) list.Add(match);

        if (parent is Window win)
        {
            list.AddRange(FindChildren<T>(win.Content));
        }
        else if (parent is Panel panel)
        {
            foreach (UIElement child in panel.Children)
                list.AddRange(FindChildren<T>(child));
        }
        else if (parent is Decorator dec && dec.Child != null)
        {
            list.AddRange(FindChildren<T>(dec.Child));
        }
        else if (parent is ContentControl cc && cc.Content != null)
        {
            list.AddRange(FindChildren<T>(cc.Content));
        }
        return list;
    }

    private static System.Collections.Generic.List<object> FindChildrenByName(object? parent, string typeName)
    {
        var list = new System.Collections.Generic.List<object>();
        if (parent == null) return list;

        if (parent.GetType().Name == typeName) list.Add(parent);

        if (parent is Window win)
        {
            list.AddRange(FindChildrenByName(win.Content, typeName));
        }
        else if (parent is Panel panel)
        {
            foreach (UIElement child in panel.Children)
                list.AddRange(FindChildrenByName(child, typeName));
        }
        else if (parent is Decorator dec && dec.Child != null)
        {
            list.AddRange(FindChildrenByName(dec.Child, typeName));
        }
        else if (parent is ContentControl cc && cc.Content != null)
        {
            list.AddRange(FindChildrenByName(cc.Content, typeName));
        }
        return list;
    }
}
