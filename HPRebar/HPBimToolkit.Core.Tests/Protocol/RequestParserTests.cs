using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Protocol;
using Xunit;

namespace HPBimToolkit.Core.Tests.Protocol;

public sealed class RequestParserTests
{
    [Fact]
    public void Parse_ValidObject_ReturnsRequestWithPayload()
    {
        const string message = """{"v":1,"kind":"request","id":"a1","command":"dashboard.getStatistics","payload":{"scope":"activeView"}}""";

        var result = RequestParser.Parse(message);

        Assert.True(result.IsAccepted);
        Assert.Equal("a1", result.Request!.Id);
        Assert.Equal("dashboard.getStatistics", result.Request.Command);
        Assert.Equal("activeView", result.Request.Payload!.Value.GetProperty("scope").GetString());
    }

    [Fact]
    public void Parse_ObjectPostedAsJsonString_ReturnsRequest()
    {
        const string message = "\"{\\\"v\\\":1,\\\"kind\\\":\\\"request\\\",\\\"id\\\":\\\"s1\\\",\\\"command\\\":\\\"app.hello\\\"}\"";

        var result = RequestParser.Parse(message);

        Assert.True(result.IsAccepted);
        Assert.Equal("app.hello", result.Request!.Command);
        Assert.Null(result.Request.Payload);
    }

    [Fact]
    public void Parse_NullPayload_ReadsAsNoPayload()
    {
        var result = RequestParser.Parse("""{"v":1,"kind":"request","id":"n1","command":"app.hello","payload":null}""");

        Assert.Null(result.Request!.Payload);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("""{"v":1,"kind":"request","command":"app.hello"}""")]
    [InlineData("""{"v":1,"kind":"request","id":"","command":"app.hello"}""")]
    public void Parse_UnidentifiableMessage_RejectsWithoutId(string message)
    {
        var result = RequestParser.Parse(message);

        Assert.False(result.IsAccepted);
        Assert.Equal(string.Empty, result.Rejection!.Id);
        Assert.Equal(BridgeErrorCodes.BadRequest, result.Rejection.Error!.Code);
    }

    [Theory]
    [InlineData("""{"v":2,"kind":"request","id":"x","command":"app.hello"}""")]
    [InlineData("""{"kind":"request","id":"x","command":"app.hello"}""")]
    [InlineData("""{"v":1,"kind":"event","id":"x","command":"app.hello"}""")]
    [InlineData("""{"v":1,"kind":"request","id":"x","command":"  "}""")]
    public void Parse_IdentifiableButInvalid_RejectsKeepingId(string message)
    {
        var result = RequestParser.Parse(message);

        Assert.False(result.IsAccepted);
        Assert.Equal("x", result.Rejection!.Id);
        Assert.Equal(BridgeErrorCodes.BadRequest, result.Rejection.Error!.Code);
    }

    [Fact]
    public void Parse_StringWrappedTwice_Rejects()
    {
        const string inner = """{"v":1,"kind":"request","id":"d","command":"app.hello"}""";
        var once = System.Text.Json.JsonSerializer.Serialize(inner);
        var twice = System.Text.Json.JsonSerializer.Serialize(once);

        var result = RequestParser.Parse(twice);

        Assert.False(result.IsAccepted);
    }

    [Fact]
    public void Parse_OversizedMessage_Rejects()
    {
        var message = new string(' ', RequestParser.MaxMessageChars + 1);

        var result = RequestParser.Parse(message);

        Assert.False(result.IsAccepted);
        Assert.Contains("exceeds", result.Rejection!.Error!.Message);
    }
}
