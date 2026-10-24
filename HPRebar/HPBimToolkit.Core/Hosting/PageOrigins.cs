namespace HPBimToolkit.Core.Hosting;

/// <summary>
///     Decides where the pane's page comes from and which page origins may talk to Revit: the embedded virtual host,
///     or — for frontend development only — a Vite dev server on this machine named by an environment variable.
/// </summary>
public sealed class PageOrigins
{
    public const string VirtualHost = "toolkit.hpbim.local";

    public const string EmbeddedOrigin = "https://" + VirtualHost;

    private PageOrigins(string startUrl, string trustedOrigin, bool isDevServer)
    {
        StartUrl = startUrl;
        TrustedOrigin = trustedOrigin;
        IsDevServer = isDevServer;
    }

    public string StartUrl { get; }

    public string TrustedOrigin { get; }

    public bool IsDevServer { get; }

    /// <summary>
    ///     Uses <paramref name="devServerUrl" /> only when it is an http(s) URL on localhost / 127.0.0.1, so a stray
    ///     variable can never point Revit's bridge at a remote page. Anything else falls back to the embedded page.
    /// </summary>
    public static PageOrigins Resolve(string? devServerUrl)
    {
        if (Uri.TryCreate(devServerUrl, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" &&
            uri.Host is "localhost" or "127.0.0.1")
        {
            return new PageOrigins(uri.AbsoluteUri, uri.GetLeftPart(UriPartial.Authority), isDevServer: true);
        }

        return new PageOrigins($"{EmbeddedOrigin}/{AssetRoutes.IndexPath}", EmbeddedOrigin, isDevServer: false);
    }

    /// <summary>True when a message's source URL belongs to the trusted origin (scheme, host and port all match).</summary>
    public bool IsTrusted(string? sourceUrl) =>
        Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
        string.Equals(uri.GetLeftPart(UriPartial.Authority), TrustedOrigin, StringComparison.OrdinalIgnoreCase);
}
