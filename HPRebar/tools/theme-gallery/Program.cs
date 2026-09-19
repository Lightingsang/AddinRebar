using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

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

        // Command/handler types reference the Revit API; the installed Revit supplies it for type loading only
        // (nothing here calls into it). Types that still fail to load are skipped, they are never views.
        var revitDir = Arg(args, "-RevitDir") ?? @"C:\Program Files\Autodesk\Revit 2026";
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            var name = new AssemblyName(e.Name).Name;
            var candidate = Path.Combine(revitDir, name + ".dll");
            return name is "RevitAPI" or "RevitAPIUI" && File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        };
        var assembly = Assembly.LoadFrom(dll);
        var bridge = assembly.GetType("HPRebar.Resources.Themes.MaterialThemeBridge", throwOnError: true)!;
        var apply = bridge.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static)!;
        Type?[] allTypes;
        try { allTypes = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { allTypes = e.Types; Console.WriteLine($"  {e.LoaderExceptions.Length} types skipped (Revit API not loadable off-Revit)"); }
        var views = allTypes.OfType<Type>()
            .Where(t => typeof(System.Windows.Controls.UserControl).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace is { } ns && ns.Contains(".View"))
            .OrderBy(t => t.FullName)
            .ToList();
        Console.WriteLine($"{views.Count} views in {Path.GetFileName(dll)} -> {outDir}");

        var failures = 0;
        var index = new List<string>();
        foreach (var viewType in views)
        {
            try
            {
                var window = new Window
                {
                    Title = viewType.Name, Width = 1000, Height = 640, WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = false
                };
                window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml") });
                window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "Brush.Background");
                window.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Brush.Foreground.Primary");
                window.SetResourceReference(System.Windows.Controls.Control.FontFamilyProperty, "Font.Family.Default");
                window.SetResourceReference(System.Windows.Controls.Control.FontSizeProperty, "Font.Size.Body");
                window.Content = (System.Windows.Controls.UserControl)Activator.CreateInstance(viewType)!;

                window.Show();
                foreach (var dark in new[] { true, false })
                {
                    apply.Invoke(null, [window, dark, null]);
                    Pump(700);
                    var file = Path.Combine(outDir, $"{viewType.Name}-{(dark ? "dark" : "light")}.png");
                    Capture(window, file);
                    var palette = window.TryFindResource("Color.Background");
                    var toolkit = window.TryFindResource("MaterialDesign.Brush.Background") as SolidColorBrush;
                    index.Add($"{viewType.FullName} {(dark ? "dark" : "light")} -> {Path.GetFileName(file)} Color.Background={palette} MaterialDesign.Brush.Background={toolkit?.Color} window.Background={(window.Background as SolidColorBrush)?.Color} content.Background={((window.Content as System.Windows.Controls.Control)?.Background as SolidColorBrush)?.Color}");
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

        // Window icons are the theme-aware vector glyphs (no PNG): WPF must be able to turn the DrawingImage into an HICON.
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

        File.WriteAllLines(Path.Combine(outDir, "index.txt"), index);
        Console.WriteLine($"SUMMARY {views.Count + 2 - failures} pass, {failures} fail");
        return failures == 0 ? 0 : 1;
    }

    private static string? Arg(string[] args, string name)
    {
        var i = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>Pumps the dispatcher for a while so layout, bindings and the 300 ms theme animation settle.</summary>
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
        var scale = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var x = (int)(window.Left * scale);
        var y = (int)(window.Top * scale);
        var w = (int)(window.ActualWidth * scale);
        var h = (int)(window.ActualHeight * scale);
        using var bitmap = new Bitmap(w, h);
        using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
        bitmap.Save(path, ImageFormat.Png);
    }
}
