using System.IO;
using System.Windows;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;

namespace HPAutoCad.McpBridge;

/// <summary>
///     Entry points the loader's Ribbon tab uses beside the command ones. Same rule as the rest of the
///     dictionary: only BCL types cross the load-context boundary (string, bool, Action), never a bridge type.
/// </summary>
public static partial class BridgeEntry
{
    /// <summary>Folder of the MCP server's tool library — the AI's stored tools, one folder per tool.</summary>
    private static readonly string ToolLibraryDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), VendorFolder, "McpServer", "tools-library");

    private static void AddRibbonEntryPoints(Dictionary<string, Delegate> entries, McpBridgeHost host, BridgeSettingsStore store)
    {
        // Subscribe to state changes: the callback gets (kind, text) now and on every change; the returned Action unsubscribes.
        entries["status.subscribe"] = new Func<Action<string, string>, Action>(callback =>
        {
            void Publish() => callback(StatusKind(host), StatusText(host));
            host.StateChanged += Publish;
            Publish();
            return () => host.StateChanged -= Publish;
        });
        entries["copyLastScript"] = new Func<string>(() =>
        {
            if (host.LastRun is not { } run) return "[HPAutoCad MCP] Chưa có script nào chạy trong phiên này.";
            Clipboard.SetText(run.Source);
            return $"[HPAutoCad MCP] Đã sao chép script '{run.Label}' ({run.Source.Length} ký tự) vào clipboard.";
        });
        entries["autoStart.get"] = new Func<bool>(() => host.AutoStartListener);
        entries["autoStart.set"] = new Action<bool>(value => host.AutoStartListener = value);
        entries["path"] = new Func<string, string>(key => key switch
        {
            "logs" => LogDirectory,
            "audit" => store.AuditDirectory,
            "settings" => store.Directory,
            "library" => ToolLibraryDirectory,
            _ => throw new ArgumentException($"Unknown path '{key}'"),
        });
    }

    /// <summary>NotStarted | Stopped | Listening | Connected | Busy | Error — what the Ribbon colours/labels by.</summary>
    private static string StatusKind(McpBridgeHost host) => host.Status switch
    {
        BridgeStatus.Stopped => "Stopped",
        BridgeStatus.Listening => host.HasClient ? "Connected" : "Listening",
        BridgeStatus.Connected => "Connected",
        BridgeStatus.Busy => "Busy",
        BridgeStatus.Error => "Error",
        _ => "Stopped",
    };

    /// <summary>One line for the Ribbon: the three states a user must tell apart, plus the opt-in — never a bare "Connected".</summary>
    private static string StatusText(McpBridgeHost host)
    {
        var state = host.Status switch
        {
            BridgeStatus.Stopped => "Bridge sẵn sàng — listener tắt",
            BridgeStatus.Listening => host.HasClient ? "MCP server đã kết nối" : $"Đang lắng nghe trên {host.PipeName} — chờ MCP server",
            BridgeStatus.Connected => "MCP server đã kết nối",
            BridgeStatus.Busy => "MCP server đã kết nối — đang chạy script",
            BridgeStatus.Error => "Lỗi listener: " + (host.StatusMessage ?? "xem nhật ký"),
            _ => host.Status.ToString(),
        };
        return $"{state} · AI code: {(host.ExecutionEnabled ? "BẬT" : "TẮT")}";
    }
}
