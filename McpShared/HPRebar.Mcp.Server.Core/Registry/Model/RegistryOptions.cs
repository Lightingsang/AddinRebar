namespace HPRebar.Mcp.Server.Registry.Model;

/// <summary>
///     Where the tool memory lives and how strict it is. Paths default to the user's roaming profile so a
///     library survives reinstalling the server; point <see cref="LibraryPath"/> at a git checkout or a
///     shared folder to review and share tools between machines.
/// </summary>
public sealed class RegistryOptions
{
    public const string SectionName = "Registry";

    public const string PolicyManual = "manual";
    public const string PolicyAuto = "auto";

    private string? _libraryPath;
    private string? _dbPath;

    /// <summary>`%AppData%\{ProductFolder}\McpServer\`; the exe's profile sets the product, so two MCPs never share a root.</summary>
    public string ProductFolder { get; set; } = "HPRebar";

    public static string DefaultRootFor(string productFolder) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), productFolder, "McpServer");

    /// <summary>Folder of tool records: &lt;Category&gt;/&lt;name&gt;/{tool.json, code.cs, examples.json}. Source of truth.</summary>
    public string LibraryPath
    {
        get => _libraryPath ?? Path.Combine(DefaultRootFor(ProductFolder), "tools-library");
        set => _libraryPath = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>SQLite index + run history. Derived from the library; safe to delete.</summary>
    public string DbPath
    {
        get => _dbPath ?? Path.Combine(DefaultRootFor(ProductFolder), "registry.db");
        set => _dbPath = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>`manual`: a human approves before a tool is published. `auto`: tested tools publish themselves.</summary>
    public string PublishPolicy { get; set; } = PolicyManual;

    public int SearchTopK { get; set; } = 5;

    /// <summary>How many recent runs feed the stability score.</summary>
    public int RunWindow { get; set; } = 50;

    public int QuarantineMinRuns { get; set; } = 5;

    public double QuarantineMaxFailureRate { get; set; } = 0.4;

    /// <summary>Copy the embedded seed tools into an empty library on first start.</summary>
    public bool InstallSeeds { get; set; } = true;

    /// <summary>Watch the library folder and re-register tools when files change (approval by editing tool.json).</summary>
    public bool WatchLibrary { get; set; } = true;

    /// <summary>Ad-hoc `execute_revit_code` runs whose code is kept for `propose_tool`; older ones are pruned.</summary>
    public int KeepAdhocRuns { get; set; } = 500;

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(LibraryPath)
        && !string.IsNullOrWhiteSpace(DbPath)
        && PublishPolicy is PolicyManual or PolicyAuto
        && SearchTopK is >= 1 and <= 50
        && RunWindow is >= 5 and <= 1000
        && QuarantineMinRuns >= 1
        && QuarantineMaxFailureRate is > 0 and <= 1
        && KeepAdhocRuns >= 0;
}
