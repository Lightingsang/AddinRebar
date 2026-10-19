using System;
using HPKata.AutoCad.Ribbon;
using Xunit;

namespace HPKata.AutoCad.Tests.Controllers
{
    public sealed class AutocadRibbonTests
    {
        [Fact]
        public void HPKataRibbonTab_ConstantsMatchContract()
        {
            Assert.Equal("HPKATA_RIBBON_TAB", HPKataRibbonTab.TabId);
            Assert.Equal("HPKata", HPKataRibbonTab.TabTitle);
        }

        [Fact]
        public void HPKataRibbonPanels_PanelTitlesMatchRevit()
        {
            Assert.Equal("Cốt Thép (Rebar)", HPKataRibbonPanels.CorePanelTitle);
            Assert.Equal("Dầm, Cột & Vách", HPKataRibbonPanels.FramingColPanelTitle);
            Assert.Equal("Móng, Sàn & Thang", HPKataRibbonPanels.FoundSlabPanelTitle);
            Assert.Equal("QS & Bản Vẽ", HPKataRibbonPanels.QsLayoutPanelTitle);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AutocadRibbonIconFactory_AllIconsGenerateForDarkAndLight(bool isDark)
        {
            Assert.NotNull(AutocadRibbonIconFactory.CreateSetupIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateDetailingIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateScheduleIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateCuttingStockIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateExportExcelIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateBeamIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateColumnWallIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateFoundationIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateSlabIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateStairIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateEtabsIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateQsIcon(isDark));
            Assert.NotNull(AutocadRibbonIconFactory.CreateLayoutIcon(isDark));
        }

        [Fact]
        public void RibbonCommandHandler_ExecutesActionAndGuardsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RibbonCommandHandler(null!, () => { }));
            Assert.Throws<ArgumentNullException>(() => new RibbonCommandHandler("CMD", null!));

            bool executed = false;
            var handler = new RibbonCommandHandler("HPKATA_TEST", () => executed = true);
            Assert.True(handler.CanExecute(null));
            handler.Execute(null);
            Assert.True(executed);
        }
    }
}
