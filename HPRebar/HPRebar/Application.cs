using System.IO;
using System.Windows.Media;
using Autodesk.Revit.UI;
using HPRebar.BeamRebar;
using HPRebar.ColumnRebar;
using HPRebar.FoundationRebar;
using HPRebar.KataExport;
using HPRebar.KataRebar;
using HPRebar.Resources.Icons;
using Nice3point.Revit.Toolkit.External;
using Serilog;
using Serilog.Events;

namespace HPRebar
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        // Every button with the glyph it shows, so a theme change can repaint all of them at once.
        private readonly List<(PushButton Button, Func<RibbonIcons, ImageSource> Icon)> _buttons = new();
#if REVIT2024_OR_GREATER
        private EventHandler<Autodesk.Revit.UI.Events.ThemeChangedEventArgs>? _onThemeChanged;
#endif

        public override void OnStartup()
        {
            CreateLogger();

            try
            {
                CreateRibbon();
            }
            catch (Exception exception)
            {
                // An exception escaping here makes Revit drop the add-in with nothing but a journal entry
                // to go on. Log it to the file sink first so there is something to read, then let Revit
                // report the failure as it normally would.
                Log.Fatal(exception, "HPRebar could not build its ribbon");
                Log.CloseAndFlush();

                throw;
            }
        }

        public override void OnShutdown()
        {
            // Multi-version: ThemeChanged exists since Revit 2024
#if REVIT2024_OR_GREATER
            if (_onThemeChanged is not null) Application.ThemeChanged -= _onThemeChanged;
#endif
            Log.CloseAndFlush();
        }

        private void CreateRibbon()
        {
#if KATA_ONLY
            var kataPanel = Application.CreatePanel("Kata", "HPRebar");
            Track(kataPanel.AddPushButton<KataExportCommand>("Kata Export"), icons => icons.KataExport);
#else
            var rebarPanel = Application.CreatePanel("Rebar", "HPRebar");
            Track(rebarPanel.AddPushButton<ColumnRebarCommand>("Column Rebar"), icons => icons.ColumnRebar);
            Track(rebarPanel.AddPushButton<BeamRebarCommand>("Beam Rebar"), icons => icons.BeamRebar);
            Track(rebarPanel.AddPushButton<FoundationRebarCommand>("Foundation Rebar"), icons => icons.FoundationRebar);
            Track(rebarPanel.AddPushButton<KataExportCommand>("Kata Export"), icons => icons.KataExport);
            Track(rebarPanel.AddPushButton<KataRebarCommand>("Kata Rebar"), icons => icons.KataRebar);
#endif

            // Vector glyphs drawn in code (RibbonIcons): crisp at any DPI, ink follows Revit's UI theme.
            ApplyIcons();

            // Multi-version: ThemeChanged exists since Revit 2024
#if REVIT2024_OR_GREATER
            _onThemeChanged = (_, _) => { ApplyIcons(); Resources.Themes.RevitHostTheme.Instance.NotifyChanged(); };
            Application.ThemeChanged += _onThemeChanged;
#endif
        }

        private void Track(PushButton button, Func<RibbonIcons, ImageSource> icon) => _buttons.Add((button, icon));

        /// <summary>One 32×32 DrawingImage serves both slots: the ribbon scales it to 16 px for the small image.</summary>
        private void ApplyIcons()
        {
            var icons = new RibbonIcons(RibbonIcons.RevitIsDark());
            foreach (var (button, icon) in _buttons)
            {
                var image = icon(icons);
                button.Image = image;
                button.LargeImage = image;
            }
        }

        private static void CreateLogger()
        {
            const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            // File sink is the only way to see Revit-side failures without a debugger attached.
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HPRebar", "logs", "hprebar-.log");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
                .WriteTo.File(logPath,
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: outputTemplate,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .MinimumLevel.Debug()
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                Log.Fatal(exception, "Domain unhandled exception");
            };
        }
    }
}