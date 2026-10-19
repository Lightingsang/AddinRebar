namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class AdversarialReceiptReaderTests
{
    [Fact]
    public void ParseReceipts_ValidMultipleReceipts_ExtractsAllFieldsAccurately()
    {
        var op1 = Guid.NewGuid();
        var op2 = Guid.NewGuid();
        string rawJson = $$"""
        {
            "count": 2,
            "receipts": [
                {
                    "operationId": "{{op1}}",
                    "planHash": "hash-alpha",
                    "committedAtUtc": "2026-10-07T21:00:00Z",
                    "elementMapJson": "{\"Doc#Column:C1\":\"uid-1\",\"Doc#Column:C2\":\"uid-2\"}"
                },
                {
                    "operationId": "{{op2}}",
                    "planHash": "hash-beta",
                    "committedAtUtc": "2026-10-07T22:00:00Z",
                    "elementMapJson": "{}"
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(rawJson);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);

        Assert.Equal(2, receipts.Count);
        Assert.Equal(op1, receipts[0].OperationId);
        Assert.Equal("hash-alpha", receipts[0].PlanHash);
        var map0 = receipts[0].ElementMap;
        Assert.NotNull(map0);
        Assert.Equal(2, map0.Count);
        Assert.Equal("uid-1", map0["Doc#Column:C1"]);

        Assert.Equal(op2, receipts[1].OperationId);
        Assert.Equal("hash-beta", receipts[1].PlanHash);
        var map1 = receipts[1].ElementMap;
        Assert.NotNull(map1);
        Assert.Empty(map1);
    }

    [Fact]
    public void ParseReceipts_EmptyOrMissingReceiptsProperty_ReturnsEmptyList()
    {
        string noReceipts = """{ "count": 0 }""";
        using var doc1 = JsonDocument.Parse(noReceipts);
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(doc1.RootElement));

        string emptyArray = """{ "count": 0, "receipts": [] }""";
        using var doc2 = JsonDocument.Parse(emptyArray);
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(doc2.RootElement));

        string nullReceipts = """{ "receipts": null }""";
        using var doc3 = JsonDocument.Parse(nullReceipts);
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(doc3.RootElement));

        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(null));
    }

    [Fact]
    public void ParseReceipts_MalformedArrayElements_GracefullyDefaultsWithoutThrowing()
    {
        string raw = """
        {
            "count": 3,
            "receipts": [
                { "operationId": "not-a-valid-guid", "planHash": "p-bad" },
                { "elementMapJson": "{ invalid json " },
                {}
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);

        Assert.Equal(3, receipts.Count);
        Assert.Equal(Guid.Empty, receipts[0].OperationId);
        Assert.Equal("p-bad", receipts[0].PlanHash);
        Assert.Null(receipts[1].ElementMap);
        Assert.Equal(Guid.Empty, receipts[2].OperationId);
    }

    [Fact]
    public void ParseReceipts_NonObjectArrayElements_GracefullyIgnoredOrHandled()
    {
        string raw = """
        {
            "receipts": [
                123,
                "some-string"
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
        Assert.Empty(receipts);
    }

    [Fact]
    public void ParseReceipts_MixedObjectAndNonObjectElements_ExtractsOnlyValidObjects()
    {
        var op = Guid.NewGuid();
        string raw = $$"""
        {
            "receipts": [
                123,
                "some-string",
                {
                    "operationId": "{{op}}",
                    "planHash": "valid-hash",
                    "committedAtUtc": "2026-10-07T22:00:00Z",
                    "elementMapJson": "{}"
                },
                null,
                true
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
        Assert.Single(receipts);
        Assert.Equal(op, receipts[0].OperationId);
        Assert.Equal("valid-hash", receipts[0].PlanHash);
    }

    [Fact]
    public void ParseElementMap_CorruptedOrTruncatedJson_ReturnsNull()
    {
        Assert.Null(ExtensibleStorageReceiptReader.ParseElementMap("{ corrupt"));
        Assert.Null(ExtensibleStorageReceiptReader.ParseElementMap("   "));
        Assert.Null(ExtensibleStorageReceiptReader.ParseElementMap(string.Empty));
        var emptyMap = ExtensibleStorageReceiptReader.ParseElementMap("{}");
        Assert.NotNull(emptyMap);
        Assert.Empty(emptyMap);
        Assert.Null(ExtensibleStorageReceiptReader.ParseElementMap("12345"));
    }

    [Fact]
    public async Task FindReceiptByOperationId_WhenNotFound_ReturnsNull()
    {
        var fakePipe = new FakeRevitPipeClient();
        var reader = new ExtensibleStorageReceiptReader(fakePipe);

        var result = await reader.FindReceiptByOperationIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task FindReceiptByPlanHash_WhenMultipleExist_ReturnsLatestByTimestamp()
    {
        var fakePipe = new FakeRevitPipeClient();
        string planHash = "target_hash";
        var r1 = new CoordinationReceiptRecord
        {
            OperationId = Guid.NewGuid(),
            PlanHash = planHash,
            CommittedAtUtc = "2026-10-07T08:00:00Z"
        };
        var r2 = new CoordinationReceiptRecord
        {
            OperationId = Guid.NewGuid(),
            PlanHash = planHash,
            CommittedAtUtc = "2026-10-07T14:30:00Z"
        };

        fakePipe.AddSimulatedReceipt(r1);
        fakePipe.AddSimulatedReceipt(r2);

        var reader = new ExtensibleStorageReceiptReader(fakePipe);
        var latest = await reader.FindReceiptByPlanHashAsync(planHash);

        Assert.NotNull(latest);
        Assert.Equal(r2.OperationId, latest.OperationId);
        Assert.Equal("2026-10-07T14:30:00Z", latest.CommittedAtUtc);
    }

    [Fact]
    public async Task QueryReceipts_WhenPipeExecutionFails_ThrowsRevitPipeException()
    {
        var fakePipe = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = true,
                Message = "Simulated Revit IPC failure while querying Extensible Storage"
            }
        };

        var reader = new ExtensibleStorageReceiptReader(fakePipe);

        var ex = await Assert.ThrowsAsync<RevitPipeException>(
            () => reader.FindReceiptByOperationIdAsync(Guid.NewGuid()));

        Assert.Contains("Simulated Revit IPC failure", ex.Message);
    }

    [Fact]
    public void ElementPlanItem_CompositeSourceKey_SerializesAsCanonicalString()
    {
        var key = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1");
        var item = new ElementPlanItem { SourceKey = key };

        var elem = BridgeJson.ToElement(item);
        Assert.NotNull(elem);
        var sk = elem.Value.GetProperty("sourceKey");
        Assert.Equal(JsonValueKind.String, sk.ValueKind);
        Assert.Equal("[Doc]#[Column]:C1", sk.GetString());
    }
}
