using System;
using System.Threading;
using HPDcePro.UI.Common;
using HPDcePro.UI.Views;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class WindowInstantiationTests
    {
        private static void RunOnStaThread(Action action)
        {
            Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    // Reason: Capture STA thread exception to propagate back to test runner
                    caught = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught != null)
            {
                throw new InvalidOperationException($"STA thread failed: {caught.Message}", caught);
            }
        }

        [Fact]
        public void BeamDetailingWindow_InstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new BeamDetailingWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }

        [Fact]
        public void CuttingOptimizationWindow_InstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new CuttingOptimizationWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }

        [Fact]
        public void AutoStrConfigWindow_InstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new AutoStrConfigWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }
    }
}
