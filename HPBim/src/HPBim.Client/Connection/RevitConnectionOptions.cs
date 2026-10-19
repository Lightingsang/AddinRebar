namespace HPBim.Client.Connection;

using HPRebar.Mcp.Contracts;

/// <summary>
///     Options for connecting to the Revit Named Pipe bridge.
/// </summary>
public sealed record RevitConnectionOptions
{
    /// <summary>
    ///     Named pipe name. Defaults to standard Revit 2026 pipe ("hprebar-mcp-r2026").
    /// </summary>
    public string PipeName { get; init; } = PipeNaming.For(2026);

    /// <summary>
    ///     Timeout for establishing named pipe connection in milliseconds.
    /// </summary>
    public int ConnectTimeoutMs { get; init; } = 5000;

    /// <summary>
    ///     Default execution timeout for scripts in seconds.
    /// </summary>
    public int DefaultExecuteTimeoutSeconds { get; init; } = 120;

    /// <summary>
    ///     Maximum permitted incoming message size in bytes. Default is 10 MB.
    /// </summary>
    public int MaxMessageBytes { get; init; } = 10 * 1024 * 1024;

    /// <summary>
    ///     Validates connection options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PipeName))
        {
            throw new ArgumentException("PipeName cannot be null or whitespace.", nameof(PipeName));
        }

        if (ConnectTimeoutMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ConnectTimeoutMs), "ConnectTimeoutMs must be positive.");
        }

        if (DefaultExecuteTimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DefaultExecuteTimeoutSeconds), "DefaultExecuteTimeoutSeconds must be positive.");
        }

        if (MaxMessageBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxMessageBytes), "MaxMessageBytes must be positive.");
        }
    }
}
