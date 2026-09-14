using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.DependencyInjection;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>
///     The human's door into the registry: `&lt;server&gt;.exe registry &lt;command&gt;` (the exe name comes from the
///     host profile: `HPRebar.Mcp.Server.exe`, `HPAutoCad.Mcp.Server.exe`). Runs in the
///     same process image as the MCP server but never starts the transport; it edits the library files
///     and exits, and every running server picks the change up through its folder watcher.
/// </summary>
public static class RegistryCli
{
    public static string Usage(string exe) => exe + """
         registry <command> [options]

          list [--status <draft|tested|pending_approval|published|quarantined|deprecated>]
          pending                          tools waiting for approval, with their review files
          show <name>                      record, stats and code
          approve <name> [--by <who>] [--force]
          reject <name> [--by <who>] --reason "<why>"
          deprecate|quarantine|restore <name> [--by <who>] [--reason "<why>"]
          stats                            counts by status, paths
          export <name> <dir>              copy a tool folder out of the library
          import <dir>                     copy tool folders (each with tool.json) into the library
        """;

    public static async Task<int> RunAsync(IServiceProvider services, string[] args, TextWriter? output = null)
    {
        output ??= Console.Out;
        var manager = services.GetRequiredService<ToolManager>();
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h") { output.WriteLine(Usage(manager.Profile.CliExecutable)); return 0; }

        var store = services.GetRequiredService<ToolLibraryStore>();
        var db = services.GetRequiredService<ToolRegistryDb>();
        var lifecycle = services.GetRequiredService<ToolLifecycleService>();

        store.EnsureRoot();
        db.Initialize();
        await manager.LoadAllAsync().ConfigureAwait(false);

        var command = args[0].ToLowerInvariant();
        var rest = args[1..];
        string Option(string name, string? fallback = null)
        {
            var i = Array.FindIndex(rest, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
            return i >= 0 && i + 1 < rest.Length ? rest[i + 1] : fallback ?? string.Empty;
        }
        bool Flag(string name) => rest.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        string Positional(int index) => rest.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).Skip(index).FirstOrDefault()
                                        ?? throw new ArgumentException($"missing argument #{index + 1}");
        var by = Option("--by", Environment.UserName);

        try
        {
            switch (command)
            {
                case "list":
                {
                    var status = Option("--status");
                    var stats = db.AllStats(manager.Options.RunWindow);
                    var rows = manager.Tools
                        .Where(t => string.IsNullOrEmpty(status) || ToolRegistryDb.StatusText(t.Status) == status)
                        .OrderBy(t => t.Category).ThenBy(t => t.Name);
                    output.WriteLine($"{"name",-36} {"category",-12} {"status",-17} {"v",2} {"runs",5} {"stab",5}");
                    foreach (var t in rows)
                    {
                        var s = stats.TryGetValue(t.Name, out var st) ? st : RunStats.Empty;
                        output.WriteLine($"{t.Name,-36} {t.Category,-12} {ToolRegistryDb.StatusText(t.Status),-17} {t.Version,2} {s.Runs,5} {StabilityScorer.Score(s),5:0.00}");
                    }
                    return 0;
                }
                case "pending":
                {
                    var pending = manager.Tools.Where(t => t.Status == ToolStatus.PendingApproval).OrderBy(t => t.Name).ToList();
                    if (pending.Count == 0) { output.WriteLine("No tools waiting for approval."); return 0; }
                    foreach (var t in pending)
                        output.WriteLine($"{t.Name} v{t.Version} — review: {Path.Combine(store.Root, ToolLifecycleService.ReviewFolder, t.Name + ".md")}");
                    return 0;
                }
                case "show":
                {
                    var details = manager.Get(Positional(0)) ?? throw new ToolNotFoundException(Positional(0));
                    var r = details.Record;
                    output.WriteLine($"{r.Name} v{r.Version} [{ToolRegistryDb.StatusText(r.Status)}] {r.Category} — {r.Title}");
                    output.WriteLine(r.Description);
                    output.WriteLine($"transaction={r.Transaction} timeout={r.TimeoutSeconds}s author={r.Author} approvedBy={r.ApprovedBy} fromRun={r.CreatedFromRunId}");
                    output.WriteLine($"runs={details.Stats.Runs} success={details.Stats.SuccessRate:P0} stability={details.Stability} lastError={details.Stats.LastError}");
                    if (r.Notes is not null) output.WriteLine("notes: " + r.Notes);
                    output.WriteLine($"folder: {r.Folder}");
                    output.WriteLine("--- code ---");
                    output.WriteLine(r.Code);
                    return 0;
                }
                case "approve":
                {
                    var record = lifecycle.Approve(Positional(0), by, Flag("--force"));
                    output.WriteLine($"{record.Name} v{record.Version} published by {by}. Running servers will list it within a second.");
                    return 0;
                }
                case "reject":
                {
                    var reason = Option("--reason");
                    if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("--reason is required.");
                    var record = lifecycle.Reject(Positional(0), by, reason);
                    output.WriteLine($"{record.Name} back to draft. Reason recorded.");
                    return 0;
                }
                case "deprecate":
                case "quarantine":
                case "restore":
                {
                    var record = lifecycle.Manage(Positional(0), command, Option("--reason", "(cli)"), by);
                    output.WriteLine($"{record.Name} → {ToolRegistryDb.StatusText(record.Status)}.");
                    return 0;
                }
                case "stats":
                {
                    output.WriteLine($"host: {manager.Profile.HostId} ({manager.Profile.DisplayName})");
                    output.WriteLine($"library: {store.Root}");
                    output.WriteLine($"database: {db.Path} (FTS5={db.HasFullTextSearch})");
                    output.WriteLine($"policy: {manager.Options.PublishPolicy}");
                    foreach (var group in manager.Tools.GroupBy(t => t.Status).OrderBy(g => g.Key))
                        output.WriteLine($"  {ToolRegistryDb.StatusText(group.Key),-17} {group.Count()}");
                    return 0;
                }
                case "export":
                {
                    if (!manager.TryGet(Positional(0), out var record)) throw new ToolNotFoundException(Positional(0));
                    var target = Path.Combine(Path.GetFullPath(Positional(1)), record.Name);
                    Directory.CreateDirectory(target);
                    foreach (var file in Directory.GetFiles(record.Folder!)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
                    output.WriteLine($"exported {record.Name} → {target}");
                    return 0;
                }
                case "import":
                {
                    var source = Path.GetFullPath(Positional(0));
                    var folders = File.Exists(Path.Combine(source, ToolLibraryStore.ToolFile)) ? [source] : Directory.GetDirectories(source).Where(d => File.Exists(Path.Combine(d, ToolLibraryStore.ToolFile))).ToArray();
                    var imported = 0;
                    foreach (var folder in folders)
                    {
                        var record = store.TryRead(folder);
                        if (record is null) { output.WriteLine($"skipped {folder}: unreadable"); continue; }
                        // Same fence as LoadAllAsync: a folder copied from the other host's library never becomes a live tool here.
                        if (record.Host is not null && !string.Equals(record.Host, manager.Profile.HostId, StringComparison.OrdinalIgnoreCase)) { output.WriteLine($"skipped {folder}: host '{record.Host}' is not '{manager.Profile.HostId}'"); continue; }
                        record.Host ??= manager.Profile.HostId;
                        if (record.Status == ToolStatus.Published && manager.Options.PublishPolicy == RegistryOptions.PolicyManual) record.Status = ToolStatus.Tested; // re-approve locally
                        record.Folder = null;
                        manager.Save(record, "imported", by, folder);
                        imported++;
                        output.WriteLine($"imported {record.Name} v{record.Version} as {ToolRegistryDb.StatusText(record.Status)}");
                    }
                    output.WriteLine($"{imported} tool(s) imported.");
                    return 0;
                }
                default:
                    output.WriteLine(Usage(manager.Profile.CliExecutable));
                    return 2;
            }
        }
        catch (Exception exception) when (exception is ToolNotFoundException or ToolNotRunnableException or ArgumentException)
        {
            output.WriteLine("error: " + exception.Message);
            return 1;
        }
    }
}
