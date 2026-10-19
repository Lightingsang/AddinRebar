namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Collections.Generic;

/// <summary>
/// Thread-safe transaction and element mutation ledger for Grasshopper E2E test executions.
/// </summary>
public sealed class GrasshopperStateLedger
{
    private readonly List<long> _committedElements = new();
    private readonly Stack<List<long>> _transactionFrames = new();

    public int CommittedCount => _committedElements.Count;
    public IReadOnlyList<long> CommittedElements => _committedElements;

    public void BeginTransaction() => _transactionFrames.Push(new List<long>());

    public long CreateElement()
    {
        long newId = 200000 + _committedElements.Count + (_transactionFrames.Count > 0 ? _transactionFrames.Peek().Count : 0) + 1;
        if (_transactionFrames.Count > 0)
        {
            _transactionFrames.Peek().Add(newId);
        }
        else
        {
            _committedElements.Add(newId);
        }
        return newId;
    }

    public IReadOnlyList<long> CommitTransaction()
    {
        if (_transactionFrames.Count == 0) return Array.Empty<long>();
        var frame = _transactionFrames.Pop();
        _committedElements.AddRange(frame);
        return frame;
    }

    public IReadOnlyList<long> RollbackTransaction()
    {
        if (_transactionFrames.Count == 0) return Array.Empty<long>();
        return _transactionFrames.Pop();
    }
}
