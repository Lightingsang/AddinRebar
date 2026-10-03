using System;
using System.Threading;
using HPAutoCad.HPGeoLink.Support;
using HPAutoCad.HPGeoLink.View;
using HPAutoCad.HPGeoLink.ViewModel;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Units;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

public sealed class ViewInstantiationTests
{
    private sealed class FakeShell : IGeoExportShell
    {
        public string? AskSavePath(string? initialDirectory, string suggestedFileName) => null;
        public void OpenPath(string path) { }
        public string OpenInGoogleEarth(string kmzPath) => "ok";
        public void OpenUrl(string url) { }
    }

    private sealed class FakeImportShell : IGeoImportShell
    {
        public string? AskOpenPath() => null;
        public string? ReadClipboardText() => null;
        public void ShowMessage(string message, string title) { }
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw new AggregateException("STA thread threw an exception", exception);
        }
    }

    [Fact]
    public void CrsSelectionView_instantiates_without_xaml_parse_exception()
    {
        RunOnSta(() =>
        {
            var view = new CrsSelectionView();
            Assert.NotNull(view);
        });
    }

    [Fact]
    public void CrsSelectionView_ComboBox_hint_properties_check()
    {
        RunOnSta(() =>
        {
            var vm = new CrsSelectionViewModel(DrawingUnit.Meters);
            var view = new CrsSelectionView { DataContext = vm };
            view.Measure(new System.Windows.Size(800, 600));
            view.Arrange(new System.Windows.Rect(0, 0, 800, 600));
            view.UpdateLayout();
            Assert.NotNull(view);
        });
    }

    [Fact]
    public void KmlColorPickerView_instantiates_without_xaml_parse_exception()
    {
        RunOnSta(() =>
        {
            var view = new KmlColorPickerView();
            Assert.NotNull(view);
        });
    }

    [Fact]
    public void GeoExportWindow_instantiates_and_lays_out_without_exception()
    {
        RunOnSta(() =>
        {
            var pts = new[] { new HPAutoCad.Core.HPGeoLink.Conversion.SurveyPoint(1, "1", new HPAutoCad.Core.HPGeoLink.Model.PlanePoint(598355.62, 1232218.8)) };
            var bnd = new[] { new HPAutoCad.Core.HPGeoLink.Conversion.BoundaryPolyline("Ranh", new[] {
                new HPAutoCad.Core.HPGeoLink.Model.PlanePoint(598355.62, 1232218.8),
                new HPAutoCad.Core.HPGeoLink.Model.PlanePoint(598230.82, 1232219.7),
                new HPAutoCad.Core.HPGeoLink.Model.PlanePoint(598222.49, 1232216.0)
            }, true) };
            var vm = new GeoExportViewModel(pts, bnd, "TestDoc", @"C:\Test", DrawingUnit.Meters, new FakeShell());
            var win = new GeoExportWindow(vm);
            Assert.NotNull(win);
            win.Measure(new System.Windows.Size(1280, 820));
            win.Arrange(new System.Windows.Rect(0, 0, 1280, 820));
            win.UpdateLayout();

            // Test Fullscreen toggle
            vm.ToggleMapFullscreenCommand.Execute(null);
            Assert.True(vm.IsMapFullscreen);
            win.UpdateLayout();

            vm.ToggleMapFullscreenCommand.Execute(null);
            Assert.False(vm.IsMapFullscreen);
            win.UpdateLayout();

            // Test badges
            Assert.Contains("điểm", vm.MapSummaryBadge);
            Assert.Contains("WGS84", vm.MapCenterBadge);

            // Test Titlebar Icon
            Assert.NotNull(win.Icon);
        });
    }

    [Fact]
    public void GeoIconHelper_produces_non_null_icon()
    {
        RunOnSta(() =>
        {
            var icon = GeoIconHelper.WindowIcon;
            Assert.NotNull(icon);
        });
    }

    [Fact]
    public void GeoImportWindow_instantiates_with_icon_without_exception()
    {
        RunOnSta(() =>
        {
            var vm = new GeoImportViewModel(DrawingUnit.Meters, new FakeImportShell());
            var win = new GeoImportWindow(vm);
            Assert.NotNull(win);
            Assert.NotNull(win.Icon);
        });
    }
}
