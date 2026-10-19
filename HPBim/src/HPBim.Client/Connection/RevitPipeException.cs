namespace HPBim.Client.Connection;

using System;

/// <summary>
///     Exception thrown during Revit Named Pipe transport or JSON-RPC execution errors.
/// </summary>
public sealed class RevitPipeException : Exception
{
    public int? ErrorCode { get; }
    public bool IsBridgeError { get; }
    public bool IsTimeout { get; }

    public RevitPipeException(string message)
        : base(message)
    {
    }

    public RevitPipeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RevitPipeException(string message, int errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
        IsBridgeError = true;
    }

    public RevitPipeException(string message, bool isTimeout, bool isBridgeError = false)
        : base(message)
    {
        IsTimeout = isTimeout;
        IsBridgeError = isBridgeError;
    }
}
