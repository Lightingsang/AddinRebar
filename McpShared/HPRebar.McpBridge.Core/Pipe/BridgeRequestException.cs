using HPRebar.Mcp.Contracts.JsonRpc;

namespace HPRebar.McpBridge.Core.Pipe;

/// <summary>
///     An executor failure that has a JSON-RPC code of its own — the host is busy past the grace period,
///     no document is open — so the dispatcher answers with that code instead of the generic internal
///     error. Codes are the actionable ones in <see cref="BridgeErrorCode"/>; the message is written for
///     the AI and must never contain machine paths.
/// </summary>
public sealed class BridgeRequestException : Exception
{
    public BridgeRequestException(int code, string message) : base(message)
    {
        Code = code;
    }

    public int Code { get; }

    public static BridgeRequestException Busy(string hostName) => new BridgeRequestException(BridgeErrorCode.Busy,
        $"{hostName} is running a command or showing a dialog. Press ESC or close the dialog in {hostName} and retry.");

    public static BridgeRequestException NoActiveDocument(string hostName, string documentNoun) => new BridgeRequestException(BridgeErrorCode.NoActiveDocument,
        $"No {documentNoun} is open in {hostName}. Open one first.");
}
