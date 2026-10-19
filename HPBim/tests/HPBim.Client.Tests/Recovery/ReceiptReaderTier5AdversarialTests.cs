namespace HPBim.Client.Tests.Recovery;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using Xunit;

public sealed class ReceiptReaderTier5AdversarialTests
{
    [Fact]
    public void ParseReceipts_NullOrNonObjectRoot_ReturnsEmptyList()
    {
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(null));

        using var docStr = JsonDocument.Parse("\"a-primitive-string\"");
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(docStr.RootElement));

        using var docNum = JsonDocument.Parse("12345");
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(docNum.RootElement));

        using var docArr = JsonDocument.Parse("[1, 2, 3]");
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(docArr.RootElement));
    }

    [Fact]
    public void ParseReceipts_ReceiptsArrayMissingOrNonArray_ReturnsEmptyList()
    {
        using var docEmpty = JsonDocument.Parse("{}");
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(docEmpty.RootElement));

        using var docNum = JsonDocument.Parse("{\"receipts\": 999}");
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(docNum.RootElement));

        using var docStr = JsonDocument.Parse("{\"receipts\": \"invalid-array-string\"}");
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(docStr.RootElement));

        using var docNull = JsonDocument.Parse("{\"receipts\": null}");
        Assert.Empty(ExtensibleStorageReceiptReader.ParseReceipts(docNull.RootElement));
    }

    [Fact]
    public void ParseReceipts_ArrayContainingNonObjects_GracefullySkipsNonObjects()
    {
        var validOpId = Guid.NewGuid();
        string raw = $$"""
        {
            "receipts": [
                null,
                123,
                "some-noise",
                true,
                [1, 2],
                {
                    "operationId": "{{validOpId}}",
                    "planHash": "hash-isolated",
                    "committedAtUtc": "2026-10-08T00:00:00Z",
                    "elementMapJson": "{}"
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);

        Assert.Single(receipts);
        Assert.Equal(validOpId, receipts[0].OperationId);
        Assert.Equal("hash-isolated", receipts[0].PlanHash);
    }

    [Fact]
    public void ParseReceipts_MissingOrCorruptedFields_DefaultsGracefully()
    {
        string raw = """
        {
            "receipts": [
                {},
                { "operationId": "not-a-valid-guid", "planHash": "bad-guid-hash" }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);

        Assert.Equal(2, receipts.Count);
        Assert.Equal(Guid.Empty, receipts[0].OperationId);
        Assert.Empty(receipts[0].PlanHash);
        Assert.Null(receipts[0].ElementMap);

        Assert.Equal(Guid.Empty, receipts[1].OperationId);
        Assert.Equal("bad-guid-hash", receipts[1].PlanHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"string-literal\"")]
    [InlineData("[1, 2, 3]")]
    [InlineData("12345")]
    [InlineData("{ malformed-unclosed-json")]
    public void ParseElementMap_EdgeCasesAndInvalidInputs_ReturnsNull(string? input)
    {
        var dict = ExtensibleStorageReceiptReader.ParseElementMap(input!);
        Assert.Null(dict);
    }

    [Fact]
    public void ParseElementMap_EmptyJsonObject_ReturnsEmptyDictionary()
    {
        var dict = ExtensibleStorageReceiptReader.ParseElementMap("{}");
        Assert.NotNull(dict);
        Assert.Empty(dict);
    }

    [Fact]
    public void ParseElementMap_ValidKeyValues_ParsesAllEntries()
    {
        string validJson = "{\"Doc#Col:C1\":\"uid-1\",\"Doc#Beam:B1\":\"uid-2\"}";
        var dict = ExtensibleStorageReceiptReader.ParseElementMap(validJson);

        Assert.NotNull(dict);
        Assert.Equal(2, dict.Count);
        Assert.Equal("uid-1", dict["Doc#Col:C1"]);
        Assert.Equal("uid-2", dict["Doc#Beam:B1"]);
    }

    [Fact]
    public async Task FindReceiptAsync_PrioritizesOperationIdOverPlanHash()
    {
        var fakePipe = new FakeRevitPipeClient();
        var opTarget = Guid.NewGuid();
        var opOther = Guid.NewGuid();

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opTarget,
            PlanHash = "plan-primary",
            CommittedAtUtc = "2026-10-08T01:00:00Z"
        });
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opOther,
            PlanHash = "plan-secondary",
            CommittedAtUtc = "2026-10-08T02:00:00Z"
        });

        var reader = new ExtensibleStorageReceiptReader(fakePipe);
        var receipt = await reader.FindReceiptAsync(opTarget, "plan-secondary");

        Assert.NotNull(receipt);
        Assert.Equal(opTarget, receipt.OperationId);
        Assert.Equal("plan-primary", receipt.PlanHash);
    }

    [Fact]
    public async Task FindReceiptAsync_FallsBackToPlanHash_WhenOperationIdMissing()
    {
        var fakePipe = new FakeRevitPipeClient();
        var opExisting = Guid.NewGuid();

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opExisting,
            PlanHash = "plan-fallback-target",
            CommittedAtUtc = "2026-10-08T03:00:00Z"
        });

        var reader = new ExtensibleStorageReceiptReader(fakePipe);
        var receipt = await reader.FindReceiptAsync(Guid.NewGuid(), "plan-fallback-target");

        Assert.NotNull(receipt);
        Assert.Equal(opExisting, receipt.OperationId);
        Assert.Equal("plan-fallback-target", receipt.PlanHash);
    }

    [Fact]
    public async Task FindReceiptAsync_WhenNeitherMatches_ReturnsNull()
    {
        var fakePipe = new FakeRevitPipeClient();
        var reader = new ExtensibleStorageReceiptReader(fakePipe);

        var receipt = await reader.FindReceiptAsync(Guid.NewGuid(), "non-existent-hash");
        Assert.Null(receipt);
    }

    [Fact]
    public async Task AutoConnect_WhenPipeClientDisconnected_ConnectsBeforeQuery()
    {
        var fakePipe = new FakeRevitPipeClient { IsConnected = false };
        var reader = new ExtensibleStorageReceiptReader(fakePipe);

        await reader.FindReceiptByOperationIdAsync(Guid.NewGuid());
        Assert.True(fakePipe.IsConnected);
    }
}
