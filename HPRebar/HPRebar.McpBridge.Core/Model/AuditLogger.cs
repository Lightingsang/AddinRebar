using System.IO;
using HPRebar.Mcp.Contracts.JsonRpc;
using Serilog;
using Serilog.Core;

namespace HPRebar.McpBridge.Core.Model;

/// <summary>One line per script run, whatever the outcome. The full source is kept: this is the forensic trail.</summary>
public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    string User,
    string? DocTitle,
    string? DocPathHash,
    string ScriptSha256,
    string Source,
    string Transaction,
    bool DryRun,
    string Outcome,
    long DurationMs,
    int Added,
    int Modified,
    int Deleted,
    string? Message);

/// <summary>
///     Append-only JSON-lines audit of everything the AI ran in Revit, in a file the user owns. Separate
///     from the diagnostic log so it is never rolled away by log retention: nothing is deleted here.
/// </summary>
public sealed class AuditLogger : IDisposable
{
    private readonly Logger _logger;

    public AuditLogger(string directory)
    {
        Directory.CreateDirectory(directory);
        FilePath = Path.Combine(directory, "audit-.log");

        _logger = new LoggerConfiguration()
            .WriteTo.File(FilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null,
                outputTemplate: "{Message:l}{NewLine}",
                shared: true)
            .CreateLogger();
    }

    public string FilePath { get; }

    public void Write(AuditEntry entry) => _logger.Information("{Line:l}", BridgeJson.Serialize(entry));

    public void Dispose() => _logger.Dispose();
}
