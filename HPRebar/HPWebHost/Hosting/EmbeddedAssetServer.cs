using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace HPWebHost.Hosting;

/// <summary>
///     Serves the frontend build embedded in <c>assembly</c> on <see cref="PageOrigins.EmbeddedOrigin" />, so the
///     window needs no loose files and no network. Requests to that host never leave the process.
/// </summary>
/// <param name="productName">Named on the placeholder page shown when the add-in was built without its frontend.</param>
public sealed class EmbeddedAssetServer(Assembly assembly, PageOrigins origins, string productName, ILogger logger)
{

    /// <summary>Hooks the virtual host into <paramref name="core" /> and opens the start page.</summary>
    public void Attach(CoreWebView2 core)
    {
        if (origins.IsDevServer)
        {
            logger.Warning("Loading the frontend from the dev server {Url}", origins.StartUrl);
        }
        else
        {
            core.AddWebResourceRequestedFilter($"{origins.EmbeddedOrigin}/*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) => e.Response = CreateResponse(core.Environment, e.Request.Uri);
        }

        // The window only ever shows its own page: no navigation away, no pop-up windows.
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

        if (stream is null && resourceName is not null && AssetRoutes.IsHtml(resourceName))
        {
            logger.Warning("{Page} is not embedded in {Assembly}; serving the placeholder page", resourceName, assembly.GetName().Name);
            stream = new MemoryStream(Encoding.UTF8.GetBytes(MissingFrontendPage()));
        }

        if (stream is null)
        {
            logger.Debug("Asset not found: {Uri}", requestUri);
            return environment.CreateWebResourceResponse(null, 404, "Not Found", "Content-Type: text/plain");
        }

        return environment.CreateWebResourceResponse(stream, 200, "OK", BuildHeaders(resourceName!));
    }

    private string MissingFrontendPage() => $"""
        <!doctype html><html><head><meta charset="utf-8"><title>{productName}</title></head>
        <body style="font-family:Segoe UI,sans-serif;padding:16px">
        <h3>{productName}</h3>
        <p>The page was not built into this add-in. Build it with Node.js 24 installed
        (the build runs <code>npm ci</code> and <code>npm run build</code> in HPBimToolkit.Frontend).</p>
        </body></html>
        """;

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
