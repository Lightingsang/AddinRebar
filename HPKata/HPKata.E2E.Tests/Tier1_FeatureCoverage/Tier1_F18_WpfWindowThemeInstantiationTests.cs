using System;
using System.Threading;
using System.Windows;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 18 / Acceptance Criteria:
/// Instantiation on STA thread for all 13 WPF windows under Dark and Light themes
/// without throwing StaticResourceExtension or XAML parse exceptions.
/// </summary>
public sealed class Tier1_F18_WpfWindowThemeInstantiationTests
{
    private static readonly (string Name, Func<Window> Factory)[] WindowFactories =
    [
        ("BeamDetailingWindow", () => new BeamDetailingWindow(new BeamDetailingViewModel())),
        ("ColumnDetailingWindow", () => new ColumnDetailingWindow(new ColumnDetailingViewModel())),
        ("CuttingStockView", () => new CuttingStockView()),
        ("DetailingView", () => new DetailingView()),
        ("DrawingUtilityWindow", () => new DrawingUtilityWindow()),
        ("EtabsImportWindow", () => new EtabsImportWindow(new EtabsImportViewModel())),
        ("FoundationDetailingWindow", () => new FoundationDetailingWindow(new FoundationDetailingViewModel())),
        ("QsTakeoffWindow", () => new QsTakeoffWindow()),
        ("ScheduleView", () => new ScheduleView()),
        ("SetupView", () => new SetupView()),
        ("SlabDetailingWindow", () => new SlabDetailingWindow(new SlabDetailingViewModel())),
        ("StairLintelDetailingWindow", () => new StairLintelDetailingWindow(new StairLintelDetailingViewModel())),
        ("WallDetailingWindow", () => new WallDetailingWindow(new WallDetailingViewModel()))
    ];

    [Fact]
    public void Exactly13Windows_AreRegisteredForVerification()
    {
        Assert.Equal(13, WindowFactories.Length);
    }

    [Fact]
    public void All13Windows_InstantiateSuccessfully_UnderDarkTheme()
    {
        RunOnSta(() =>
        {
            ApplyTheme("pack://application:,,,/HPKata.UI;component/Themes/ThemeDark.xaml");

            foreach (var (name, factory) in WindowFactories)
            {
                Window window = factory();
                Assert.NotNull(window);
                Assert.True(window is Window, $"{name} must inherit from System.Windows.Window");
                Assert.False(string.IsNullOrWhiteSpace(window.Title), $"{name} must have a non-empty Title");
            }
        });
    }

    [Fact]
    public void All13Windows_InstantiateSuccessfully_UnderLightTheme()
    {
        RunOnSta(() =>
        {
            ApplyTheme("pack://application:,,,/HPKata.UI;component/Themes/ThemeLight.xaml");

            foreach (var (name, factory) in WindowFactories)
            {
                Window window = factory();
                Assert.NotNull(window);
                Assert.True(window is Window, $"{name} must inherit from System.Windows.Window");
                Assert.False(string.IsNullOrWhiteSpace(window.Title), $"{name} must have a non-empty Title");
            }
        });
    }

    [Theory]
    [InlineData("BeamDetailingWindow")]
    [InlineData("ColumnDetailingWindow")]
    [InlineData("CuttingStockView")]
    [InlineData("DetailingView")]
    [InlineData("DrawingUtilityWindow")]
    [InlineData("EtabsImportWindow")]
    [InlineData("FoundationDetailingWindow")]
    [InlineData("QsTakeoffWindow")]
    [InlineData("ScheduleView")]
    [InlineData("SetupView")]
    [InlineData("SlabDetailingWindow")]
    [InlineData("StairLintelDetailingWindow")]
    [InlineData("WallDetailingWindow")]
    public void IndividualWindow_HasValidDataContext_AndDimensions(string windowName)
    {
        RunOnSta(() =>
        {
            var match = Array.Find(WindowFactories, w => w.Name == windowName);
            Assert.True(match.Factory != null, $"Factory for {windowName} must exist");

            Window window = match.Factory();
            Assert.NotNull(window);
            Assert.NotNull(window.DataContext);
            Assert.True(window.Width > 0 || window.MinWidth > 0 || !double.IsNaN(window.Width));
            Assert.True(window.Height > 0 || window.MinHeight > 0 || !double.IsNaN(window.Height));
        });
    }

    [Fact]
    public void DetailingWindows_STA_ExcelToolbarControls_InstantiateAndBindCleanly()
    {
        RunOnSta(() =>
        {
            var beamVm = new BeamDetailingViewModel();
            var beamWindow = new BeamDetailingWindow(beamVm);
            Assert.NotNull(beamWindow);
            Assert.False(beamVm.AutoSyncOnApply);
            Assert.True(beamVm.OpenExcelCommand.CanExecute(null));

            var colVm = new ColumnDetailingViewModel();
            var colWindow = new ColumnDetailingWindow(colVm);
            Assert.NotNull(colWindow);
            Assert.False(colVm.AutoSyncOnApply);
            Assert.True(colVm.OpenExcelCommand.CanExecute(null));

            var wallVm = new WallDetailingViewModel();
            var wallWindow = new WallDetailingWindow(wallVm);
            Assert.NotNull(wallWindow);
            Assert.False(wallVm.AutoSyncOnApply);
            Assert.True(wallVm.OpenExcelCommand.CanExecute(null));
        });
    }

    private static void ApplyTheme(string themeUriString)
    {
        var app = Application.Current!;
        var themeDict = new ResourceDictionary
        {
            Source = new Uri(themeUriString, UriKind.Absolute)
        };
        app.Resources.MergedDictionaries.Add(themeDict);
    }

    private static void RunOnSta(Action action) => Common.E2EWpfHelper.RunOnSta(action);
}
