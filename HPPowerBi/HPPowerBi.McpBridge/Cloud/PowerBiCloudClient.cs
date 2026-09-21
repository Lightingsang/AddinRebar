using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Serilog;

namespace HPPowerBi.McpBridge.Cloud;

#region Cloud DTOs

public sealed record CloudWorkspaceDto(
    string Id,
    string Name,
    bool IsReadOnly,
    string? Type);

public sealed record CloudDatasetDto(
    string Id,
    string Name,
    string? ConfiguredBy,
    bool IsRefreshable,
    string? TargetStorageMode);

public sealed record CloudRefreshResultDto(
    bool Success,
    string DatasetId,
    string? RequestId,
    string Status,
    string? ErrorMessage = null);

public sealed record CloudDaxResultDto(
    bool Success,
    JsonElement? Results,
    string? ErrorMessage = null);

#endregion

/// <summary>
///     Client for Power BI Service REST API using MSAL.NET OAuth 2.0 authentication.
///     Supports Service Principal (client credentials) and Interactive / Device Code flows.
/// </summary>
public sealed class PowerBiCloudClient : IDisposable
{
    public const string DefaultPowerBiScope = "https://analysis.windows.net/powerbi/api/.default";
    public const string BaseApiUrl = "https://api.powerbi.com/v1.0/myorg/";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt;

    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public PowerBiCloudClient(
        string? tenantId = null,
        string? clientId = null,
        string? clientSecret = null,
        HttpClient? httpClient = null)
    {
        TenantId = tenantId;
        ClientId = clientId;
        ClientSecret = clientSecret;

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _ownsHttpClient = true;
        }
    }

    /// <summary>
    ///     Acquires an OAuth 2.0 access token via MSAL.
    ///     Uses Service Principal credentials (ClientId + ClientSecret + TenantId) if provided.
    /// </summary>
    public async Task<string> AcquireTokenAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt.AddMinutes(-5))
            return _cachedToken;

        if (string.IsNullOrWhiteSpace(TenantId) || string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException("TenantId and ClientId must be configured to acquire a Power BI cloud token.");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            throw new InvalidOperationException("ClientSecret must be configured for Service Principal authentication.");
        }

        var app = ConfidentialClientApplicationBuilder.Create(ClientId)
            .WithClientSecret(ClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{TenantId}"))
            .Build();

        var scopes = new[] { DefaultPowerBiScope };
        var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync(ct).ConfigureAwait(false);

        _cachedToken = authResult.AccessToken;
        _tokenExpiresAt = authResult.ExpiresOn;

        Log.Information("Acquired Power BI Cloud access token, expires: {Expires}", _tokenExpiresAt);
        return _cachedToken;
    }

    /// <summary>
    ///     Sets an explicit bearer token (for testing or external token acquisition).
    /// </summary>
    public void SetToken(string token, DateTimeOffset? expiresOn = null)
    {
        _cachedToken = token;
        _tokenExpiresAt = expiresOn ?? DateTimeOffset.UtcNow.AddHours(1);
    }

    /// <summary>
    ///     Lists accessible Power BI Service workspaces via GET v1.0/myorg/groups.
    /// </summary>
    public async Task<IReadOnlyList<CloudWorkspaceDto>> ListWorkspacesAsync(int top = 100, CancellationToken ct = default)
    {
        var token = await AcquireTokenAsync(ct).ConfigureAwait(false);
        var url = $"{BaseApiUrl}groups?$top={Math.Clamp(top, 1, 1000)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Power BI API error ({response.StatusCode}): {json}");
        }

        var results = new List<CloudWorkspaceDto>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("value", out var valueArray) && valueArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in valueArray.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? string.Empty;
                var name = item.GetProperty("name").GetString() ?? string.Empty;
                var isReadOnly = item.TryGetProperty("isReadOnly", out var ro) && ro.GetBoolean();
                var type = item.TryGetProperty("type", out var t) ? t.GetString() : null;

                results.Add(new CloudWorkspaceDto(id, name, isReadOnly, type));
            }
        }

        return results;
    }

    /// <summary>
    ///     Lists datasets within a specific workspace or "My Workspace".
    /// </summary>
    public async Task<IReadOnlyList<CloudDatasetDto>> ListDatasetsAsync(string? workspaceId = null, CancellationToken ct = default)
    {
        var token = await AcquireTokenAsync(ct).ConfigureAwait(false);
        var url = !string.IsNullOrWhiteSpace(workspaceId)
            ? $"{BaseApiUrl}groups/{workspaceId}/datasets"
            : $"{BaseApiUrl}datasets";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Power BI API error ({response.StatusCode}): {json}");
        }

        var results = new List<CloudDatasetDto>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("value", out var valueArray) && valueArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in valueArray.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? string.Empty;
                var name = item.GetProperty("name").GetString() ?? string.Empty;
                var configuredBy = item.TryGetProperty("configuredBy", out var cb) ? cb.GetString() : null;
                var isRefreshable = item.TryGetProperty("isRefreshable", out var ir) && ir.GetBoolean();
                var targetStorageMode = item.TryGetProperty("targetStorageMode", out var tsm) ? tsm.GetString() : null;

                results.Add(new CloudDatasetDto(id, name, configuredBy, isRefreshable, targetStorageMode));
            }
        }

        return results;
    }

    /// <summary>
    ///     Triggers an on-demand dataset refresh in Power BI Service.
    /// </summary>
    public async Task<CloudRefreshResultDto> TriggerRefreshAsync(
        string datasetId,
        string? workspaceId = null,
        string notifyOption = "NoNotification",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
            throw new ArgumentException("DatasetId cannot be empty.", nameof(datasetId));

        var token = await AcquireTokenAsync(ct).ConfigureAwait(false);
        var url = !string.IsNullOrWhiteSpace(workspaceId)
            ? $"{BaseApiUrl}groups/{workspaceId}/datasets/{datasetId}/refreshes"
            : $"{BaseApiUrl}datasets/{datasetId}/refreshes";

        var payload = JsonSerializer.Serialize(new { notifyOption });
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string? requestId = null;
            if (response.Headers.TryGetValues("requestId", out var rIds))
            {
                using var enumerator = rIds.GetEnumerator();
                if (enumerator.MoveNext()) requestId = enumerator.Current;
            }

            return new CloudRefreshResultDto(true, datasetId, requestId, "Accepted");
        }

        return new CloudRefreshResultDto(false, datasetId, null, response.StatusCode.ToString(), content);
    }

    /// <summary>
    ///     Executes a DAX query against a Power BI Service cloud dataset via REST API executeQueries endpoint.
    /// </summary>
    public async Task<CloudDaxResultDto> ExecuteDaxAsync(
        string datasetId,
        string query,
        string? workspaceId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(datasetId))
            throw new ArgumentException("DatasetId cannot be empty.", nameof(datasetId));
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("DAX query cannot be empty.", nameof(query));

        var token = await AcquireTokenAsync(ct).ConfigureAwait(false);
        var url = !string.IsNullOrWhiteSpace(workspaceId)
            ? $"{BaseApiUrl}groups/{workspaceId}/datasets/{datasetId}/executeQueries"
            : $"{BaseApiUrl}datasets/{datasetId}/executeQueries";

        var payload = JsonSerializer.Serialize(new
        {
            queries = new[] { new { query } },
            serializerSettings = new { includeNulls = true }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(content);
            return new CloudDaxResultDto(true, doc.RootElement.Clone());
        }

        return new CloudDaxResultDto(false, null, $"API error ({response.StatusCode}): {content}");
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
