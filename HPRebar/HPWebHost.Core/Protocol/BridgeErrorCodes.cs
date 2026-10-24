namespace HPWebHost.Protocol;

/// <summary>Error codes of protocol v1. The page switches on these, so they never change once shipped.</summary>
public static class BridgeErrorCodes
{
    /// <summary>The message is not valid JSON, not a v1 request, or its payload does not fit the command.</summary>
    public const string BadRequest = "BAD_REQUEST";

    public const string UnknownCommand = "UNKNOWN_COMMAND";

    /// <summary>The command needs an open project and Revit has none active. The page shows its empty state.</summary>
    public const string NoDocument = "NO_DOCUMENT";

    /// <summary>The same long-running command is still running; the page retries later.</summary>
    public const string Busy = "BUSY";

    /// <summary>Revit did not run the request in time, typically because a modal dialog is open.</summary>
    public const string Timeout = "TIMEOUT";

    public const string Cancelled = "CANCELLED";

    /// <summary>The Revit API refused the operation (invalid element, read-only document, …).</summary>
    public const string RevitApiError = "REVIT_API_ERROR";

    public const string Internal = "INTERNAL";
}

/// <summary>Thrown by handlers to answer with a specific code; the dispatcher turns it into a failure response.</summary>
public sealed class BridgeException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
