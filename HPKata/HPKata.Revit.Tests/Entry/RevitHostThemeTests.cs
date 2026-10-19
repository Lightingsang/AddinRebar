using HPKata.Revit.Entry;
using HPKata.UI.Common;
using Xunit;

namespace HPKata.Revit.Tests.Entry;

public sealed class RevitHostThemeTests
{
    [Fact]
    public void Instance_IsSingleton()
    {
        var instance1 = RevitHostTheme.Instance;
        var instance2 = RevitHostTheme.Instance;

        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Changed_EventFiresOnNotification()
    {
        var theme = RevitHostTheme.Instance;
        bool eventFired = false;

        Action handler = () => eventFired = true;
        theme.Changed += handler;

        try
        {
            theme.NotifyThemeChanged();
            Assert.True(eventFired, "NotifyThemeChanged should trigger the Changed event.");
        }
        finally
        {
            theme.Changed -= handler;
        }
    }

    [Fact]
    public void ImplementsIHostThemeContract()
    {
        var theme = RevitHostTheme.Instance;
        Assert.IsAssignableFrom<IHostTheme>(theme);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RibbonIconFactory_AllIconsGenerateForDarkAndLight(bool isDark)
    {
        Assert.NotNull(RibbonIconFactory.CreateSetupIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateDetailingIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateScheduleIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateCuttingStockIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateExportExcelIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateBeamIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateColumnWallIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateFoundationIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateSlabIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateStairIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateEtabsIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateQsIcon(isDark));
        Assert.NotNull(RibbonIconFactory.CreateLayoutIcon(isDark));
    }

    [Fact]
    public void RibbonBuilder_PanelNamesAreDefined()
    {
        Assert.Equal("HPKata", RibbonBuilder.TabName);
        Assert.Equal("Cốt Thép (Rebar)", RibbonBuilder.PanelName);
        Assert.Equal("Dầm, Cột & Vách", RibbonBuilder.FramingColumnPanelName);
        Assert.Equal("Móng, Sàn & Thang", RibbonBuilder.FoundationSlabPanelName);
        Assert.Equal("QS & Bản Vẽ", RibbonBuilder.QsLayoutPanelName);
    }
}
