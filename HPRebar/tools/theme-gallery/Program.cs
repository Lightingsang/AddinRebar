using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.ViewModel;
using Control = System.Windows.Controls.Control;
using UserControl = System.Windows.Controls.UserControl;

namespace HPRebar.Tools.ThemeGallery;

/// <summary>
///     Hosts every HPRebar tab view off-Revit (no Application object — the add-in case) and screenshots it on both
///     palettes. Exit code 1 when a view fails to parse or a theme call throws.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var dll = Path.GetFullPath(Arg(args, "-Dll") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "HPRebar", "bin", "Debug.R26", "HPRebar.dll"));
        var outDir = Path.GetFullPath(Arg(args, "-Out") ?? Path.Combine(Path.GetDirectoryName(dll)!, "..", "..", "..", "..", "output", "theme-gallery"));
        Directory.CreateDirectory(outDir);

        var bridgeDll = ResolveBridgeDll(args, dll);
        SetupAssemblyResolver(Arg(args, "-RevitDir") ?? @"C:\Program Files\Autodesk\Revit 2026", Path.GetDirectoryName(dll), bridgeDll is not null ? Path.GetDirectoryName(bridgeDll) : null);

        var assembly = Assembly.LoadFrom(dll);
        var bridge = assembly.GetType("HPRebar.Resources.Themes.MaterialThemeBridge", throwOnError: true)!;
        var apply = bridge.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static)!;

        var views = DiscoverViews(assembly);
        Console.WriteLine($"{views.Count} views in {Path.GetFileName(dll)} -> {outDir}");

        var failures = 0;
        var index = new List<string>();

        failures += ProcessTabViews(views, apply, outDir, index);
        if (bridgeDll is not null && File.Exists(bridgeDll))
            failures += ProcessMcpBridgeView(bridgeDll, bridge, outDir, index);
        failures += KataElevationGallery.Run(assembly, apply, outDir, index, Pump, Capture);
        failures += ProcessRibbonIcons(assembly, index);

        File.WriteAllLines(Path.Combine(outDir, "index.txt"), index);
        Console.WriteLine($"SUMMARY: {index.Count} runs recorded, {failures} failures");
        return failures == 0 ? 0 : 1;
    }

    private static string? ResolveBridgeDll(string[] args, string addInDll)
    {
        var explicitBridge = Arg(args, "-BridgeDll");
        if (explicitBridge is not null) return Path.GetFullPath(explicitBridge);

        var dir = Path.GetDirectoryName(addInDll)!;
        var config = Path.GetFileName(dir);
        var candidate = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "HPRebar.McpBridge", "bin", config, "HPRebar.McpBridge.dll"));
        if (File.Exists(candidate)) return candidate;

        var fallback = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "HPRebar.McpBridge", "bin", "Debug.R26", "HPRebar.McpBridge.dll"));
        return File.Exists(fallback) ? fallback : null;
    }

    private static void SetupAssemblyResolver(string revitDir, string? addInDir, string? bridgeDir)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            var name = new AssemblyName(e.Name).Name;
            var candidate = Path.Combine(revitDir, name + ".dll");
            if (name is "RevitAPI" or "RevitAPIUI" && File.Exists(candidate)) return Assembly.LoadFrom(candidate);
            if (bridgeDir is not null && File.Exists(Path.Combine(bridgeDir, name + ".dll")))
                return Assembly.LoadFrom(Path.Combine(bridgeDir, name + ".dll"));
            if (addInDir is not null && File.Exists(Path.Combine(addInDir, name + ".dll")))
                return Assembly.LoadFrom(Path.Combine(addInDir, name + ".dll"));
            return null;
        };
    }

    private static List<Type> DiscoverViews(Assembly assembly)
    {
        Type?[] allTypes;
        try { allTypes = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { allTypes = e.Types; Console.WriteLine($"  {e.LoaderExceptions.Length} types skipped (Revit API not loadable off-Revit)"); }
        return allTypes.OfType<Type>()
            .Where(t => typeof(UserControl).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace is { } ns && ns.Contains(".View"))
            .OrderBy(t => t.FullName)
            .ToList();
    }

    private static int ProcessTabViews(List<Type> views, MethodInfo apply, string outDir, List<string> index)
    {
        var failures = 0;
        foreach (var viewType in views)
        {
            try
            {
                var window = new Window
                {
                    Title = viewType.Name, Width = 1000, Height = 640,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = false
                };
                window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml") });
                window.SetResourceReference(Control.BackgroundProperty, "Brush.Background");
                window.SetResourceReference(Control.ForegroundProperty, "Brush.Foreground.Primary");
                window.SetResourceReference(Control.FontFamilyProperty, "Font.Family.Default");
                window.SetResourceReference(Control.FontSizeProperty, "Font.Size.Body");
                window.Content = (UserControl)Activator.CreateInstance(viewType)!;

                window.Show();
                foreach (var dark in new[] { true, false })
                {
                    apply.Invoke(null, [window, dark, null]);
                    Pump(700);
                    var file = Path.Combine(outDir, $"{viewType.Name}-{(dark ? "dark" : "light")}.png");
                    Capture(window, file);
                    var palette = window.TryFindResource("Color.Background");
                    var toolkit = window.TryFindResource("MaterialDesign.Brush.Background") as SolidColorBrush;
                    index.Add($"{viewType.FullName} {(dark ? "dark" : "light")} -> {Path.GetFileName(file)} Color.Background={palette} MaterialDesign.Brush.Background={toolkit?.Color}");
                }
                window.Close();
                Pump(100);
                Console.WriteLine($"PASS {viewType.FullName}");
            }
            catch (Exception exception)
            {
                failures++;
                var root = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
                Console.WriteLine($"FAIL {viewType.FullName}: {root.GetType().Name}: {root.Message}");
                index.Add($"{viewType.FullName} FAIL {root.GetType().Name}: {root.Message}");
            }
        }
        return failures;
    }

    private static int ProcessMcpBridgeView(string bridgeDll, Type defaultBridge, string outDir, List<string> index)
    {
        try
        {
            var bridgeAssembly = Assembly.LoadFrom(bridgeDll);
            var statusViewType = bridgeAssembly.GetType("HPRebar.McpBridge.View.McpBridgeStatusView", throwOnError: true)!;
            var bridgeType = bridgeAssembly.GetType("HPRebar.Resources.Themes.MaterialThemeBridge", throwOnError: false) ?? defaultBridge;
            var apply = bridgeType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static)!;

            var runner = new GalleryBridgeRunner();
            var vm = new McpBridgeStatusViewModel(runner, a => a());
            var statusWindow = (Window)Activator.CreateInstance(statusViewType, vm)!;

            statusWindow.Show();
            foreach (var dark in new[] { true, false })
            {
                apply.Invoke(null, [statusWindow, dark, null]);
                Pump(700);
                var file = Path.Combine(outDir, $"McpBridgeStatusView-{(dark ? "dark" : "light")}.png");
                Capture(statusWindow, file);
                index.Add($"HPRebar.McpBridge.View.McpBridgeStatusView {(dark ? "dark" : "light")} -> {Path.GetFileName(file)}");
            }
            statusWindow.Close();
            Pump(100);
            Console.WriteLine("PASS HPRebar.McpBridge.View.McpBridgeStatusView");
            return 0;
        }
        catch (Exception exception)
        {
            var root = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            Console.WriteLine($"FAIL McpBridgeStatusView: {root.GetType().Name}: {root.Message}");
            index.Add($"McpBridgeStatusView FAIL {root.GetType().Name}: {root.Message}");
            return 1;
        }
    }

    private sealed class GalleryBridgeRunner : IMcpBridgeRunner
    {
        public string PipeName => @"\\.\pipe\hprebar-mcp-r2026";
        public string RevitVersion => "2026";
        public string HostName => "Revit";
        public string AuditDirectory => @"C:\temp";
        public BridgeStatus Status => BridgeStatus.Listening;
        public string? StatusMessage => "Listening for AI requests";
        public bool HasClient => true;
        public bool ExecutionEnabled { get; set; } = true;
        public bool AutoStartListener { get; set; } = false;
        public LastRunInfo? LastRun => null;
        public int CompiledScriptCount => 12;
        public event Action? StateChanged { add { } remove { } }
        public void Start() {}
        public void Stop() {}
        public void Restart() {}
    }

    private static int ProcessRibbonIcons(Assembly assembly, List<string> index)
    {
        var failures = 0;
        foreach (var dark in new[] { true, false })
        {
            try
            {
                var iconsType = assembly.GetType("HPRebar.Resources.Icons.RibbonIcons", throwOnError: true)!;
                var icons = Activator.CreateInstance(iconsType, new object[] { dark })!;
                var window = new Window { Title = "icon " + (dark ? "dark" : "light"), Width = 320, Height = 120, ShowInTaskbar = false };
                foreach (var name in new[] { "ColumnRebar", "BeamRebar", "FoundationRebar" })
                {
                    window.Icon = (ImageSource)iconsType.GetProperty(name)!.GetValue(icons)!;
                    if (!window.IsVisible) window.Show();
                    Pump(150);
                }
                window.Close();
                Pump(50);
                Console.WriteLine($"PASS window icons from RibbonIcons({(dark ? "dark" : "light")})");
                index.Add($"window icons {(dark ? "dark" : "light")} PASS");
            }
            catch (Exception exception)
            {
                failures++;
                Console.WriteLine($"FAIL window icons ({(dark ? "dark" : "light")}): {exception.GetType().Name}: {exception.Message}");
                index.Add($"window icons {(dark ? "dark" : "light")} FAIL {exception.Message}");
            }
        }
        return failures;
    }

    private static string? Arg(string[] args, string name)
    {
        var i = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static void Pump(int ms)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(ms), DispatcherPriority.Normal, (_, _) => frame.Continue = false, Dispatcher.CurrentDispatcher);
        timer.Start();
        Dispatcher.PushFrame(frame);
        timer.Stop();
    }

    private static void Capture(Window window, string path)
    {
        window.UpdateLayout();
        var width = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        var height = window.ActualHeight > 0 ? window.ActualHeight : window.Height;
        if (double.IsNaN(width) || width <= 0) width = 800;
        if (double.IsNaN(height) || height <= 0) height = 600;

        var dpiX = 96.0;
        var dpiY = 96.0;
        var source = PresentationSource.FromVisual(window);
        if (source?.CompositionTarget != null)
        {
            dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
        }

        var pixelWidth = Math.Max(1, (int)Math.Round(width * (dpiX / 96.0)));
        var pixelHeight = Math.Max(1, (int)Math.Round(height * (dpiY / 96.0)));

        var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);
        rtb.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
