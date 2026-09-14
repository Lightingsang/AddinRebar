using System.Text.Json;
using System.Text.Json.Nodes;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>
///     An MCP tool backed by a library record: schema from tool.json, execution through
///     <see cref="ToolManager.RunAsync"/>. `dryRun` is added to every tool that writes, matching the
///     host's execute tool. Built as an <see cref="AIFunction"/> so the SDK can host a tool whose
///     schema is data rather than a C# signature.
/// </summary>
public sealed class RegistryToolFunction : AIFunction
{
    private const string DryRunKey = "dryRun";

    private readonly ToolRecord _record;
    private readonly ToolManager _manager;
    private readonly ResultFormatter _formatter;
    private readonly JsonElement _schema;
    private readonly string _description;

    public RegistryToolFunction(ToolRecord record, ToolManager manager, ResultFormatter formatter)
    {
        _record = record;
        _manager = manager;
        _formatter = formatter;
        _schema = BuildSchema(record);
        _description = $"{record.Description} [Registry tool v{record.Version}, {record.Category}, transaction={record.Transaction}. Registry tools run stored, reviewed C# inside {manager.Profile.DisplayName} — prefer them over {manager.Profile.ExecuteToolName} for the same task.]";
    }

    public ToolRecord Record => _record;

    public override string Name => _record.Name;

    public override string Description => _description;

    public override JsonElement JsonSchema => _schema;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var dryRun = false;
        var payload = new JsonObject();
        foreach (var (key, value) in arguments)
        {
            var element = ToElement(value);
            if (string.Equals(key, DryRunKey, StringComparison.OrdinalIgnoreCase))
            {
                dryRun = element.ValueKind == JsonValueKind.True || (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var b) && b);
                continue;
            }

            payload[key] = element.ValueKind == JsonValueKind.Undefined ? null : JsonNode.Parse(element.GetRawText());
        }

        var args = JsonSerializer.SerializeToElement(payload);
        return await _formatter.RunAsync(async () =>
        {
            try
            {
                var result = await _manager.RunAsync(_record.Name, args, dryRun, allowUnpublished: false, RunRecord.KindTool, cancellationToken).ConfigureAwait(false);
                return _formatter.FromExecute(result);
            }
            catch (ToolNotRunnableException exception)
            {
                return _formatter.Error(exception.Message);
            }
            catch (ToolNotFoundException exception)
            {
                return _formatter.Error(exception.Message);
            }
        }).ConfigureAwait(false);
    }

    private static JsonElement ToElement(object? value) => value switch
    {
        null => default,
        JsonElement element => element,
        JsonNode node => JsonSerializer.SerializeToElement(node),
        _ => JsonSerializer.SerializeToElement(value),
    };

    /// <summary>tool.json schema, guaranteed to be an object schema, plus `dryRun` for writing tools.</summary>
    public static JsonElement BuildSchema(ToolRecord record)
    {
        var node = record.InputSchema.ValueKind == JsonValueKind.Object ? JsonNode.Parse(record.InputSchema.GetRawText())!.AsObject() : new JsonObject();
        node["type"] = "object";
        var properties = node["properties"] as JsonObject ?? new JsonObject();
        node["properties"] = properties;

        if (!string.Equals(record.Transaction, "none", StringComparison.OrdinalIgnoreCase) && !properties.ContainsKey(DryRunKey))
        {
            properties[DryRunKey] = new JsonObject
            {
                ["type"] = "boolean",
                ["default"] = false,
                ["description"] = "Run the tool, then roll everything back. Use first on a model you care about.",
            };
        }

        return JsonSerializer.SerializeToElement(node);
    }
}

/// <summary>
///     Keeps the server's live tool list equal to the set of published library records. Adding or
///     removing from <see cref="McpServerOptions.ToolCollection"/> makes the SDK send
///     <c>notifications/tools/list_changed</c>, so an AI client sees a newly approved tool without a restart.
/// </summary>
public sealed class DynamicToolRegistrar
{
    private readonly ToolManager _manager;
    private readonly ResultFormatter _formatter;
    private readonly IOptions<McpServerOptions> _serverOptions;
    private readonly ILogger<DynamicToolRegistrar> _logger;
    private readonly Dictionary<string, (McpServerTool Tool, string Checksum)> _registered = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public DynamicToolRegistrar(ToolManager manager, ResultFormatter formatter, IOptions<McpServerOptions> serverOptions, ILogger<DynamicToolRegistrar> logger)
    {
        _manager = manager;
        _formatter = formatter;
        _serverOptions = serverOptions;
        _logger = logger;
    }

    public McpServerPrimitiveCollection<McpServerTool> Collection => _serverOptions.Value.ToolCollection ??= [];

    public IReadOnlyCollection<string> RegisteredNames
    {
        get { lock (_gate) return _registered.Keys.ToArray(); }
    }

    /// <summary>Diff published records against what is registered; one list_changed for the whole batch.</summary>
    public int Sync()
    {
        lock (_gate)
        {
            var collection = Collection;
            var desired = _manager.Tools.Where(t => t.IsPublished).ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
            var added = 0; var removed = 0;

            using (collection.DeferChangedEvents())
            {
                foreach (var name in _registered.Keys.ToArray())
                {
                    if (desired.TryGetValue(name, out var current) && current.Checksum == _registered[name].Checksum) continue;
                    collection.Remove(_registered[name].Tool);
                    _registered.Remove(name);
                    removed++;
                }

                foreach (var record in desired.Values)
                {
                    if (_registered.ContainsKey(record.Name)) continue;
                    var tool = McpServerTool.Create(new RegistryToolFunction(record, _manager, _formatter), new McpServerToolCreateOptions
                    {
                        Name = record.Name,
                        Title = record.Title ?? record.Name,
                        Description = null,
                        Destructive = record.Destructive,
                        ReadOnly = string.Equals(record.Transaction, "none", StringComparison.OrdinalIgnoreCase),
                        Idempotent = false,
                        OpenWorld = false,
                    });

                    if (!collection.TryAdd(tool))
                    {
                        _logger.LogWarning("Registry tool {Name} clashes with a built-in tool name; not registered", record.Name);
                        continue;
                    }

                    _registered[record.Name] = (tool, record.Checksum);
                    added++;
                }
            }

            if (added + removed > 0) _logger.LogInformation("Registry tools synced: +{Added} −{Removed}, {Total} registered", added, removed, _registered.Count);
            return _registered.Count;
        }
    }
}
