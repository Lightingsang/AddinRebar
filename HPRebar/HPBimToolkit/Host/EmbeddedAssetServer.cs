using System.IO;
using System.Reflection;
using System.Text;
using HPBimToolkit.Core.Hosting;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace HPBimToolkit.Host;

/// <summary>
///     Serves the frontend build embedded in this assembly on <see cref="PageOrigins.EmbeddedOrigin" />, so the pane
///     needs no loose files and no network. Requests to that host never leave the process.
/// </summary>
public sealed class EmbeddedAssetServer(Assembly assembly, PageOrigins origins, ILogger logger)
{
    private const string MissingFrontendPage = """
        <!doctype html><html><head><meta charset="utf-8"><title>HP BIM Toolkit</title></head>
        <body style="font-family:Segoe UI,sans-serif;padding:16px">
        <h3>HP BIM Toolkit</h3>
        <p>The dashboard page was not built into this add-in. Build HPBimToolkit with Node.js 24 installed
        (the build runs <code>npm ci</code> and <code>npm run build</code> in HPBimToolkit.Frontend).</p>
        </body></html>
        """;

    /// <summary>Hooks the virtual host into <paramref name="core" /> and opens the start page.</summary>
    public void Attach(CoreWebView2 core)
    {
        if (origins.IsDevServer)
        {
            logger.Warning("Loading the frontend from the dev server {Url}", origins.StartUrl);
        }
        else
        {
            core.AddWebResourceRequestedFilter($"{PageOrigins.EmbeddedOrigin}/*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) => e.Response = CreateResponse(core.Environment, e.Request.Uri);
        }

        // The pane only ever shows the toolkit page: no navigation away, no pop-up windows.
        core.NavigationStarting += (_, e) =>
        {
            if (!origins.IsTrusted(e.Uri))
            {
                logger.Warning("Blocked navigation to {Uri}", e.Uri);
                e.Cancel = true;
            }
        };
        core.NewWindowRequested += (_, e) =>
        {
            logger.Warning("Blocked a new window for {Uri}", e.Uri);
            e.Handled = true;
        };

        core.Navigate(origins.StartUrl);
    }

    private CoreWebView2WebResourceResponse CreateResponse(CoreWebView2Environment environment, string requestUri)
    {
        var resourceName = AssetRoutes.ToResourceName(new Uri(requestUri).AbsolutePath);
        var stream = resourceName is null ? null : assembly.GetManifestResourceStream(resourceName);

        if (stream is null && resourceName == AssetRoutes.ResourcePrefix + AssetRoutes.IndexPath)
        {
            logger.Warning("No embedded frontend in {Assembly}; serving the placeholder page", assembly.GetName().Name);
            stream = new MemoryStream(Encoding.UTF8.GetBytes(MissingFrontendPage));
        }

        if (stream is null)
        {
            logger.Debug("Asset not found: {Uri}", requestUri);
            return environment.CreateWebResourceResponse(null, 404, "Not Found", "Content-Type: text/plain");
        }

        return environment.CreateWebResourceResponse(stream, 200, "OK", BuildHeaders(resourceName!));
    }

    private static string BuildHeaders(string resourceName)
    {
        var headers = new StringBuilder()
            .Append("Content-Type: ").Append(AssetRoutes.ContentTypeFor(resourceName)).Append("\r\n")
            .Append("Cache-Control: no-cache\r\n")
            .Append("X-Content-Type-Options: nosniff");
        if (AssetRoutes.IsHtml(resourceName))
        {
            headers.Append("\r\nContent-Security-Policy: ").Append(AssetRoutes.ContentSecurityPolicy);
        }

        return headers.ToString();
    }
}
