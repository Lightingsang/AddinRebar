namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using Xunit;

public sealed class ExtensibleStorageReceiptReaderTests
{
    [Fact]
    public async Task FindReceiptByOperationId_MatchesExpectedReceipt()
    {
        var fakePipe = new FakeRevitPipeClient();
        var opId = Guid.NewGuid();
        var receipt = new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash111",
            CommittedAtUtc = "2026-10-07T21:00:00Z",
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Column:C1"] = "uid-111",
                ["Doc#Wall:W1"] = "uid-222"
            }
        };
        fakePipe.AddSimulatedReceipt(receipt);

        var reader = new ExtensibleStorageReceiptReader(fakePipe);
        var found = await reader.FindReceiptByOperationIdAsync(opId);

        Assert.NotNull(found);
        Assert.Equal(opId, found.OperationId);
        Assert.Equal("hash111", found.PlanHash);
        var map = found.ElementMap;
        Assert.NotNull(map);
        Assert.Equal(2, map.Count);
        Assert.Equal("uid-111", map["Doc#Column:C1"]);
    }

    [Fact]
    public async Task FindReceiptByOperationId_ReturnsNullWhenNotFound()
    {
        var fakePipe = new FakeRevitPipeClient();
        var reader = new ExtensibleStorageReceiptReader(fakePipe);

        var found = await reader.FindReceiptByOperationIdAsync(Guid.NewGuid());
        Assert.Null(found);
    }

    [Fact]
    public async Task FindReceiptByPlanHash_ReturnsLatestMatchingReceipt()
    {
        var fakePipe = new FakeRevitPipeClient();
        string planHash = "common_plan_hash";

        var older = new CoordinationReceiptRecord
        {
            OperationId = Guid.NewGuid(),
            PlanHash = planHash,
            CommittedAtUtc = "2026-10-07T10:00:00Z"
        };
        var newer = new CoordinationReceiptRecord
        {
            OperationId = Guid.NewGuid(),
            PlanHash = planHash,
            CommittedAtUtc = "2026-10-07T12:00:00Z"
        };

        fakePipe.AddSimulatedReceipt(older);
        fakePipe.AddSimulatedReceipt(newer);

        var reader = new ExtensibleStorageReceiptReader(fakePipe);
        var found = await reader.FindReceiptByPlanHashAsync(planHash);

        Assert.NotNull(found);
        Assert.Equal(newer.OperationId, found.OperationId);
        Assert.Equal("2026-10-07T12:00:00Z", found.CommittedAtUtc);
    }

    [Fact]
    public async Task GetAllReceipts_ReturnsAllStoredReceipts()
    {
        var fakePipe = new FakeRevitPipeClient();
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord { OperationId = Guid.NewGuid(), PlanHash = "p1" });
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord { OperationId = Guid.NewGuid(), PlanHash = "p2" });

        var reader = new ExtensibleStorageReceiptReader(fakePipe);
        var all = await reader.GetAllReceiptsAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task FindReceiptAsync_FallsBackToPlanHash_WhenOpIdNotFound()
    {
        var fakePipe = new FakeRevitPipeClient();
        var storedOpId = Guid.NewGuid();
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord { OperationId = storedOpId, PlanHash = "matched_hash" });

        var reader = new ExtensibleStorageReceiptReader(fakePipe);
        var found = await reader.FindReceiptAsync(Guid.NewGuid(), "matched_hash");

        Assert.NotNull(found);
        Assert.Equal(storedOpId, found.OperationId);
    }

    [Fact]
    public void ParseReceipts_WithCorruptedElementMapJson_ReturnsNull()
    {
        string rawJson = """
        {
            "count": 1,
            "receipts": [
                {
                    "operationId": "D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90",
                    "planHash": "some_hash",
                    "committedAtUtc": "2026-10-07T21:00:00Z",
                    "elementMapJson": "{ corrupted json..."
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(rawJson);
        var records = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);

        Assert.Single(records);
        Assert.Null(records[0].ElementMap);
    }

    [Fact]
    public void ParseReceipts_WithNullOrEmptyJson_ReturnsEmptyList()
    {
        var recordsNull = ExtensibleStorageReceiptReader.ParseReceipts(null);
        Assert.Empty(recordsNull);

        var mapEmpty = ExtensibleStorageReceiptReader.ParseElementMap(string.Empty);
        Assert.Null(mapEmpty);
    }

    [Fact]
    public void BimPlanRunnerScriptTemplate_ContainsExtensibleStorageReceiptLogic()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90", script);
        Assert.Contains("HPBimCoordinationReceipt", script);
        Assert.Contains("DataStorage", script);
        Assert.Contains("ElementMapJson", script);
        Assert.Contains("if (!dryRun && !string.IsNullOrEmpty(planHash))", script);
    }

    [Fact]
    public void ExtensibleStorageQueryScriptTemplate_GeneratesValidQueryScript()
    {
        string script = ExtensibleStorageQueryScriptTemplate.GetQueryScript();

        Assert.NotNull(script);
        Assert.Contains("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90", script);
        Assert.Contains("FilteredElementCollector", script);
        Assert.Contains("DataStorage", script);
        Assert.Contains("targetOperationId", script);
        Assert.Contains("targetPlanHash", script);
        Assert.Contains("ElementMapJson", script);
        Assert.Contains("return new { count = receipts.Count, receipts = receipts };", script);
    }
}
