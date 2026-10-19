using HPKata.Core.Configuration;
using HPKata.Core.Models;
using HPKata.UI.Common;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.E2E.Tests.Tier3_CrossFeature;

/// <summary>
/// Tier 3 Pipeline: Reactive UI ViewModels, config mutations, detailing geometry bindings, and schedule metrics.
/// </summary>
public sealed class Tier3_ReactiveUiConfigPipelineTests
{
    [Fact]
    public void Pipeline_SetupConfigMutation_PropagatesToDetailingRules()
    {
        // 1. Arrange: SetupViewModel with initial defaults
        var setupVm = new SetupViewModel();
        Assert.False(setupVm.IsDirty);

        // 2. Mutate configuration parameters
        setupVm.ConcreteCoverMm = 35.0;
        setupVm.BaseBarDiameter = 22;
        setupVm.LapLengthMultiplier = 45.0;
        Assert.True(setupVm.IsDirty);

        // 3. Export to strongly typed KataConfig
        var updatedConfig = setupVm.ToConfig();
        Assert.Equal(35.0, updatedConfig.ConcreteCoverMm);
        Assert.Equal(22, updatedConfig.BaseBarDiameter);
        Assert.Equal(45.0, updatedConfig.LapLengthMultiplier);

        // 4. Initialize DetailingViewModel using updated defaults
        var detailingVm = new DetailingViewModel
        {
            Diameter = updatedConfig.BaseBarDiameter,
            L1 = 5000.0,
            L2 = 400.0
        };
        Assert.Equal(22, detailingVm.Diameter);
        Assert.Equal(5000.0, detailingVm.CuttingLengthMm);
    }

    [Fact]
    public void Pipeline_DetailingViewModelItemCreation_UpdatesScheduleMetrics()
    {
        // 1. DetailingViewModel inputs
        var detailingVm = new DetailingViewModel
        {
            ComponentMark = "B301",
            BarMark = "1",
            Diameter = 25,
            CountPerMember = 4,
            MemberCount = 2,
            L1 = 6000.0,
            L2 = 500.0,
            L3 = 500.0
        };

        // 2. Build RebarItem from ViewModel state
        var item = new RebarItem
        {
            ComponentMark = detailingVm.ComponentMark,
            BarMark = detailingVm.BarMark,
            DiameterMm = detailingVm.Diameter,
            CountPerMember = detailingVm.CountPerMember,
            MemberCount = detailingVm.MemberCount,
            CuttingLengthMm = detailingVm.CuttingLengthMm
        };

        // 3. ScheduleViewModel loaded with item
        var scheduleVm = new ScheduleViewModel();
        scheduleVm.Items.Clear();
        scheduleVm.Items.Add(item);

        // 4. Assert: total bar count = 4 * 2 = 8 bars
        Assert.Single(scheduleVm.Items);
        Assert.Equal(8, scheduleVm.Items[0].TotalCount);
    }

    [Fact]
    public void Pipeline_HostThemeContract_NotifiesSubscribersOnThemeToggle()
    {
        // Arrange: Custom mock host theme implementing IHostTheme
        var hostTheme = new MockHostTheme();
        int changedCount = 0;
        hostTheme.Changed += () => changedCount++;

        // Act: Toggle theme twice
        hostTheme.ToggleTheme();
        hostTheme.ToggleTheme();

        // Assert: 2 events triggered, theme state correctly tracked
        Assert.Equal(2, changedCount);
        Assert.False(hostTheme.IsDark);
    }

    private sealed class MockHostTheme : IHostTheme
    {
        public bool IsDark { get; private set; }
        public event System.Action? Changed;

        public void ToggleTheme()
        {
            IsDark = !IsDark;
            Changed?.Invoke();
        }
    }

    [Fact]
    public void Pipeline_ScheduleViewModel_SelectingDifferentItems_UpdatesQrBarcodePayload()
    {
        // Arrange
        var vm = new ScheduleViewModel();
        Assert.True(vm.Items.Count >= 2);

        // Act: Select first item, then second item
        vm.SelectedItem = vm.Items[0];
        string payload1 = vm.SelectedQrPayload;

        vm.SelectedItem = vm.Items[1];
        string payload2 = vm.SelectedQrPayload;

        // Assert: Payloads are distinct and correspond to selected items
        Assert.NotEmpty(payload1);
        Assert.NotEmpty(payload2);
        Assert.NotEqual(payload1, payload2);
    }
}
