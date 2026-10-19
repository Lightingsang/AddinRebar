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

public sealed class StrictBatchRollbackTests
{
    [Fact]
    public void BimPlanRunnerScriptTemplate_GeneratedScript_ContainsBatchRollbackGuard()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("bool hasBatchErrors = records.Any(r => (string)r[\"status\"] == \"Failed\");", script);
        Assert.Contains("!hasBatchErrors", script);
        Assert.Contains("[BATCH_TRANSACTION_FAILED]", script);
        Assert.Contains("||PAYLOAD:", script);
    }

    [Fact]
    public async Task ExecutePlanAsync_WhenBatchFails_RollsBackAllItemsAndPreservesDiagnostics()
    {
        var items = new List<ElementPlanItem>();
        for (int i = 1; i <= 10; i++)
        {
            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, $"C{i}"),
                Category = CoordinationCategory.Column,
                Mark = $"C{i}",
                SequenceOrder = i
            });
        }

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-hash-batch-fail",
            Items = items,
            Options = new ExecutionOptions { DryRun = false }
        };

        // Simulate 9 items Created and 1 item Failed, serialized in ||PAYLOAD:
        var simulatedRecords = new List<object>();
        for (int i = 1; i <= 9; i++)
        {
            simulatedRecords.Add(new
            {
                sourceKey = $"[Doc]#[Column]:C{i}",
                category = "Column",
                mark = $"C{i}",
                status = "Created",
                targetUniqueId = $"uid-{i}",
                targetElementId = 1000L + i
            });
        }
        simulatedRecords.Add(new
        {
            sourceKey = "[Doc]#[Column]:C10",
            category = "Column",
            mark = "C10",
            status = "Failed",
            errorCode = "ERR_SYMBOL_NOT_FOUND",
            errorReason = "Column symbol 'Concrete 220x220' not found."
        });

        string payloadJson = JsonSerializer.Serialize(simulatedRecords);
        string errorMessage = $"[BATCH_TRANSACTION_FAILED] Execution failed for 1 element(s). First failure: [ERR_SYMBOL_NOT_FOUND] Column symbol 'Concrete 220x220' not found. ||PAYLOAD:{payloadJson}";

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = true,
                RolledBack = true,
                Message = errorMessage,
                Value = null
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        // Strict all-or-nothing batch rollback verified:
        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(10, result.ElementRecords.Count);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(10, result.FailedCount);

        // The specific failed item maintains its original error code
        var item10Record = result.ElementRecords[9];
        Assert.Equal("C10", item10Record.Mark);
        Assert.Equal(ElementExecutionStatus.Failed, item10Record.Status);
        Assert.Equal("ERR_SYMBOL_NOT_FOUND", item10Record.ErrorCode);
        Assert.Contains("not found", item10Record.ErrorReason);

        // The other 9 items that had succeeded in subtransaction are marked Failed due to batch rollback
        for (int i = 0; i < 9; i++)
        {
            var rec = result.ElementRecords[i];
            Assert.Equal(ElementExecutionStatus.Failed, rec.Status);
            Assert.Equal("ERR_BATCH_ROLLED_BACK", rec.ErrorCode);
            Assert.Contains("Transaction rolled back", rec.ErrorReason);
        }

        // Formatted error message should be clean without payload suffix
        Assert.DoesNotContain("||PAYLOAD:", result.ErrorMessage);
        Assert.Contains("[BATCH_TRANSACTION_FAILED]", result.ErrorMessage);
    }
}
