using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ETABSv1;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Contract for managing a unified, thread-safe ETABS OAPI session across all MCP tools.
/// </summary>
public interface IEtabsSessionManager : IDisposable
{
    /// <summary>Current connection state.</summary>
    EtabsConnectionState State { get; }

    /// <summary>True if currently connected to an active, healthy ETABS instance.</summary>
    bool IsConnected { get; }

    /// <summary>Active ETABS process PID (0 if not connected).</summary>
    int Pid { get; }

    /// <summary>True if the ETABS application window is currently visible on desktop.</summary>
    bool IsVisible { get; }

    /// <summary>Controls visibility of the ETABS application window on desktop.</summary>
    Task<bool> SetVisibleAsync(bool visible, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Centralized connection entry: checks health, attaches to existing instance,
    ///     or automatically launches a new ETABS instance if none is running.
    /// </summary>
    Task<EtabsConnectionResult> EnsureConnectedAsync(EtabsConnectionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes a delegate against the active <see cref="cSapModel"/> on the dedicated STA thread,
    ///     ensuring auto-connection and full serialization.
    /// </summary>
    Task<T> ExecuteWithSapModelAsync<T>(Func<cSapModel, T> action, CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the active ETABS instance and cleans up COM proxies.</summary>
    Task DisconnectAsync(string reason);
}

/// <summary>
///     Singleton implementation of <see cref="IEtabsSessionManager"/>.
///     <para>
///         <b>Registration in DI:</b>
///         <code>
///             services.AddSingleton&lt;IEtabsSessionManager, EtabsSessionManager&gt;();
///         </code>
///     </para>
///     <para>
///         <b>Example MCP Tool Injection:</b>
///         <code>
///             [McpServerToolType]
///             public sealed class GetStoriesTool(IEtabsSessionManager sessionManager)
///             {
///                 [McpServerTool(Name = "get_stories", Title = "Get Stories")]
///                 public async Task&lt;CallToolResult&gt; RunAsync(CancellationToken ct)
///                 {
///                     return await sessionManager.ExecuteWithSapModelAsync(sapModel =>
///                     {
///                         int count = 0;
///                         string[]? names = null;
///                         int ret = sapModel.Story.GetNameList(ref count, ref names);
///                         if (ret != 0) throw new InvalidOperationException($"Failed to get stories: {ret}");
///                         return new CallToolResult { Content = [new TextContentBlock { Text = string.Join(", ", names ?? []) }] };
///                     }, ct);
///                 }
///             }
///         </code>
///     </para>
/// </summary>
public sealed class EtabsSessionManager : IEtabsSessionManager
{
    private readonly object _gate = new();
    private readonly SemaphoreSlim _execLock = new(1, 1);
    private readonly EtabsAttachment _attachment;
    private EtabsConnectionConfig _config;

    public EtabsSessionManager(EtabsAttachment? attachment = null, EtabsConnectionConfig? config = null)
    {
        _attachment = attachment ?? new EtabsAttachment();
        _config = config ?? new EtabsConnectionConfig();
        _attachment.Config = _config;
    }

    public EtabsConnectionState State
    {
        get
        {
            if (!_attachment.Attached) return EtabsConnectionState.failed;
            return _attachment.IsHealthy() ? EtabsConnectionState.already_connected : EtabsConnectionState.failed;
        }
    }

    public bool IsConnected => _attachment.Attached && _attachment.IsHealthy();

    public int Pid => _attachment.Pid;

    public bool IsVisible => _attachment.IsVisible;

    public async Task<bool> SetVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        await _execLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _attachment.SetVisible(visible);
        }
        finally
        {
            _execLock.Release();
        }
    }

    public async Task<EtabsConnectionResult> EnsureConnectedAsync(EtabsConnectionConfig? config = null, CancellationToken cancellationToken = default)
    {
        await _execLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (config is not null)
            {
                _config = config;
                _attachment.Config = config;
            }

            return _attachment.EnsureConnected(_config);
        }
        finally
        {
            _execLock.Release();
        }
    }

    public async Task<T> ExecuteWithSapModelAsync<T>(Func<cSapModel, T> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _execLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var conn = _attachment.EnsureConnected(_config);
            if (conn.State == EtabsConnectionState.failed)
            {
                throw new InvalidOperationException($"Unable to connect or start ETABS: {conn.ErrorMessage}");
            }

            var (_, sapModel) = _attachment.Require();
            return action(sapModel);
        }
        catch (Exception ex)
        {
            _attachment.DetachIfGone(ex);
            throw;
        }
        finally
        {
            _execLock.Release();
        }
    }

    public async Task DisconnectAsync(string reason)
    {
        await _execLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _attachment.Detach(reason);
        }
        finally
        {
            _execLock.Release();
        }
    }

    public void Dispose()
    {
        _execLock.Dispose();
        _attachment.Dispose();
    }
}
