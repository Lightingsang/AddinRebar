namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPGrasshopper.McpBridge.Support;

/// <summary>
///     Production transaction context wrapping native Revit TransactionGroup and Transaction.
/// </summary>
internal sealed class RevitTransactionContext : ITransactionContext
{
    private readonly Document _document;
    private TransactionGroup? _group;
    private Transaction? _autoTx;
    private AutoDismissFailurePreprocessor? _failurePreprocessor;

    public RevitTransactionContext(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public bool IsGroupStarted => _group?.HasStarted() == true && !_group.HasEnded();
    public bool IsAutoTransactionStarted => _autoTx?.HasStarted() == true && !_autoTx.HasEnded();
    public IReadOnlyList<string> Warnings => _failurePreprocessor?.Warnings ?? Array.Empty<string>();

    public void StartGroup(string label)
    {
        _group = new TransactionGroup(_document, label);
        _group.Start();
    }

    public void StartAutoTransaction(string label)
    {
        _autoTx = new Transaction(_document, label);
        _failurePreprocessor = AutoDismissFailurePreprocessor.ApplyTo(_autoTx);
        _autoTx.Start();
    }

    public bool CommitAutoTransaction()
    {
        return _autoTx == null || _autoTx.Commit() == TransactionStatus.Committed;
    }

    public void RollbackAutoTransaction()
    {
        if (IsAutoTransactionStarted) _autoTx?.RollBack();
    }

    public void RollbackGroup()
    {
        if (IsGroupStarted) _group?.RollBack();
    }

    public void AssimilateGroup()
    {
        _group?.Assimilate();
    }

    public void Dispose()
    {
        _autoTx?.Dispose();
        _group?.Dispose();
    }
}
