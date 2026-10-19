namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class EmpiricalChallengerBatchRollbackTests
{
    private static BimExecutionPlan CreateBatchOf10Plan()
    {
        var items = Enumerable.Range(1, 10).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, $"C_{i:D2}"),
            Category = CoordinationCategory.Column,
            Mark = $"C_{i:D2}",
            SequenceOrder = i
        }).ToList();

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-empirical-rollback-10",
            Items = items,
            Options = new ExecutionOptions { DryRun = false }
        };
    }

    private static FakeRevitPipeClient CreateRollbackClient(string errorMsg) => new()
    {
        CustomExecuteHandler = _ => new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = errorMsg,
            Value = null
        }
    };

    private static string FormatBatchErrorMsg(string errCode, string errReason, IEnumerable<object> records)
    {
        string payload = JsonSerializer.Serialize(records);
        return $"[BATCH_TRANSACTION_FAILED] Execution failed for 1 element(s). First failure: [{errCode}] {errReason} ||PAYLOAD:{payload}";
    }

    private static void AssertAllFailed(BimPipelineResult result, BimCoordinationLedgerService ledger)
    {
        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(10, result.ElementRecords.Count);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(10, result.FailedCount);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task BatchOf10_Item1Fails_EntireBatchRollsBack_NoReceipt_AllMarkedFailed()
    {
        var plan = CreateBatchOf10Plan();
        var records = new List<object>
        {
            new { sourceKey = "[DocA]#[Column]:C_01", category = "Column", mark = "C_01", status = "Failed", errorCode = "ERR_LEVEL_NOT_FOUND", errorReason = "BaseLevel 'L1' missing." }
        };

        var ledger = new BimCoordinationLedgerService();
        var client = CreateRollbackClient(FormatBatchErrorMsg("ERR_LEVEL_NOT_FOUND", "BaseLevel 'L1' missing.", records));
        var engine = new BimPipelineEngine(client) { LedgerService = ledger };
        var result = await engine.ExecutePlanAsync(plan);

        AssertAllFailed(result, ledger);
        Assert.Equal("ERR_LEVEL_NOT_FOUND", result.ElementRecords[0].ErrorCode);
        for (int i = 1; i < 10; i++)
        {
            Assert.Equal("ERR_BATCH_ROLLED_BACK", result.ElementRecords[i].ErrorCode);
        }
    }

    [Fact]
    public async Task BatchOf10_MiddleItemFails_PrecedingItemsRolledBack_NotMarkedCreated()
    {
        var plan = CreateBatchOf10Plan();
        var records = Enumerable.Range(1, 4).Select(i => (object)new
        {
            sourceKey = $"[DocA]#[Column]:C_{i:D2}", category = "Column", mark = $"C_{i:D2}", status = "Created", targetUniqueId = $"uid-{i}", targetElementId = 2000L + i
        }).ToList();
        records.Add(new { sourceKey = "[DocA]#[Column]:C_05", category = "Column", mark = "C_05", status = "Failed", errorCode = "ERR_SYMBOL_NOT_FOUND", errorReason = "Col not found." });

        var ledger = new BimCoordinationLedgerService();
        var client = CreateRollbackClient(FormatBatchErrorMsg("ERR_SYMBOL_NOT_FOUND", "Col not found.", records));
        var engine = new BimPipelineEngine(client) { LedgerService = ledger };
        var result = await engine.ExecutePlanAsync(plan);

        AssertAllFailed(result, ledger);
        for (int i = 0; i < 4; i++) Assert.Equal("ERR_BATCH_ROLLED_BACK", result.ElementRecords[i].ErrorCode);
        Assert.Equal("ERR_SYMBOL_NOT_FOUND", result.ElementRecords[4].ErrorCode);
        for (int i = 5; i < 10; i++) Assert.Equal("ERR_BATCH_ROLLED_BACK", result.ElementRecords[i].ErrorCode);
    }

    [Fact]
    public async Task BatchOf10_LastItemFails_AllPrior9ItemsRolledBack()
    {
        var plan = CreateBatchOf10Plan();
        var records = Enumerable.Range(1, 9).Select(i => (object)new
        {
            sourceKey = $"[DocA]#[Column]:C_{i:D2}", category = "Column", mark = $"C_{i:D2}", status = "Created", targetUniqueId = $"uid-{i}", targetElementId = 3000L + i
        }).ToList();
        records.Add(new { sourceKey = "[DocA]#[Column]:C_10", category = "Column", mark = "C_10", status = "Failed", errorCode = "ERR_REVIT_API_EXCEPTION", errorReason = "Non-planar curve." });

        var ledger = new BimCoordinationLedgerService();
        var client = CreateRollbackClient(FormatBatchErrorMsg("ERR_REVIT_API_EXCEPTION", "Non-planar curve.", records));
        var engine = new BimPipelineEngine(client) { LedgerService = ledger };
        var result = await engine.ExecutePlanAsync(plan);

        AssertAllFailed(result, ledger);
        for (int i = 0; i < 9; i++) Assert.Equal("ERR_BATCH_ROLLED_BACK", result.ElementRecords[i].ErrorCode);
        Assert.Equal("ERR_REVIT_API_EXCEPTION", result.ElementRecords[9].ErrorCode);
        Assert.Contains("Non-planar", result.ElementRecords[9].ErrorReason);
    }

    [Fact]
    public void ReceiptSection_GuardedByZeroErrors_AndThrowsBatchTransactionFailed()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("bool hasBatchErrors = records.Any(r => (string)r[\"status\"] == \"Failed\");", script);
        Assert.Contains("if (!dryRun && !string.IsNullOrEmpty(planHash))", script);
        Assert.Contains("if (!hasBatchErrors)", script);
        Assert.Contains("receiptDs.SetEntity(entity);", script);
        Assert.Contains("if (hasBatchErrors)", script);
        Assert.Contains("throw new InvalidOperationException($\"[BATCH_TRANSACTION_FAILED]", script);
        Assert.Contains("||PAYLOAD:{payloadJson}\");", script);
    }

    [Fact]
    public async Task GenerateCommand_WhenBatchFails_RollsBackAndReturnsExitCode1()
    {
        string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"batch_fail_cli_{Guid.NewGuid():N}.json");
        try
        {
            var model = HPBim.Core.Tests.Fixtures.NhaPho5x18mFixture.CreateModel();
            await HPBim.Core.Serialization.BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var errorMsg = "[BATCH_TRANSACTION_FAILED] Execution failed for 1 element(s). First failure: [ERR_SYMBOL_NOT_FOUND] Missing ||PAYLOAD:[]";
            var client = CreateRollbackClient(errorMsg);
            using var sw = new System.IO.StringWriter();
            var cmd = new HPBim.Cli.Commands.GenerateCommand(new HPBim.Cli.Formatting.ConsoleFormatter(sw), client);
            var cliArgs = HPBim.Cli.CommandLine.CliParser.Parse(["generate", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(cliArgs);

            Assert.Equal(1, exitCode);
            string output = sw.ToString();
            Assert.Contains("DỰNG HÌNH THẤT BẠI", output);
        }
        finally
        {
            if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
        }
    }
}
