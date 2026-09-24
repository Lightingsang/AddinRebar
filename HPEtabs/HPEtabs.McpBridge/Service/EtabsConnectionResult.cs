using System.Text.Json.Serialization;

namespace HPEtabs.McpBridge.Service;

/// <summary>State machine outcomes for an ETABS connection request.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EtabsConnectionState
{
    /// <summary>Connection was already active and healthy.</summary>
    already_connected,

    /// <summary>Attached successfully to an existing running ETABS instance.</summary>
    attached_existing,

    /// <summary>No running instance found; launched a new ETABS instance and initialized model.</summary>
    started_new,

    /// <summary>Failed to connect or start ETABS.</summary>
    failed,
}

/// <summary>Detailed outcome of EnsureConnected() or an etabs.connect RPC call.</summary>
public sealed record EtabsConnectionResult(
    EtabsConnectionState State,
    string? Version = null,
    double? VersionNumber = null,
    string? ModelTitle = null,
    int Pid = 0,
    string? ErrorMessage = null,
    string? Warning = null);
