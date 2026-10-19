using System;
using System.IO;
using System.Windows;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.E2E.Tests.Common;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Empirical challenge suite for UI Stress and Theme verification (Milestone M2, Feature 6).
/// Verifies all 13 WPF windows instantiate under Dark/Light themes on STA thread,
/// and verifies ViewModels handle missing files and failing/throwing adapters without unhandled exceptions.
/// </summary>
public sealed class Tier1_M2_ChallengerUiStressTests
{
    private static readonly Func<Window>[] WindowDefaultFactories =
    [
        () => new BeamDetailingWindow(),
        () => new ColumnDetailingWindow(),
        () => new CuttingStockView(),
        () => new DetailingView(),
        () => new DrawingUtilityWindow(),
        () => new EtabsImportWindow(),
        () => new FoundationDetailingWindow(),
        () => new QsTakeoffWindow(),
        () => new ScheduleView(),
        () => new SetupView(),
        () => new SlabDetailingWindow(),
        () => new StairLintelDetailingWindow(),
        () => new WallDetailingWindow()
    ];

    [Fact]
    public void All13Windows_InstantiateWithDefaultConstructors_UnderBothThemes()
    {
        E2EWpfHelper.RunOnSta(() =>
        {
            ApplyTheme("pack://application:,,,/HPKata.UI;component/Themes/ThemeDark.xaml");
            foreach (var factory in WindowDefaultFactories)
            {
                Window w = factory();
                Assert.NotNull(w);
                Assert.False(string.IsNullOrWhiteSpace(w.Title));
            }

            ApplyTheme("pack://application:,,,/HPKata.UI;component/Themes/ThemeLight.xaml");
            foreach (var factory in WindowDefaultFactories)
            {
                Window w = factory();
                Assert.NotNull(w);
                Assert.False(string.IsNullOrWhiteSpace(w.Title));
            }
        });
    }

    [Fact]
    public void BeamViewModel_MissingFileAndFailingAdapter_ZeroUnhandledExceptions()
    {
        string emptyDir = Path.Combine(Path.GetTempPath(), $"HPKata_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);
        try
        {
            using (KataInstallEnvironment.UseRootDirectory(emptyDir))
            {
                var failingAdapter = new FailingOrThrowingExcelAdapter(shouldThrow: false);
                var vm = new BeamDetailingViewModel(failingAdapter);

                vm.OpenExcelCommand.Execute(null);
                Assert.Contains("Không tìm thấy", vm.ExcelStatus);

                vm.ReadExcelCommand.Execute(null);
                Assert.Contains("Không thể đọc", vm.ExcelStatus);
                Assert.False(vm.IsExcelBusy);

                vm.WriteExcelCommand.Execute(null);
                Assert.Contains("Không thể lưu", vm.ExcelStatus);
                Assert.False(vm.IsExcelBusy);

                vm.AutoSyncOnApply = true;
                vm.ApplyCommand.Execute(null);
                Assert.False(vm.IsExcelBusy);
            }
        }
        finally
        {
            if (Directory.Exists(emptyDir)) Directory.Delete(emptyDir, true);
        }
    }

    [Fact]
    public void BeamViewModel_ThrowingAdapter_ZeroUnhandledExceptions()
    {
        var throwingAdapter = new FailingOrThrowingExcelAdapter(shouldThrow: true);
        var vm = new BeamDetailingViewModel(throwingAdapter);

        vm.ReadExcelCommand.Execute(null);
        Assert.Contains("Lỗi đọc", vm.ExcelStatus);
        Assert.False(vm.IsExcelBusy);

        vm.WriteExcelCommand.Execute(null);
        Assert.Contains("Lỗi ghi", vm.ExcelStatus);
        Assert.False(vm.IsExcelBusy);

        vm.AutoSyncOnApply = true;
        vm.ApplyCommand.Execute(null);
        Assert.False(vm.IsExcelBusy);
    }

    [Fact]
    public void ColumnViewModel_MissingFileAndThrowingAdapter_ZeroUnhandledExceptions()
    {
        string emptyDir = Path.Combine(Path.GetTempPath(), $"HPKata_ColTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);
        try
        {
            using (KataInstallEnvironment.UseRootDirectory(emptyDir))
            {
                var throwingAdapter = new FailingOrThrowingExcelAdapter(shouldThrow: true);
                var vm = new ColumnDetailingViewModel(throwingAdapter);

                vm.OpenExcelCommand.Execute(null);
                Assert.Contains("Không tìm thấy", vm.ExcelStatus);

                vm.ReadExcelCommand.Execute(null);
                Assert.Contains("Lỗi đọc", vm.ExcelStatus);
                Assert.False(vm.IsExcelBusy);

                vm.WriteExcelCommand.Execute(null);
                Assert.Contains("Lỗi ghi", vm.ExcelStatus);
                Assert.False(vm.IsExcelBusy);

                vm.AutoSyncOnApply = true;
                vm.ApplyCommand.Execute(null);
                Assert.False(vm.IsExcelBusy);
            }
        }
        finally
        {
            if (Directory.Exists(emptyDir)) Directory.Delete(emptyDir, true);
        }
    }

    [Fact]
    public void WallViewModel_MissingFileAndThrowingAdapter_ZeroUnhandledExceptions()
    {
        string emptyDir = Path.Combine(Path.GetTempPath(), $"HPKata_WallTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);
        try
        {
            using (KataInstallEnvironment.UseRootDirectory(emptyDir))
            {
                var throwingAdapter = new FailingOrThrowingExcelAdapter(shouldThrow: true);
                var vm = new WallDetailingViewModel(throwingAdapter);

                vm.OpenExcelCommand.Execute(null);
                Assert.Contains("Không tìm thấy", vm.ExcelStatus);

                vm.ReadExcelCommand.Execute(null);
                Assert.Contains("Lỗi đọc", vm.ExcelStatus);
                Assert.False(vm.IsExcelBusy);

                vm.WriteExcelCommand.Execute(null);
                Assert.Contains("Lỗi ghi", vm.ExcelStatus);
                Assert.False(vm.IsExcelBusy);

                vm.AutoSyncOnApply = true;
                vm.ApplyCommand.Execute(null);
                Assert.False(vm.IsExcelBusy);
            }
        }
        finally
        {
            if (Directory.Exists(emptyDir)) Directory.Delete(emptyDir, true);
        }
    }

    [Fact]
    public void ViewModels_WithDegenerateEmptyData_ApplySafely()
    {
        var beamVm = new BeamDetailingViewModel();
        beamVm.Spans.Clear();
        beamVm.ApplyCommand.Execute(null);

        var colVm = new ColumnDetailingViewModel();
        colVm.Stories.Clear();
        colVm.ApplyCommand.Execute(null);
        Assert.NotNull(colVm.ResultLayout);

        var wallVm = new WallDetailingViewModel();
        wallVm.StoryCount = 0;
        wallVm.Openings.Clear();
        wallVm.ApplyCommand.Execute(null);
        Assert.NotNull(wallVm.ResultModel);
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

    private sealed class FailingOrThrowingExcelAdapter : IKataExcelAdapter
    {
        private readonly bool _shouldThrow;

        public FailingOrThrowingExcelAdapter(bool shouldThrow) => _shouldThrow = shouldThrow;

        public bool IsDesktopExcelOpen => false;

        public bool ReadBeamSheet(string? path, out KataBeamSheetData data)
        {
            if (_shouldThrow) throw new IOException("Disk error while reading Dam sheet");
            data = new KataBeamSheetData();
            return false;
        }

        public bool WriteBeamSheet(string? path, in KataBeamSheetData data)
        {
            if (_shouldThrow) throw new UnauthorizedAccessException("Cannot write Dam sheet");
            return false;
        }

        public bool ReadColumnSheet(string? path, out KataColumnSheetData data)
        {
            if (_shouldThrow) throw new IOException("Disk error while reading Cot sheet");
            data = new KataColumnSheetData();
            return false;
        }

        public bool WriteColumnSheet(string? path, in KataColumnSheetData data)
        {
            if (_shouldThrow) throw new UnauthorizedAccessException("Cannot write Cot sheet");
            return false;
        }

        public bool ReadWallSheet(string? path, out KataWallSheetData data)
        {
            if (_shouldThrow) throw new IOException("Disk error while reading Vach sheet");
            data = new KataWallSheetData();
            return false;
        }

        public bool WriteWallSheet(string? path, in KataWallSheetData data)
        {
            if (_shouldThrow) throw new UnauthorizedAccessException("Cannot write Vach sheet");
            return false;
        }

        public bool ExportSchedule(string? path, System.Collections.Generic.IEnumerable<KataRebarScheduleItem> items) => false;
        public bool ExportSchedule(string? path, System.Collections.Generic.IEnumerable<HPKata.Core.Models.RebarItem> items) => false;
        public bool ExportQhPlusSchedule(string? path, System.Collections.Generic.IEnumerable<KataRebarScheduleItem> items) => false;
        public bool ExportQhPlusSchedule(string? path, System.Collections.Generic.IEnumerable<HPKata.Core.Models.RebarItem> items) => false;
    }
}
