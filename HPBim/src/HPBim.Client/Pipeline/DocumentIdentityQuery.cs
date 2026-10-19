namespace HPBim.Client.Pipeline;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
/// Service for querying active Revit document identity and version state via Named Pipe.
/// </summary>
public interface IDocumentIdentityQuery
{
    Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(CancellationToken cancellationToken = default);
    Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(bool dryRun, CancellationToken cancellationToken = default);
}

/// <summary>
/// Queries the live Revit Document state using a lightweight read-only script.
/// </summary>
public sealed class DocumentIdentityQuery : IDocumentIdentityQuery
{
    private readonly IRevitPipeClient _pipeClient;

    public const string QueryScript = """
        if (doc == null)
        {
            throw new InvalidOperationException("[ERR_NO_ACTIVE_DOCUMENT] No active document open in Revit.");
        }
        var docVer = Document.GetDocumentVersion(doc);
        return new
        {
            title = doc.Title ?? string.Empty,
            pathName = string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName,
            versionGuid = docVer != null ? docVer.VersionGUID.ToString("D") : Guid.Empty.ToString("D"),
            numberOfSaves = docVer != null ? docVer.NumberOfSaves : 0,
            centralGuid = (doc.IsWorkshared ? doc.WorksharingCentralGUID : Guid.Empty).ToString("D"),
            activeViewName = uidoc?.ActiveView?.Name
        };
        """;

    public DocumentIdentityQuery(IRevitPipeClient pipeClient)
    {
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
    }

    public Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(CancellationToken cancellationToken = default) =>
        QueryActiveDocumentAsync(dryRun: false, cancellationToken);

    public async Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        if (!_pipeClient.IsConnected)
        {
            await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        var request = new ExecuteRequest(
            Code: QueryScript,
            Transaction: TransactionModes.None,
            DryRun: dryRun,
            TimeoutSeconds: 30,
            Label: "Query Active Document Identity");

        var result = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsError || result.Value == null)
        {
            throw new RevitPipeException($"Failed to query active document identity: {result.Message}");
        }

        return ParseSnapshot(result.Value.Value);
    }

    public static DocumentIdentitySnapshot ParseSnapshot(JsonElement root)
    {
        string title = GetString(root, "title") ?? string.Empty;
        string? pathName = GetString(root, "pathName");
        string? versionGuidStr = GetString(root, "versionGuid");
        int numberOfSaves = GetInt(root, "numberOfSaves");
        string? centralGuidStr = GetString(root, "centralGuid");
        string? activeViewName = GetString(root, "activeViewName");

        Guid versionGuid = Guid.TryParse(versionGuidStr, out var vGuid) ? vGuid : Guid.Empty;
        Guid? centralGuid = Guid.TryParse(centralGuidStr, out var cGuid) && cGuid != Guid.Empty ? cGuid : null;

        return new DocumentIdentitySnapshot
        {
            Title = title,
            PathName = pathName,
            VersionGuid = versionGuid,
            NumberOfSaves = numberOfSaves,
            CentralGuid = centralGuid,
            ActiveViewName = activeViewName
        };
    }

    private static string? GetString(JsonElement elem, string prop) =>
        elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int GetInt(JsonElement elem, string prop) =>
        elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : 0;
}
