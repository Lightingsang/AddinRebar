namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;

/// <summary>
///     Abstraction for transaction boundaries to enable deterministic headless testing of rollback and commit semantics.
/// </summary>
public interface ITransactionContext : IDisposable
{
    void StartGroup(string label);
    void StartAutoTransaction(string label);
    bool CommitAutoTransaction();
    void RollbackAutoTransaction();
    void RollbackGroup();
    void AssimilateGroup();
    bool IsGroupStarted { get; }
    bool IsAutoTransactionStarted { get; }
    IReadOnlyList<string> Warnings { get; }
}
