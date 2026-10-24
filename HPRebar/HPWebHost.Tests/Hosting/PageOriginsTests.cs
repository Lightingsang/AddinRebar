using HPWebHost.Hosting;
using Xunit;

namespace HPWebHost.Tests.Hosting;

public sealed class PageOriginsTests
{
    private const string Host = "toolkit.hpbim.local";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("https://evil.example.com/")]
    [InlineData("file:///C:/temp/index.html")]
    [InlineData("http://localhost.evil.com:5173/")]
    public void Resolve_NoSafeDevServer_UsesEmbeddedPage(string? devUrl)
    {
        var origins = PageOrigins.Resolve(Host, devUrl);

        Assert.False(origins.IsDevServer);
        Assert.Equal("https://toolkit.hpbim.local/index.html", origins.StartUrl);
        Assert.Equal("https://toolkit.hpbim.local", origins.TrustedOrigin);
    }

    [Theory]
    [InlineData("http://localhost:5173/", "http://localhost:5173")]
    [InlineData("http://127.0.0.1:4000/app", "http://127.0.0.1:4000")]
    public void Resolve_LocalDevServer_UsesIt(string devUrl, string expectedOrigin)
    {
        var origins = PageOrigins.Resolve(Host, devUrl);

        Assert.True(origins.IsDevServer);
        Assert.Equal(expectedOrigin, origins.TrustedOrigin);
    }

    [Theory]
    [InlineData("https://toolkit.hpbim.local/index.html", true)]
    [InlineData("https://toolkit.hpbim.local/#/dashboard", true)]
    [InlineData("https://TOOLKIT.hpbim.local/", true)]
    [InlineData("http://toolkit.hpbim.local/", false)]
    [InlineData("https://toolkit.hpbim.local.evil.com/", false)]
    [InlineData("https://toolkit.hpbim.local:8443/", false)]
    [InlineData("about:blank", false)]
    [InlineData(null, false)]
    public void IsTrusted_EmbeddedOrigin_MatchesSchemeHostAndPortOnly(string? source, bool expected)
    {
        Assert.Equal(expected, PageOrigins.Resolve(Host, null).IsTrusted(source));
    }

    [Fact]
    public void Resolve_StartPage_OpensThatPageOnItsOwnHost()
    {
        var origins = PageOrigins.Resolve("kata.hprebar.local", null, "kata.html");

        Assert.Equal("https://kata.hprebar.local/kata.html", origins.StartUrl);
        Assert.Equal("https://kata.hprebar.local", origins.EmbeddedOrigin);
        Assert.False(origins.IsTrusted("https://toolkit.hpbim.local/index.html"));
    }
}
