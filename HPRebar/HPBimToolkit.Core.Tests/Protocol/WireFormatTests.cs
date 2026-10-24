using System.Text.Json;
using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Protocol;
using Xunit;

namespace HPBimToolkit.Core.Tests.Protocol;

/// <summary>Pins the JSON the page reads: field names, enum spelling and nulls must match the frontend's protocol.ts.</summary>
public sealed class WireFormatTests
{
    [Fact]
    public void Serialize_SuccessResponse_UsesV1CamelCaseShape()
    {
        var response = ResponseEnvelope.Success("r9", new ShowElementsResult(3, 2));

        var json = JsonDocument.Parse(BridgeJson.Serialize(response)).RootElement;

        Assert.Equal(1, json.GetProperty("v").GetInt32());
        Assert.Equal("response", json.GetProperty("kind").GetString());
        Assert.Equal("r9", json.GetProperty("id").GetString());
        Assert.True(json.GetProperty("ok").GetBoolean());
        Assert.Equal(3, json.GetProperty("payload").GetProperty("shown").GetInt32());
        Assert.Equal(2, json.GetProperty("payload").GetProperty("zoomed").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("error").ValueKind);
    }

    [Fact]
    public void Serialize_FailureResponse_CarriesCodeAndMessage()
    {
        var response = ResponseEnvelope.Failure("r2", new BridgeError(BridgeErrorCodes.NoDocument, "No project is open."));

        var error = JsonDocument.Parse(BridgeJson.Serialize(response)).RootElement.GetProperty("error");

        Assert.Equal("NO_DOCUMENT", error.GetProperty("code").GetString());
        Assert.Equal("No project is open.", error.GetProperty("message").GetString());
    }

    [Fact]
    public void Serialize_Statistics_WritesEnumsCamelCaseAndKeepsNullWorksets()
    {
        var statistics = new ModelStatistics(@"D:\p\a.rvt", 31245, StatisticsScope.ActiveView, 5,
            [new CategoryCount(-2000011, "Walls", 5)], new ModelMetrics(1, 2, 3, 4, 5, 6, 7, null), 12);

        var json = JsonDocument.Parse(BridgeJson.Serialize(statistics)).RootElement;

        Assert.Equal(@"D:\p\a.rvt", json.GetProperty("documentKey").GetString());
        Assert.Equal(31245, json.GetProperty("viewId").GetInt64());
        Assert.Equal("activeView", json.GetProperty("scope").GetString());
        Assert.Equal(-2000011, json.GetProperty("categories")[0].GetProperty("categoryId").GetInt64());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("metrics").GetProperty("worksets").ValueKind);
        Assert.Equal(12, json.GetProperty("elapsedMs").GetInt64());
    }

    [Fact]
    public void Serialize_EventEnvelope_UsesEventField()
    {
        var json = JsonDocument.Parse(BridgeJson.Serialize(new EventEnvelope(EventNames.SelectionChanged, new SelectionChangedEvent(4))))
            .RootElement;

        Assert.Equal("event", json.GetProperty("kind").GetString());
        Assert.Equal("selection.changed", json.GetProperty("event").GetString());
        Assert.Equal(4, json.GetProperty("payload").GetProperty("count").GetInt32());
    }

    [Fact]
    public void Serialize_NoDocument_KeepsNullFields()
    {
        var json = JsonDocument.Parse(BridgeJson.Serialize(DocumentSnapshot.None)).RootElement;

        Assert.False(json.GetProperty("hasDocument").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("activeView").ValueKind);
    }
}
