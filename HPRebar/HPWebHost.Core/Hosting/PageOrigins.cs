namespace HPWebHost.Hosting;

/// <summary>
///     Decides where a host's page comes from and which page origins may talk to Revit: the embedded virtual host,
///     or — for frontend development only — a Vite dev server on this machine named by an environment variable.
/// </summary>
public sealed class PageOrigins
{
    private PageOrigins(string embeddedOrigin, string startUrl, string trustedOrigin, bool isDevServer)
    {
        EmbeddedOrigin = embeddedOrigin;
        StartUrl = startUrl;
        TrustedOrigin = trustedOrigin;
        IsDevServer = isDevServer;
    }

    /// <summary><c>https://&lt;virtual host&gt;</c>: where the embedded frontend is served, dev server or not.</summary>
    public string EmbeddedOrigin { get; }

    public string StartUrl { get; }

    public string TrustedOrigin { get; }

    public bool IsDevServer { get; }

    /// <summary>
    ///     Uses <paramref name="devServerUrl" /> only when it is an http(s) URL on localhost / 127.0.0.1, so a stray
    ///     variable can never point Revit's bridge at a remote page. Anything else falls back to the embedded page.
    /// </summary>
    /// <param name="virtualHost">Host name the embedded page is served on, one per add-in (e.g. <c>toolkit.hpbim.local</c>).</param>
    /// <param name="devServerUrl">The page on a local dev server, or null.</param>
    /// <param name="startPage">The embedded page opened first (the frontend builds one HTML file per window).</param>
    public static PageOrigins Resolve(string virtualHost, string? devServerUrl, string startPage = AssetRoutes.IndexPath)
    {
        var embeddedOrigin = "https://" + virtualHost;
        if (Uri.TryCreate(devServerUrl, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" &&
            uri.Host is "localhost" or "127.0.0.1")
        {
            return new PageOrigins(embeddedOrigin, uri.AbsoluteUri, uri.GetLeftPart(UriPartial.Authority), isDevServer: true);
        }

        return new PageOrigins(embeddedOrigin, $"{embeddedOrigin}/{startPage}", embeddedOrigin, isDevServer: false);
    }

    /// <summary>True when a message's source URL belongs to the trusted origin (scheme, host and port all match).</summary>
    public bool IsTrusted(string? sourceUrl) =>
        Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
        string.Equals(uri.GetLeftPart(UriPartial.Authority), TrustedOrigin, StringComparison.OrdinalIgnoreCase);
}
