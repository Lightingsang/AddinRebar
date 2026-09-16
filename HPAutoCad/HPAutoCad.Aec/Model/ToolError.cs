namespace HPAutoCad.Aec.Model;

/// <summary>Stable error codes every AEC tool uses; the AI matches on these, never on message text.</summary>
public static class ToolErrorCode
{
    public const string InvalidArgument = "INVALID_ARGUMENT";
    public const string InvalidHandle = "INVALID_HANDLE";
    public const string Erased = "ERASED";
    public const string NotAnEntity = "NOT_AN_ENTITY";
    public const string UnsupportedEntity = "UNSUPPORTED_ENTITY";
    public const string NoGeometry = "NO_GEOMETRY";
    public const string LayerLocked = "LAYER_LOCKED";
    public const string LayerFrozen = "LAYER_FROZEN";
    public const string LimitExceeded = "LIMIT_EXCEEDED";
    public const string NotClosed = "NOT_CLOSED";
    public const string NoDocument = "NO_DOCUMENT";
    public const string Internal = "INTERNAL";
}

/// <summary>One structured error or warning: a code, a sentence, and the handle it is about when there is one.</summary>
public sealed record ToolError(string Code, string Message, string? Handle = null)
{
    public static ToolError Argument(string message) => new(ToolErrorCode.InvalidArgument, message);

    public static ToolError ForHandle(string code, string handle, string message) => new(code, message, handle);
}
