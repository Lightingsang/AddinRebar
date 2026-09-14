using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPRebar.Mcp.Server.Registry.Model;

/// <summary>Lifecycle of a tool in the library. Serialised as snake_case (`pending_approval`) in tool.json.</summary>
public enum ToolStatus
{
    Draft,
    Tested,
    PendingApproval,
    Published,
    Quarantined,
    Deprecated,
}

/// <summary>
///     One tool as stored on disk: the metadata of `tool.json`, the script of `code.cs` and the cases of
///     `examples.json`. Files are the source of truth; the database only indexes them. A tool is data —
///     running it means sending <see cref="Code"/> and the caller's args down the ordinary
///     `revit.execute` path, so nothing is ever loaded into Revit per tool.
/// </summary>
public sealed class ToolRecord
{
    public string Name { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Architecture · Structure · MEP · Annotation · View · Data · Generic.</summary>
    public string Category { get; set; } = "Generic";

    public List<string> Tags { get; set; } = [];

    public ToolStatus Status { get; set; } = ToolStatus.Draft;

    public int Version { get; set; } = 1;

    /// <summary>JSON Schema (object) of the tool's arguments as the AI sees them.</summary>
    public JsonElement InputSchema { get; set; } = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}""");

    /// <summary>auto · manual · none — the bridge transaction policy the script needs.</summary>
    public string Transaction { get; set; } = "auto";

    public int TimeoutSeconds { get; set; } = 60;

    public bool Destructive { get; set; } = true;

    /// <summary>Host the code targets (`revit`, `autocad`). Null in files written before the field existed — read as the exe's host.</summary>
    public string? Host { get; set; }

    /// <summary>Host versions the tool was written/tested against. Older files use <see cref="RevitVersions"/>.</summary>
    public List<string> HostVersions { get; set; } = [];

    public List<string> RevitVersions { get; set; } = [];

    /// <summary>`hprebar` for seeds, `ai` for proposals, `human:&lt;name&gt;` for hand-written tools.</summary>
    public string Author { get; set; } = "ai";

    /// <summary>The ad-hoc `execute_revit_code` run this tool was packaged from.</summary>
    public long? CreatedFromRunId { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Free text kept with the record: why it was quarantined, review notes.</summary>
    public string? Notes { get; set; }

    // ---- not part of tool.json ------------------------------------------------------------------

    [JsonIgnore]
    public string Code { get; set; } = string.Empty;

    [JsonIgnore]
    public List<ToolExample> Examples { get; set; } = [];

    /// <summary>SHA-256 over tool.json + code.cs + examples.json as read from disk.</summary>
    [JsonIgnore]
    public string Checksum { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Folder { get; set; }

    public bool IsPublished => Status == ToolStatus.Published;

    public bool IsRunnable => Status is not ToolStatus.Deprecated;
}

/// <summary>One usage example; <see cref="Args"/> must satisfy the tool's input schema.</summary>
public sealed class ToolExample
{
    public string Title { get; set; } = string.Empty;

    public JsonElement Args { get; set; } = JsonSerializer.Deserialize<JsonElement>("{}");

    public JsonElement? Expected { get; set; }

    public long? VerifiedRunId { get; set; }
}

/// <summary>One execution recorded in the run history: an ad-hoc script, a tool call or a test case.</summary>
public sealed class RunRecord
{
    public const string KindAdhoc = "adhoc";
    public const string KindTool = "tool";
    public const string KindTest = "test";

    public long Id { get; set; }

    public string? ToolName { get; set; }

    public int? Version { get; set; }

    public string Kind { get; set; } = KindAdhoc;

    public string? ArgsSha { get; set; }

    public string? CodeSha { get; set; }

    public bool DryRun { get; set; }

    public bool Success { get; set; }

    public long DurationMs { get; set; }

    public string? Error { get; set; }

    public string? RevitVersion { get; set; }

    public string? DocTitle { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Kept for successful ad-hoc runs so `propose_tool` can package them; null otherwise.</summary>
    public string? Code { get; set; }

    /// <summary>Args of the run, JSON text, when the caller sent any.</summary>
    public string? ArgsJson { get; set; }
}

/// <summary>Success statistics over the recent run window of one tool.</summary>
public sealed record RunStats(int Runs, int Successes, DateTimeOffset? LastRun, string? LastError)
{
    public static readonly RunStats Empty = new(0, 0, null, null);

    public int Failures => Runs - Successes;

    public double SuccessRate => Runs == 0 ? 0 : (double)Successes / Runs;
}
