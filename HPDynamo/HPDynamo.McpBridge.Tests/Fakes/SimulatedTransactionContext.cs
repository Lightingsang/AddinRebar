namespace HPDynamo.McpBridge.Tests.Fakes;

using System;
using System.Collections.Generic;
using HPDynamo.McpBridge.Execution;

public sealed class SimulatedTransactionContext : ITransactionContext
{
    public bool IsGroupStarted { get; private set; }
    public bool IsAutoTransactionStarted { get; private set; }
    public List<string> WarningsList { get; } = new();
    public IReadOnlyList<string> Warnings => WarningsList;

    public int GroupStartCount { get; private set; }
    public int AutoTxStartCount { get; private set; }
    public int CommitAutoTxCount { get; private set; }
    public int RollbackAutoTxCount { get; private set; }
    public int RollbackGroupCount { get; private set; }
    public int AssimilateGroupCount { get; private set; }

    public string? LastGroupLabel { get; private set; }
    public string? LastAutoTxLabel { get; private set; }
    public bool AutoTransactionCommitResult { get; set; } = true;
    public bool ThrowOnRollbackAutoTransaction { get; set; }

    public void StartGroup(string label)
    {
        GroupStartCount++;
        LastGroupLabel = label;
        IsGroupStarted = true;
    }

    public void StartAutoTransaction(string label)
    {
        AutoTxStartCount++;
        LastAutoTxLabel = label;
        IsAutoTransactionStarted = true;
    }

    public bool CommitAutoTransaction()
    {
        CommitAutoTxCount++;
        IsAutoTransactionStarted = false;
        return AutoTransactionCommitResult;
    }

    public void RollbackAutoTransaction()
    {
        if (ThrowOnRollbackAutoTransaction)
            throw new InvalidOperationException("Simulated auto-transaction rollback failure.");

        RollbackAutoTxCount++;
        IsAutoTransactionStarted = false;
    }

    public void RollbackGroup()
    {
        RollbackGroupCount++;
        IsGroupStarted = false;
    }

    public void AssimilateGroup()
    {
        AssimilateGroupCount++;
        IsGroupStarted = false;
    }

    public void Dispose()
    {
        IsAutoTransactionStarted = false;
        IsGroupStarted = false;
    }
}
