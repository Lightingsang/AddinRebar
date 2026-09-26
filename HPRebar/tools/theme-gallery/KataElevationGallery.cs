using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HPRebar.Tools.ThemeGallery;

/// <summary>
///     Renders the Kata Export elevation canvas off-Revit on sample runs (a floor beam with a free end, a crossing
///     girder, a joint, a stepped span and columns above; a tie-beam run on footings; a 37-span run) framed whole,
///     zoomed on a span and reversed, dark and light. Everything is reached by reflection through the merged
///     HPRebar.dll, because the Core types inside it are not the ones a project reference would load.
/// </summary>
internal static class KataElevationGallery
{
    private const string Models = "HPRebar.Core.KataExport.Models.";
    private const string Calculators = "HPRebar.Core.KataExport.Calculators.";

    public const int SceneCount = 5;

    public static int Run(Assembly hprebar, MethodInfo apply, string outDir, List<string> index, Action<int> pump, Action<Window, string> capture)
    {
        var k = new Kata(hprebar);
        var scenes = new (string Name, object Input, bool Reverse, int? Focus, int Selected)[]
        {
            ("kata-floor", k.FloorRun(), false, null, 3),
            ("kata-floor-zoom", k.FloorRun(), false, 5, 5),
            ("kata-floor-reverse", k.FloorRun(), true, null, -1),
            ("kata-tie-beams", k.TieBeamRun(), false, null, -1),
            ("kata-long", k.LongRun(), false, null, 40)
        };

        int failures = 0;
        foreach (var scene in scenes)
        {
            try
            {
                var elevation = k.Elevation(scene.Input, scene.Reverse);
                var canvas = (FrameworkElement)Activator.CreateInstance(hprebar.GetType("HPRebar.KataExport.View.Controls.KataElevationCanvas", true)!)!;
                var canvasType = canvas.GetType();
                canvas.SetResourceReference((DependencyProperty)canvasType.GetField("SurfaceBrushProperty")!.GetValue(null)!, "Brush.Canvas.Fill");
                canvasType.GetProperty("Elevation")!.SetValue(canvas, elevation);
                canvasType.GetProperty("SelectedColumnIndex")!.SetValue(canvas, scene.Selected);

                var window = new Window
                {
                    Title = scene.Name, Width = 1040, Height = 380, WindowStartupLocation = WindowStartupLocation.CenterScreen, ShowInTaskbar = false,
                    Content = new Border { Padding = new Thickness(8), Child = canvas }
                };
                window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml") });
                window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "Brush.Background");
                window.Show();

                foreach (var dark in new[] { true, false })
                {
                    apply.Invoke(null, [window, dark, null]);
                    if (scene.Focus is { } focus)
                    {
                        var focusType = hprebar.GetType("HPRebar.KataExport.ViewModel.KataViewFocus", true)!;
                        canvasType.GetProperty("FocusRequest")!.SetValue(canvas, Activator.CreateInstance(focusType, (int?)focus, dark ? 1 : 2));
                    }

                    pump(500);
                    var file = Path.Combine(outDir, $"{scene.Name}-{(dark ? "dark" : "light")}.png");
                    capture(window, file);
                    index.Add($"{scene.Name} {(dark ? "dark" : "light")} -> {Path.GetFileName(file)} surface={(canvasType.GetProperty("SurfaceBrush")!.GetValue(canvas) as SolidColorBrush)?.Color}");
                }

                window.Close();
                pump(100);
                Console.WriteLine($"PASS {scene.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                var root = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
                Console.WriteLine($"FAIL {scene.Name}: {root.GetType().Name}: {root.Message}");
                index.Add($"{scene.Name} FAIL {root.GetType().Name}: {root.Message}");
            }
        }

        return failures;
    }

    /// <summary>Builds Core records of the merged assembly by reflection.</summary>
    private sealed class Kata(Assembly assembly)
    {
        private readonly Type _interval = assembly.GetType(Models + "Interval1D", true)!;
        private readonly Type _piece = assembly.GetType(Models + "KataBeamPiece", true)!;
        private readonly Type _support = assembly.GetType(Models + "KataSupport", true)!;
        private readonly Type _kind = assembly.GetType(Models + "KataSupportKind", true)!;
        private readonly Type _grid = assembly.GetType(Models + "KataGridCrossing", true)!;
        private readonly Type _header = assembly.GetType(Models + "KataHeader", true)!;
        private readonly Type _input = assembly.GetType(Models + "KataRunInput", true)!;
        private readonly Type _options = assembly.GetType(Models + "KataBuildOptions", true)!;

        public object Elevation(object input, bool reverse)
        {
            var options = Activator.CreateInstance(_options)!;
            _options.GetProperty("Reverse")!.SetValue(options, reverse);
            var sheet = assembly.GetType(Calculators + "KataRowBuilder", true)!.GetMethod("Build")!.Invoke(null, [input, options])!;
            return assembly.GetType(Calculators + "KataElevationBuilder", true)!.GetMethod("Build")!.Invoke(null, [input, options, sheet])!;
        }

        /// <summary>Floor beam: free start, crossing girder, joint, stepped span, columns above, a grid off the supports.</summary>
        public object FloorRun() => Input(
            [Piece(-1200, 4000, 220, 500, 0), Piece(4000, 9000, 220, 500, 0), Piece(9000, 13500, 250, 600, -50), Piece(13500, 18000, 220, 500, 0)],
            [
                Support(0, -200, 200, upper: (-150, 150)),
                Support(0, 8800, 9200, upper: (8850, 9250)),
                Support(2, 13350, 13650, section: "300x600"),
                Support(0, 17800, 18200, upper: (17800, 18200)),
                Support(2, 17900, 18150, section: "220x400")
            ],
            [Grid("1", 0), Grid("2", 9000), Grid("2a", 11200), Grid("3", 13500), Grid("4", 17950)],
            Header("D1", 500, 220));

        /// <summary>Tie beams on nine 1700–2650 mm footings, a column centred on each, the last one offset.</summary>
        public object TieBeamRun()
        {
            double[] centres = [0, 4300, 8600, 12900, 17200, 21500, 25800, 30100, 34400];
            var pieces = centres.Skip(1).Select((c, i) => Piece(centres[i], c, 300, 500, 0)).ToArray();
            var supports = centres.Select((c, i) =>
            {
                double half = i % 3 == 1 ? 1325 : 850;
                double shift = i == centres.Length - 1 ? 790 : 0;
                return Support(1, c - half, c + half, upper: (c - 125 + shift, c + 125 + shift));
            }).ToArray();
            var grids = centres.Select((c, i) => Grid($"{14 + i}A", c + (i == 2 ? -50 : 0))).ToArray();
            return Input(pieces, supports, grids, Header("GMX3", 500, 300));
        }

        /// <summary>37 spans of 5 m on 300 mm columns: 75 Kata columns.</summary>
        public object LongRun()
        {
            var pieces = Enumerable.Range(0, 37).Select(i => Piece(i * 5000, (i + 1) * 5000, 220, 450, 0)).ToArray();
            var supports = Enumerable.Range(0, 38).Select(i => Support(0, i * 5000 - 150, i * 5000 + 150)).ToArray();
            var grids = Enumerable.Range(0, 38).Select(i => Grid($"{i + 1}", i * 5000)).ToArray();
            return Input(pieces, supports, grids, Header("DL", 450, 220));
        }

        private object Interval(double start, double end) => Activator.CreateInstance(_interval, start, end)!;

        private object Piece(double start, double end, double b, double h, double z) =>
            Activator.CreateInstance(_piece, Interval(start, end), b, h, z, $"P{start}")!;

        private object Support(int kind, double start, double end, (double, double)? upper = null, string? section = null) =>
            Activator.CreateInstance(_support, Enum.ToObject(_kind, kind), Interval(start, end), $"S{start}", section,
                upper is { } u ? Interval(u.Item1, u.Item2) : null, null)!;

        private object Grid(string name, double station) => Activator.CreateInstance(_grid, name, station)!;

        private object Header(string name, double h, double b) =>
            Activator.CreateInstance(_header, name, 1, h, b, 3300.0, 120.0, "B", -b / 2)!;

        private object Input(object[] pieces, object[] supports, object[] grids, object header) =>
            Activator.CreateInstance(_input, Typed(_piece, pieces), Typed(_support, supports), Typed(_grid, grids), header)!;

        private static Array Typed(Type type, object[] items)
        {
            var array = Array.CreateInstance(type, items.Length);
            for (int i = 0; i < items.Length; i++) array.SetValue(items[i], i);
            return array;
        }
    }
}
