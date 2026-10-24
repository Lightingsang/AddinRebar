using System.Text.Json;

namespace HPWebHost.Protocol;

/// <summary>Message kinds of protocol v1, as they appear in the <c>kind</c> field.</summary>
public static class EnvelopeKinds
{
    public const string Request = "request";
    public const string Response = "response";
    public const string Event = "event";
}

/// <summary>A call from the page: <c>{v:1, kind:"request", id, command, payload}</c>.</summary>
public sealed record RequestEnvelope(string Id, string Command, JsonElement? Payload);

/// <summary>A machine-readable failure: a code from <see cref="BridgeErrorCodes" /> plus text for the user.</summary>
public sealed record BridgeError(string Code, string Message, object? Details = null);

/// <summary>The answer to one request; exactly one of <see cref="Payload" /> and <see cref="Error" /> is meaningful.</summary>
public sealed record ResponseEnvelope(string Id, bool Ok, object? Payload, BridgeError? Error)
{
    public int V => ProtocolVersion.Current;

    public string Kind => EnvelopeKinds.Response;

    public static ResponseEnvelope Success(string id, object? payload) => new(id, true, payload, null);

    public static ResponseEnvelope Failure(string id, BridgeError error) => new(id, false, null, error);
}

/// <summary>A push from Revit to the page that answers no request: <c>{v:1, kind:"event", event, payload}</c>.</summary>
public sealed record EventEnvelope(string Event, object? Payload)
{
    public int V => ProtocolVersion.Current;

    public string Kind => EnvelopeKinds.Event;
}

public static class ProtocolVersion
{
    public const int Current = 1;
}
