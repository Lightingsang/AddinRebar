using HPWebHost.Hosting;
using Xunit;

namespace HPWebHost.Tests.Hosting;

public sealed class AssetRoutesTests
{
    [Theory]
    [InlineData("/", "wwwroot/index.html")]
    [InlineData("", "wwwroot/index.html")]
    [InlineData("/index.html", "wwwroot/index.html")]
    [InlineData("/assets/index-abc.js", "wwwroot/assets/index-abc.js")]
    [InlineData("/assets/my%20font.woff2", "wwwroot/assets/my font.woff2")]
    public void ToResourceName_FilePaths_MapUnderWwwroot(string path, string expected)
    {
        Assert.Equal(expected, AssetRoutes.ToResourceName(path));
    }

    [Theory]
    [InlineData("/../secrets.txt")]
    [InlineData("/assets/../../x.js")]
    [InlineData("/assets//x.js")]
    [InlineData("/assets/%2e%2e/x.js")]
    [InlineData("/assets/")]
    public void ToResourceName_EscapingOrEmptySegments_ReturnsNull(string path)
    {
        Assert.Null(AssetRoutes.ToResourceName(path));
    }

    [Theory]
    [InlineData("wwwroot/index.html", "text/html; charset=utf-8")]
    [InlineData("wwwroot/assets/a.JS", "text/javascript; charset=utf-8")]
    [InlineData("wwwroot/assets/a.css", "text/css; charset=utf-8")]
    [InlineData("wwwroot/assets/a.bin", "application/octet-stream")]
    public void ContentTypeFor_KnownAndUnknownExtensions_ReturnsType(string resource, string expected)
    {
        Assert.Equal(expected, AssetRoutes.ContentTypeFor(resource));
    }
}
