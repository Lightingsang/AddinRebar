using System.Linq;
using System.Reflection;
using HPKata.Core.Configuration;
using HPKata.Core.Models;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 12: Host neutrality (0 Autodesk dependencies in HPKata.Core) and reactive MVVM ViewModels.
/// </summary>
public sealed class Tier1_F12_HostNeutralityAndUiTests
{
    [Fact]
    public void HPKataCore_HasZeroAutodeskReferences()
    {
        // Arrange
        var coreAssembly = typeof(KataConfig).Assembly;

        // Act
        var referencedAssemblies = coreAssembly.GetReferencedAssemblies();

        // Assert
        Assert.DoesNotContain(referencedAssemblies, a =>
            a.Name?.StartsWith("Autodesk", System.StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(referencedAssemblies, a =>
            a.Name?.Contains("Revit", System.StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(referencedAssemblies, a =>
            a.Name?.Contains("AutoCAD", System.StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void DetailingViewModel_ChangingDimensions_RecalculatesCuttingLengthReactively()
    {
        // Arrange
        var vm = new DetailingViewModel
        {
            Diameter = 20,
            CountPerMember = 2,
            MemberCount = 1,
            L1 = 4000.0,
            L2 = 500.0
        };

        // Act: Change L1
        vm.L1 = 6000.0;

        // Assert: Cutting length updated reactively
        Assert.True(vm.CuttingLengthMm >= 6000.0);
        Assert.True(vm.TotalWeightKg > 0.0);
    }

    [Fact]
    public void SetupViewModel_SaveToConfig_ReflectsDirtyPropertyState()
    {
        // Arrange
        var config = new KataConfig { ConcreteCoverMm = 25.0 };
        var vm = new SetupViewModel(config);

        // Act
        vm.ConcreteCoverMm = 40.0;
        var updatedConfig = vm.ToConfig();

        // Assert
        Assert.Equal(40.0, updatedConfig.ConcreteCoverMm);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void ScheduleViewModel_SelectingItem_GeneratesQrPayloadReactively()
    {
        // Arrange
        var vm = new ScheduleViewModel();
        Assert.NotEmpty(vm.Items);

        // Act
        vm.SelectedItem = vm.Items[0];

        // Assert
        Assert.NotEmpty(vm.SelectedQrPayload);
        Assert.StartsWith("KATA|", vm.SelectedQrPayload);
    }

    [Fact]
    public void CuttingStockViewModel_SetsStandardBarLengthAndKerf()
    {
        // Arrange
        var vm = new CuttingStockViewModel
        {
            StandardStockLengthMm = 11700.0,
            SawKerfMm = 5.0,
            MinRemnantLengthMm = 500.0
        };

        // Assert
        Assert.Equal(11700.0, vm.StandardStockLengthMm);
        Assert.Equal(5.0, vm.SawKerfMm);
        Assert.Equal(500.0, vm.MinRemnantLengthMm);
    }
}
