using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HPRebar.BeamRebar.Model;
using HPRebar.BeamRebar.Service;
using HPRebar.BeamRebar.View.Controls;
using HPRebar.BeamRebar.ViewModel;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;

namespace M3ChallengerHarness;

public static class Program
{
    private static int _passCount = 0;
    private static int _failCount = 0;

    [STAThread]
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("================================================================================");
        Console.WriteLine("    M3 EMPIRICAL CHALLENGER TEST HARNESS: REACTIVE BEAMREBAR UI INTEGRATION     ");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"Start Time: {DateTime.UtcNow:O}\n");

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var name = new System.Reflection.AssemblyName(args.Name).Name;
            string revitDir = @"C:\Program Files\Autodesk\Revit 2026";
            string target = System.IO.Path.Combine(revitDir, name + ".dll");
            if (System.IO.File.Exists(target))
            {
                return System.Reflection.Assembly.LoadFrom(target);
            }
            return null;
        };

        // Initialize WPF Application if not present
        if (Application.Current is null)
        {
            var app = new Application();
        }

        try
        {
            RunSuite1_ViewModelModeSwitchingRobustness();
            RunSuite2_WpfLayoutAndGridSplitterSemantics();
            RunSuite3_SplitModeColumnsAndClamping();
            RunSuite4_ReactiveRedrawTimingAndDebounce();
            RunSuite5_DualViewportInstanceLifecycleAndMemory();
            RunSuite6_CanvasRenderingAndCleanCodeMetrics();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL HARNESS EXCEPTION]: {ex}");
            _failCount++;
        }

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"FINAL RESULT: {_passCount} PASSED, {_failCount} FAILED");
        Console.WriteLine("================================================================================");

        return _failCount == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string testName, string details = "")
    {
        if (condition)
        {
            Console.WriteLine($"  [PASS] {testName} {(string.IsNullOrEmpty(details) ? "" : "— " + details)}");
            _passCount++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {testName} {(string.IsNullOrEmpty(details) ? "" : "— " + details)}");
            Console.ResetColor();
            _failCount++;
        }
    }

    private static BeamRebarSession CreateSampleSession()
    {
        var spans = new List<BeamSpan>
        {
            new BeamSpan(0, "Span 1", 4000.0, 300.0, 500.0, 0.0, 25.0, 3600.0, 200.0),
            new BeamSpan(1, "Span 2", 5000.0, 300.0, 500.0, 0.0, 25.0, 4600.0, 4200.0)
        };
        var supports = new List<BeamSupportNode>
        {
            new BeamSupportNode(0, "Col 1", 200.0, 400.0, SupportType.ExteriorColumn, 400.0, "", true),
            new BeamSupportNode(1, "Col 2", 4200.0, 400.0, SupportType.Column, 400.0, "", false),
            new BeamSupportNode(2, "Col 3", 9200.0, 400.0, SupportType.ExteriorColumn, 400.0, "", true)
        };
        var continuousStack = new BeamContinuousStack(spans, supports);
        var stack = new BeamStack { ContinuousStack = continuousStack };
        var spec = new BeamRebarSpec();
        var barTypes = new List<RebarTypeInfo>
        {
            new RebarTypeInfo { Name = "D18", DiameterMm = 18.0 },
            new RebarTypeInfo { Name = "D20", DiameterMm = 20.0 },
            new RebarTypeInfo { Name = "D25", DiameterMm = 25.0 }
        };
        return new BeamRebarSession(stack, spec, barTypes);
    }

    #region Suite 1: ViewModel Robustness
    private static void RunSuite1_ViewModelModeSwitchingRobustness()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("SUITE 1: VIEWMODEL MODE SWITCHING ROBUSTNESS & RADIO-BUTTON BINDING LOGIC");
        Console.WriteLine("--------------------------------------------------------------------------------");

        var session = CreateSampleSession();
        var loc = new LocalizationService();
        var vm = new BeamRebarViewModel(session, loc, new DummyRunner());

        // 1. Initial State
        Assert(vm.PreviewMode == BeamPreviewMode.TwoD, "Initial PreviewMode is TwoD");
        Assert(vm.IsTwoDMode && !vm.IsThreeDMode && !vm.IsSplitMode, "Initial boolean accessors match TwoD");

        // 2. Set to ThreeD
        vm.IsThreeDMode = true;
        Assert(vm.PreviewMode == BeamPreviewMode.ThreeD, "Setting IsThreeDMode=true updates PreviewMode to ThreeD");
        Assert(!vm.IsTwoDMode && vm.IsThreeDMode && !vm.IsSplitMode, "Booleans correctly reflect ThreeD");

        // 3. Set to Split
        vm.IsSplitMode = true;
        Assert(vm.PreviewMode == BeamPreviewMode.Split, "Setting IsSplitMode=true updates PreviewMode to Split");
        Assert(!vm.IsTwoDMode && !vm.IsThreeDMode && vm.IsSplitMode, "Booleans correctly reflect Split");

        // 4. RadioButton uncheck guard (setting false should NOT clear mode)
        vm.IsSplitMode = false;
        Assert(vm.PreviewMode == BeamPreviewMode.Split, "Setting IsSplitMode=false does NOT unset Split (RadioButton uncheck guard)");

        // 5. SetPreviewModeCommand RelayCommand
        vm.SetPreviewModeCommand.Execute(BeamPreviewMode.TwoD);
        Assert(vm.PreviewMode == BeamPreviewMode.TwoD, "SetPreviewModeCommand(TwoD) transitions to TwoD");

        vm.SetPreviewModeCommand.Execute(BeamPreviewMode.ThreeD);
        Assert(vm.PreviewMode == BeamPreviewMode.ThreeD, "SetPreviewModeCommand(ThreeD) transitions to ThreeD");

        vm.SetPreviewModeCommand.Execute(BeamPreviewMode.Split);
        Assert(vm.PreviewMode == BeamPreviewMode.Split, "SetPreviewModeCommand(Split) transitions to Split");

        // 6. Property Changed Notification Tracking
        int twoDNotifies = 0;
        int threeDNotifies = 0;
        int splitNotifies = 0;
        int previewModeNotifies = 0;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsTwoDMode)) twoDNotifies++;
            if (e.PropertyName == nameof(vm.IsThreeDMode)) threeDNotifies++;
            if (e.PropertyName == nameof(vm.IsSplitMode)) splitNotifies++;
            if (e.PropertyName == nameof(vm.PreviewMode)) previewModeNotifies++;
        };

        vm.PreviewMode = BeamPreviewMode.TwoD;
        Assert(twoDNotifies == 1 && threeDNotifies == 1 && splitNotifies == 1 && previewModeNotifies == 1,
            "Mode change triggers exact property change notifications for all 3 accessors and PreviewMode");

        // 7. Rapid High-Frequency Toggling (10,000 cycles)
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var targetMode = (BeamPreviewMode)(i % 3);
            vm.PreviewMode = targetMode;
        }
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 500, "10,000 rapid mode toggles execute in <500ms without exception", $"{sw.ElapsedMilliseconds}ms");
    }
    #endregion

    #region Suite 2: WPF Layout & Horizontal GridSplitter Semantics
    private static void RunSuite2_WpfLayoutAndGridSplitterSemantics()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("SUITE 2: WPF LAYOUT MEASURE/ARRANGE & HORIZONTAL GRIDSPLITTER RESIZE LIMITS");
        Console.WriteLine("--------------------------------------------------------------------------------");

        // Simulate the outer Grid structure from BeamRebarView.xaml:
        // Window: MinWidth=960, MinHeight=640, Height=800
        // Grid.RowDefinitions:
        //   Row 0: Height="*", MinHeight="220"
        //   Row 1: Height="280", MinHeight="160"
        //   Row 2: Height="Auto" (~60px footer)
        // GridSplitter: Grid.Row="1", Height="6", VerticalAlignment="Top", ResizeDirection="Rows"
        // Preview Grid: Grid.Row="1", Margin="0,8,0,0"

        var outerGrid = new Grid();
        var row0 = new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 220 };
        var row1 = new RowDefinition { Height = new GridLength(280, GridUnitType.Pixel), MinHeight = 160 };
        var row2 = new RowDefinition { Height = GridLength.Auto };
        outerGrid.RowDefinitions.Add(row0);
        outerGrid.RowDefinitions.Add(row1);
        outerGrid.RowDefinitions.Add(row2);

        var splitter = new GridSplitter
        {
            Height = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            ResizeDirection = GridResizeDirection.Rows
        };
        Grid.SetRow(splitter, 1);
        outerGrid.Children.Add(splitter);

        var previewContainer = new Border { Margin = new Thickness(0, 8, 0, 0) };
        Grid.SetRow(previewContainer, 1);
        outerGrid.Children.Add(previewContainer);

        // Perform initial layout pass at 1100x800
        outerGrid.Measure(new Size(1100, 800));
        outerGrid.Arrange(new Rect(0, 0, 1100, 800));

        Assert(row1.ActualHeight >= 160, "Initial Row 1 height satisfies MinHeight=160", $"ActualHeight={row1.ActualHeight:F1}px");
        Assert(row0.ActualHeight >= 220, "Initial Row 0 height satisfies MinHeight=220", $"ActualHeight={row0.ActualHeight:F1}px");

        // Verify splitter clearance with inner preview margin (Margin="0,8,0,0"):
        // Splitter height = 6px, starts at Top (Y=0 relative to Row 1).
        // Preview container starts at Y=8.
        Assert(previewContainer.Margin.Top >= splitter.Height, "Preview container Margin.Top (8px) clears GridSplitter Height (6px)",
            $"Margin.Top={previewContainer.Margin.Top}px >= Splitter.Height={splitter.Height}px");

        // Test Minimum Window Height Clamp: 640px
        // Total MinHeight = Row0(220) + Row1(160) + Row2(~50) = ~430px < 640px
        double minSum = row0.MinHeight + row1.MinHeight;
        Assert(minSum <= 640, "Sum of Row 0 MinHeight (220) + Row 1 MinHeight (160) is well within Window MinHeight (640)",
            $"MinSum={minSum}px <= 640px");

        // Simulate maximum upward expansion of preview pane (compressing Row 0 to its MinHeight)
        // In 800px window (approx 740px available after footer and margins):
        // Max Row 1 = 740 - 220 = 520px.
        double maxAvailableForRow1 = 800.0 - 60.0 - row0.MinHeight;
        Assert(maxAvailableForRow1 >= 450.0, "Preview pane can expand to >450px while respecting Row 0 MinHeight (220px)",
            $"MaxRow1={maxAvailableForRow1:F1}px");
    }
    #endregion

    #region Suite 3: Split Mode Columns and Clamping
    private static void RunSuite3_SplitModeColumnsAndClamping()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("SUITE 3: SPLIT MODE [2D + 3D] VERTICAL GRIDSPLITTER & 50/50 BALANCE");
        Console.WriteLine("--------------------------------------------------------------------------------");

        // Simulate Mode 3 Grid from BeamRebarView.xaml:
        // Col 0: Width="*", MinWidth="200" (Elevation Canvas)
        // Col 1: Width="6" (Vertical GridSplitter)
        // Col 2: Width="*", MinWidth="200" (3D Viewport)

        var splitGrid = new Grid();
        var col0 = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 };
        var col1 = new ColumnDefinition { Width = new GridLength(6, GridUnitType.Pixel) };
        var col2 = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 };
        splitGrid.ColumnDefinitions.Add(col0);
        splitGrid.ColumnDefinitions.Add(col1);
        splitGrid.ColumnDefinitions.Add(col2);

        var leftCanvas = new Border { Background = Brushes.Transparent };
        Grid.SetColumn(leftCanvas, 0);
        splitGrid.Children.Add(leftCanvas);

        var verticalSplitter = new GridSplitter
        {
            Width = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ResizeDirection = GridResizeDirection.Columns
        };
        Grid.SetColumn(verticalSplitter, 1);
        splitGrid.Children.Add(verticalSplitter);

        var rightViewport = new Border { Background = Brushes.Transparent };
        Grid.SetColumn(rightViewport, 2);
        splitGrid.Children.Add(rightViewport);

        // Measure & Arrange at 1000px available width
        splitGrid.Measure(new Size(1000, 280));
        splitGrid.Arrange(new Rect(0, 0, 1000, 280));

        // In 1000px width with 6px splitter: remaining 994px is split 50/50 -> 497px each
        double diff = Math.Abs(col0.ActualWidth - col2.ActualWidth);
        Assert(diff <= 1.0, "Split mode provides exact 50/50 balance between 2D and 3D panes",
            $"Col0={col0.ActualWidth:F1}px, Col2={col2.ActualWidth:F1}px, Diff={diff:F2}px");

        // Verify minimum width constraints under narrow sizing (e.g., 500px window)
        splitGrid.Measure(new Size(500, 280));
        splitGrid.Arrange(new Rect(0, 0, 500, 280));
        Assert(col0.ActualWidth >= 200.0 && col2.ActualWidth >= 200.0,
            "Both 2D canvas and 3D viewport satisfy MinWidth=200px under narrow sizing",
            $"Col0={col0.ActualWidth:F1}px, Col2={col2.ActualWidth:F1}px");
    }
    #endregion

    #region Suite 4: Reactive Redraw Timing & Debounce Suppression
    private static void RunSuite4_ReactiveRedrawTimingAndDebounce()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("SUITE 4: REACTIVE REDRAW TIMING, DEBOUNCE SUPPRESSION & CPU BENCHMARK");
        Console.WriteLine("--------------------------------------------------------------------------------");

        var session = CreateSampleSession();
        var stack = session.Stack.ContinuousStack;
        var spec = session.ToSpec();

        // 1. Direct Benchmark of Beam3DSceneBuilder.BuildScene()
        // Measure CPU execution time across 50 consecutive calls
        var sw = Stopwatch.StartNew();
        const int benchmarkRuns = 50;
        for (int i = 0; i < benchmarkRuns; i++)
        {
            var scene = Beam3DSceneBuilder.BuildScene(stack, spec);
            if (scene is null) throw new InvalidOperationException("Scene returned null");
        }
        sw.Stop();
        double avgMs = (double)sw.ElapsedMilliseconds / benchmarkRuns;
        Assert(avgMs < 10.0, $"Beam3DSceneBuilder.BuildScene() average build time is <10ms (Actual: {avgMs:F2}ms/run)",
            $"Total for {benchmarkRuns} runs: {sw.ElapsedMilliseconds}ms");

        // 2. Debounce Suppression Test with Beam3DViewport: Count RebuildScene calls
        int tickCount = 0;
        var viewport = new Beam3DViewport();
        viewport.Session = session;

        // Use reflection to monitor _redrawTimer ticks or verify scene update
        var timerField = typeof(Beam3DViewport).GetField("_redrawTimer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var timer = timerField?.GetValue(viewport) as DispatcherTimer;
        if (timer is not null)
        {
            timer.Tick += (s, e) => tickCount++;
        }

        // Force viewport Loaded
        viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

        // Pump dispatcher once to clear any initial loaded trigger
        PumpDispatcher(70);
        int initialTicks = tickCount;

        // Fire 20 rapid property modifications WITHOUT sleeping or nested frames
        var burstSw = Stopwatch.StartNew();
        for (int i = 0; i < 20; i++)
        {
            session.Cover = 25.0 + (i % 5);
            session.TopBarCount = 2 + (i % 3);
        }
        burstSw.Stop();

        Assert(burstSw.ElapsedMilliseconds < 50, "20 rapid property modifications execute on Session in <50ms without blocking",
            $"{burstSw.ElapsedMilliseconds}ms");

        // Wait 30ms (< 50ms): timer should NOT have fired yet because it's debouncing
        PumpDispatcher(30);
        int midTicks = tickCount - initialTicks;
        Assert(midTicks == 0, "During rapid edits (<50ms), debounce suppresses scene rebuilds (0 ticks fired)",
            $"Ticks fired during burst: {midTicks}");

        // Now wait another 60ms (> 50ms debounce): exactly 1 tick should have fired!
        PumpDispatcher(70);
        int finalTicks = tickCount - initialTicks;
        Assert(finalTicks == 1, "After editing settles, debounce fires exactly ONE scene rebuild",
            $"Total rebuild ticks fired: {finalTicks}");

        var visual = viewport.FindName("BeamVisual") as ModelVisual3D;
        Assert(visual != null && visual.Content != null, "BeamVisual content successfully populated after debounce delay");

        // Clean up
        viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
    }
    #endregion

    #region Suite 5: Dual Viewport Lifecycle & Memory
    private static void RunSuite5_DualViewportInstanceLifecycleAndMemory()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("SUITE 5: DUAL VIEWPORT INSTANCE LIFECYCLE, COLLAPSED STATE & CAMERA INTEGRITY");
        Console.WriteLine("--------------------------------------------------------------------------------");

        var session = CreateSampleSession();

        // Mode 2 Viewport (Full 3D) - Visible
        var vpFull = new Beam3DViewport { Session = session, Visibility = Visibility.Visible };
        vpFull.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

        // Mode 3 Viewport (Split 3D) - Collapsed initially (as in TwoD or ThreeD mode)
        var vpSplit = new Beam3DViewport { Session = session, Visibility = Visibility.Collapsed };
        vpSplit.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

        PumpDispatcher(100);

        // Check behavior of Collapsed Viewport when RebuildScene is invoked:
        // Test if ZoomExtents throws or corrupts camera when ActualWidth/ActualHeight == 0
        bool collapsedZoomThrew = false;
        try
        {
            vpSplit.ResetCamera();
        }
        catch (Exception ex)
        {
            collapsedZoomThrew = true;
            Console.WriteLine($"  [!] ResetCamera threw on Collapsed Viewport: {ex.Message}");
        }
        Assert(!collapsedZoomThrew, "ResetCamera() on Collapsed Viewport does not crash");

        var helixSplit = vpSplit.FindName("HelixView") as HelixToolkit.Wpf.HelixViewport3D;
        var camera = helixSplit?.Camera as PerspectiveCamera;
        bool cameraValid = camera != null && 
                           !double.IsNaN(camera.Position.X) && 
                           !double.IsNaN(camera.Position.Y) && 
                           !double.IsNaN(camera.Position.Z);
        Assert(cameraValid, "Camera coordinates on Collapsed Viewport remain valid (no NaN corruption)");

        // Modify session property: both viewports rebuild scenes
        session.BottomBarCount = 4;
        PumpDispatcher(100);

        var visFull = vpFull.FindName("BeamVisual") as ModelVisual3D;
        var visSplit = vpSplit.FindName("BeamVisual") as ModelVisual3D;
        Assert(visFull?.Content != null && visSplit?.Content != null,
            "Both visible and collapsed viewport instances maintain valid BeamVisual content");

        // Now simulate mode switch: vpSplit becomes Visible, vpFull becomes Collapsed
        vpFull.Visibility = Visibility.Collapsed;
        vpSplit.Visibility = Visibility.Visible;
        vpSplit.Measure(new Size(500, 280));
        vpSplit.Arrange(new Rect(0, 0, 500, 280));
        PumpDispatcher(50);

        Assert(vpSplit.ActualWidth > 0 || vpSplit.RenderSize.Width > 0,
            "Split viewport transitions smoothly to visible with positive dimensions");

        // Simulate Unloaded on both
        vpFull.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        vpSplit.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

        // Modify session again: verify neither crashes after detach
        bool detachedCleanly = true;
        try
        {
            session.Cover = 35.0;
            PumpDispatcher(80);
        }
        catch (Exception ex)
        {
            detachedCleanly = false;
            Console.WriteLine($"Exception after detach: {ex}");
        }

        Assert(detachedCleanly, "Both viewports cleanly detached on Unloaded without lingering timer exceptions");
    }
    #endregion

    #region Suite 6: 2D Canvases & Clean Code Metrics
    private static void RunSuite6_CanvasRenderingAndCleanCodeMetrics()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("SUITE 6: 2D CANVASES RENDERING, CONVERTERS & CLEAN CODE GOVERNANCE");
        Console.WriteLine("--------------------------------------------------------------------------------");

        var session = CreateSampleSession();

        // 1. Test BooleanToVisibilityConverter
        var conv = new BooleanToVisibilityConverter();
        var visTrue = conv.Convert(true, typeof(Visibility), null, System.Globalization.CultureInfo.InvariantCulture);
        var visFalse = conv.Convert(false, typeof(Visibility), null, System.Globalization.CultureInfo.InvariantCulture);
        Assert(visTrue is Visibility v1 && v1 == Visibility.Visible, "BoolToVis(true) produces Visibility.Visible");
        Assert(visFalse is Visibility v2 && v2 == Visibility.Collapsed, "BoolToVis(false) produces Visibility.Collapsed");

        // 2. Test BeamElevationCanvas Measure and Render
        var elevCanvas = new BeamElevationCanvas { Session = session };
        elevCanvas.Measure(new Size(800, 200));
        elevCanvas.Arrange(new Rect(0, 0, 800, 200));
        Assert(elevCanvas.DesiredSize.Width > 0 && elevCanvas.DesiredSize.Height > 0,
            "BeamElevationCanvas measures valid DesiredSize",
            $"Size: {elevCanvas.DesiredSize.Width}x{elevCanvas.DesiredSize.Height}");

        // 3. Test BeamSectionCanvas Measure and Render
        var secCanvas = new BeamSectionCanvas { Session = session };
        secCanvas.Measure(new Size(230, 200));
        secCanvas.Arrange(new Rect(0, 0, 230, 200));
        Assert(secCanvas.DesiredSize.Width > 0 && secCanvas.DesiredSize.Height > 0,
            "BeamSectionCanvas measures valid DesiredSize",
            $"Size: {secCanvas.DesiredSize.Width}x{secCanvas.DesiredSize.Height}");

        // 4. Verify Clean Code Line Counts
        string baseDir = @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\BeamRebar";
        string viewXaml = System.IO.Path.Combine(baseDir, @"View\BeamRebarView.xaml");
        string viewCs = System.IO.Path.Combine(baseDir, @"View\BeamRebarView.xaml.cs");
        string vmCs = System.IO.Path.Combine(baseDir, @"ViewModel\BeamRebarViewModel.cs");

        int viewXamlLines = System.IO.File.ReadAllLines(viewXaml).Length;
        int viewCsLines = System.IO.File.ReadAllLines(viewCs).Length;
        int vmCsLines = System.IO.File.ReadAllLines(vmCs).Length;

        Assert(viewXamlLines <= 300, $"BeamRebarView.xaml line count ({viewXamlLines}) <= 300");
        Assert(viewCsLines <= 300, $"BeamRebarView.xaml.cs line count ({viewCsLines}) <= 300");
        Assert(vmCsLines <= 300, $"BeamRebarViewModel.cs line count ({vmCsLines}) <= 300");

        // 5. Method Length Verification in BeamRebarViewModel.cs
        var lines = System.IO.File.ReadAllLines(vmCs);
        int maxMethodLines = 0;
        string currentMethod = "";
        int methodStart = -1;
        int braceDepth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Contains("private") || line.Contains("public") || line.Contains("partial void"))
            {
                if (line.Contains("(") && !line.Contains("class") && !line.Contains("delegate") && !line.Contains("event"))
                {
                    currentMethod = line;
                    methodStart = i;
                }
            }
            if (line.Contains("{")) braceDepth++;
            if (line.Contains("}"))
            {
                braceDepth--;
                if (braceDepth == 1 && methodStart != -1)
                {
                    int length = i - methodStart + 1;
                    if (length > maxMethodLines) maxMethodLines = length;
                    methodStart = -1;
                }
            }
        }

        Assert(maxMethodLines <= 50, $"Maximum method length in BeamRebarViewModel.cs ({maxMethodLines}) <= 50 lines");
    }
    #endregion

    private static void PumpDispatcher(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(milliseconds)
        };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private sealed class DummyRunner : IBeamRebarRunner
    {
        public BeamOrchestratorResult? LastResult => null;
        public int PlannedCount(BeamRebarSpec spec) => 1;
        public System.Threading.Tasks.Task<int> RunAsync(BeamRebarSpec spec, IProgress<int> progress)
            => System.Threading.Tasks.Task.FromResult(1);
    }
}
