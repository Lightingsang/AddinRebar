using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace HPBimToolkit.Infrastructure;

/// <summary>
///     Builds the toolkit's own logger. It is deliberately NOT assigned to <c>Log.Logger</c>: the HP add-ins resolve one
///     Serilog assembly inside Revit, so a global logger set here would be replaced by whichever add-in loads next.
/// </summary>
public static class ToolkitLogger
{
    public static Logger Create()
    {
        const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
            .WriteTo.File(Path.Combine(ToolkitPaths.LogFolder, "toolkit-.log"),
                restrictedToMinimumLevel: LogEventLevel.Debug,
                outputTemplate: outputTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true) // a second Revit writes to the same day file
            .CreateLogger();
    }
}
