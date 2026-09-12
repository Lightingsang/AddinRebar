using System.IO;
using HPRebar.BeamRebar;
using HPRebar.ColumnRebar;
using HPRebar.Commands;
using HPRebar.FoundationRebar;
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
            Log.CloseAndFlush();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "HPRebar");

            panel.AddPushButton<StartupCommand>("Execute")
                .SetImage("/HPRebar;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/HPRebar;component/Resources/Icons/RibbonIcon32.png");

            var rebarPanel = Application.CreatePanel("Rebar", "HPRebar");

            rebarPanel.AddPushButton<ColumnRebarCommand>("Column Rebar")
                .SetImage("/HPRebar;component/Resources/Icons/ColumnRebar16.png")
                .SetLargeImage("/HPRebar;component/Resources/Icons/ColumnRebar32.png");

            rebarPanel.AddPushButton<BeamRebarCommand>("Beam Rebar")
                .SetImage("/HPRebar;component/Resources/Icons/BeamRebar16.png")
                .SetLargeImage("/HPRebar;component/Resources/Icons/BeamRebar32.png");

            rebarPanel.AddPushButton<FoundationRebarCommand>("Foundation Rebar")
                .SetImage("/HPRebar;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/HPRebar;component/Resources/Icons/RibbonIcon32.png");
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