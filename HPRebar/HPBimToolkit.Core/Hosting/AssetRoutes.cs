namespace HPBimToolkit.Core.Hosting;

/// <summary>
///     Maps URLs of the toolkit's virtual host to the frontend files embedded in the add-in, and says how to serve them.
///     The page is a single-page app with hash routing, so only real files are ever requested.
/// </summary>
public static class AssetRoutes
{
    /// <summary>Prefix of the embedded resource names the build gives the frontend's <c>dist/</c> files.</summary>
    public const string ResourcePrefix = "wwwroot/";

    public const string IndexPath = "index.html";

    /// <summary>
    ///     Policy for the page: only its own bundled files run; inline styles are allowed because the UI library injects
    ///     some, inline scripts are not. Nothing reaches the network.
    /// </summary>
    public const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'none'; form-action 'none'";

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".mjs"] = "text/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".ico"] = "image/x-icon",
        [".woff2"] = "font/woff2",
    };

    /// <summary>
    ///     The embedded resource name for a request path (<c>/assets/app.js</c> → <c>wwwroot/assets/app.js</c>);
    ///     <c>/</c> serves the index. Null for paths that try to leave the root or carry no file name.
    /// </summary>
    public static string? ToResourceName(string absolutePath)
    {
        var path = Uri.UnescapeDataString(absolutePath).Replace('\\', '/').TrimStart('/');
        if (path.Length == 0)
        {
            path = IndexPath;
        }

        var segments = path.Split('/');
        if (segments.Any(segment => segment is "" or "." or ".."))
        {
            return null;
        }

        return ResourcePrefix + path;
    }

    /// <summary>Content type by extension; unknown extensions are served as opaque bytes.</summary>
    public static string ContentTypeFor(string resourceName)
    {
        var extension = System.IO.Path.GetExtension(resourceName);
        return ContentTypes.TryGetValue(extension, out var type) ? type : "application/octet-stream";
    }

    public static bool IsHtml(string resourceName) =>
        resourceName.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
}
