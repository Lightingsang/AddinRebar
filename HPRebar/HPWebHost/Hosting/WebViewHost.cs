using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Windows.Controls;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using Grid = System.Windows.Controls.Grid;

namespace HPWebHost.Hosting;

/// <summary>Who owns a <see cref="WebViewHost" />: where its browser profile lives and what its failure text names.</summary>
/// <param name="UserDataFolder">Browser profile folder; Revit's install folder is read-only, so it must live elsewhere.</param>
/// <param name="DebugPortVariable">Environment variable that, set to a port, opens the DevTools protocol for harnesses.</param>
/// <param name="ProductName">Named in the message shown when the browser cannot start.</param>
/// <param name="LogFolder">Shown with that message, so the user knows where to look.</param>
public sealed record WebViewHostOptions(string UserDataFolder, string DebugPortVariable, string ProductName, string LogFolder);

/// <summary>
///     A WebView2 created the first time the control is shown (Revit builds every dockable pane at start-up, a browser
///     per hidden pane would cost start time for nothing). Owns the browser and nothing else — what is loaded into it
///     and what its messages mean belong to the caller.
/// </summary>
public sealed class WebViewHost : UserControl, IDisposable
{
    private readonly Grid _root = new();
    private readonly WebViewHostOptions _options;
    private readonly Action<CoreWebView2> _navigate;
    private readonly ILogger _logger;
    private WebView2? _webView;
    private bool _hasStarted;
    private bool _isDisposed;

    /// <param name="navigate">Loads the first page once the browser core exists (handlers are attached before it runs).</param>
    public WebViewHost(WebViewHostOptions options, Action<CoreWebView2> navigate, ILogger logger)
    {
        _options = options;
        _navigate = navigate;
        _logger = logger;
        Content = _root;
        Loaded += OnLoaded;
    }

    /// <summary>Raised on the UI thread with the sender page's URL and its message as JSON text.</summary>
    public event Action<string, string>? MessageReceived;

    public bool IsReady => _webView?.CoreWebView2 is not null;

    /// <summary>
    ///     Posts JSON to the page. Dropped (and logged) while the browser is not ready or after its process died —
    ///     callers include Revit event handlers and timers, where an exception would surface on Revit's UI thread.
    /// </summary>
    public void PostJson(string json)
    {
        if (_isDisposed || _webView?.CoreWebView2 is not { } core)
        {
            _logger.Debug("WebView not ready; dropped message of {Length} chars", json.Length);
            return;
        }

        try
        {
            core.PostWebMessageAsJson(json);
        }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        {
            _logger.Warning(exception, "Could not post to the page; message of {Length} chars dropped", json.Length);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Loaded -= OnLoaded;
        _webView?.Dispose();
        _webView = null;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasStarted || _isDisposed)
        {
            return;
        }

        _hasStarted = true;
        try
        {
            await StartBrowserAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "WebView2 could not start");
            ShowStartFailure(exception);
        }
    }

    private async Task StartBrowserAsync()
    {
        LogWebViewAssembly();
        var options = new CoreWebView2EnvironmentOptions();
        var userDataFolder = _options.UserDataFolder;
        var debugPort = Environment.GetEnvironmentVariable(_options.DebugPortVariable);
        if (!string.IsNullOrEmpty(debugPort))
        {
            // Opt-in for the live harness only: it reads the page through the DevTools protocol on this port. Its own
            // profile, because a profile already open in another Revit with other browser arguments refuses to start.
            options.AdditionalBrowserArguments = $"--remote-debugging-port={debugPort}";
            userDataFolder = $"{_options.UserDataFolder}-debug-{debugPort}";
            _logger.Warning("WebView2 remote debugging enabled on port {Port}", debugPort);
        }

        Directory.CreateDirectory(userDataFolder);
        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
        if (_isDisposed)
        {
            return; // the window closed while the browser environment was being created
        }

        var webView = new WebView2();
        _root.Children.Add(webView);
        _webView = webView;

        await webView.EnsureCoreWebView2Async(environment);
        if (_isDisposed)
        {
            webView.Dispose(); // Dispose ran before the control existed or while it was starting
            return;
        }

        var core = webView.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDevToolsEnabled = !string.IsNullOrEmpty(debugPort);
        core.Settings.IsZoomControlEnabled = false;
        // Handled from a normal dispatcher frame, not inside WebView2's COM event: the work behind a message (Revit
        // queue, JIT, assembly loads) must never run nested in the browser's callback.
        core.WebMessageReceived += (_, args) =>
        {
            var source = args.Source;
            var message = args.WebMessageAsJson;
            Dispatcher.BeginInvoke(() => MessageReceived?.Invoke(source, message));
        };
        core.ProcessFailed += OnProcessFailed;
        core.DownloadStarting += OnDownloadStarting;

        _logger.Information("WebView2 runtime {Version} ready, profile {Folder}", environment.BrowserVersionString, userDataFolder);
        _navigate(core);
    }

    /// <summary>
    ///     A file the page offers (an exported drawing, as a blob download) is saved where the user picks in a Save dialog
    ///     owned by this window, never silently into the Downloads folder; cancelling the dialog cancels the download.
    ///     The dialog runs from a normal dispatcher frame, outside WebView2's event.
    /// </summary>
    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        var deferral = e.GetDeferral();
        var suggested = Path.GetFileName(e.ResultFilePath);
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var extension = Path.GetExtension(suggested);
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = suggested,
                    DefaultExt = extension,
                    Filter = extension.Length > 1 ? $"{extension.TrimStart('.').ToUpperInvariant()} (*{extension})|*{extension}" : "All files (*.*)|*.*",
                };
                if (dialog.ShowDialog(Window.GetWindow(this)) == true)
                {
                    e.ResultFilePath = dialog.FileName;
                    e.Handled = true;
                    _logger.Information("Saving {File} from the page", dialog.FileName);
                }
                else
                {
                    e.Cancel = true;
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "The page's download {File} could not be saved", suggested);
                e.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        });
    }

    /// <summary>A crashed page reloads itself; a dead browser process cannot be revived, so the control says what happened.</summary>
    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        _logger.Error("WebView2 process failed: {Kind} ({Reason}, exit code {ExitCode})", e.ProcessFailedKind, e.Reason, e.ExitCode);
        if (e.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
        {
            _webView?.CoreWebView2?.Reload();
            return;
        }

        if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
        {
            _webView?.Dispose();
            _webView = null;
            ShowStartFailure(new InvalidOperationException($"The browser process stopped. Restart Revit to use {_options.ProductName} again."));
        }
    }

    private void LogWebViewAssembly()
    {
        var assembly = typeof(CoreWebView2Environment).Assembly;
        _logger.Information(
            "WebView2 managed API {Version} from {Location} in load context {Context}",
            assembly.GetName().Version,
            assembly.Location,
            AssemblyLoadContext.GetLoadContext(assembly)?.Name);
    }

    private void ShowStartFailure(Exception exception)
    {
        _root.Children.Clear();
        _root.Children.Add(new TextBlock
        {
            Text = $"{_options.ProductName} could not start its browser (Microsoft Edge WebView2).\n\n" +
                   $"{exception.Message}\n\nLog: {_options.LogFolder}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16),
        });
    }
}
