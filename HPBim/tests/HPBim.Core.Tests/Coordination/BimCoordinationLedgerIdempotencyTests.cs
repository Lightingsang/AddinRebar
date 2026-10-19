namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class BimCoordinationLedgerIdempotencyTests
{
    private sealed class FakeExistenceQuery : IElementExistenceQuery
    {
        private readonly Dictionary<string, ElementState> _elements = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string uid, long id, string typeName, string levelName) =>
            _elements[uid] = new ElementState(true, id, uid, typeName, levelName);

        public ElementState? QueryElement(string uniqueId) =>
            _elements.TryGetValue(uniqueId, out var state) ? state : new ElementState(false, null, uniqueId, string.Empty, string.Empty);
    }

    [Fact]
    public void BimCoordinationLedger_RegisterAndRetrieveMapping_Success()
    {
        var ledger = new BimCoordinationLedger();
        var key = new CompositeSourceKey("DocA", CoordinationCategory.Column, "C1");
        var mapping = LedgerElementMapping.Create(key, "uid-col-1", 1001, CoordinationCategory.Column, "C1", "hash-v1");

        ledger = ledger.RegisterMapping(mapping);

        Assert.True(ledger.TryGetTarget(key, out var targetUid));
        Assert.Equal("uid-col-1", targetUid);
        Assert.True(ledger.TryGetMapping(key, out var retrieved));
        Assert.Equal(1001, retrieved?.TargetElementId);
    }

    [Fact]
    public void BimCoordinationLedger_UpdateAndRemoveMapping_UpdatesStateCleanly()
    {
        var ledger = new BimCoordinationLedger();
        var key = new CompositeSourceKey("DocA", CoordinationCategory.Beam, "B1");

        ledger = ledger.UpdateMapping(key, "uid-b1", "hash-1", ElementExecutionStatus.Created, 2001);
        Assert.True(ledger.TryGetTarget(key, out var uid));
        Assert.Equal("uid-b1", uid);

        ledger = ledger.UpdateMapping(key, "uid-b1-updated", "hash-2", ElementExecutionStatus.Updated, 2002);
        ledger.TryGetMapping(key, out var updated);
        Assert.Equal("uid-b1-updated", updated?.TargetUniqueId);
        Assert.Equal(ElementExecutionStatus.Updated, updated?.LastModifiedStatus);

        ledger = ledger.RemoveMapping(key);
        Assert.False(ledger.TryGetTarget(key, out _));
    }

    private static ElementExecutionRecord CreateRecord(
        CompositeSourceKey key,
        string mark,
        ElementExecutionStatus status,
        string uid,
        long elemId) =>
        new()
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = mark,
            Status = status,
            TargetUniqueId = uid,
            TargetElementId = elemId
        };

    [Fact]
    public void BimCoordinationLedgerService_SyncFromExecutionRecords_RegistersMappings()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("TestProject");
        var key1 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1");
        var key2 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C2");

        var records = new List<ElementExecutionRecord>
        {
            CreateRecord(key1, "C1", ElementExecutionStatus.Created, "uid-c1", 3001),
            CreateRecord(key2, "C2", ElementExecutionStatus.Created, "uid-c2", 3002)
        };

        service.SyncFromExecutionRecords(records, "plan-hash-1");

        Assert.True(service.TryGetTarget(key1, out var c1Uid));
        Assert.Equal("uid-c1", c1Uid);
        Assert.True(service.TryGetTarget(key2, out var c2Uid));
        Assert.Equal("uid-c2", c2Uid);
    }

    [Fact]
    public void BimCoordinationLedgerService_SyncFromExecutionRecords_UpdatesExistingWithNoOp()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("TestProject");
        var key1 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1");

        var initial = new[] { CreateRecord(key1, "C1", ElementExecutionStatus.Created, "uid-c1", 3001) };
        service.SyncFromExecutionRecords(initial, "plan-hash-1");

        var rerun = new[] { CreateRecord(key1, "C1", ElementExecutionStatus.NoOp, "uid-c1", 3001) };
        service.SyncFromExecutionRecords(rerun, "plan-hash-2");

        Assert.True(service.TryGetMapping(key1, out var mapping1));
        Assert.Equal(ElementExecutionStatus.NoOp, mapping1?.LastModifiedStatus);
    }

    [Fact]
    public void IdempotentElementResolver_WhenNotInLedger_ResolvesToCreate()
    {
        var ledger = new BimCoordinationLedger();
        var query = new FakeExistenceQuery();
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1"
        };

        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.Create, decision.Action);
    }

    [Fact]
    public void IdempotentElementResolver_WhenExistsAndIdentical_ResolvesToNoOp()
    {
        var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-c1", "h1", ElementExecutionStatus.Created, 5001);

        var query = new FakeExistenceQuery();
        query.Add("uid-c1", 5001, "Col-220x220", "Level 1");

        var item = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = "C1",
            TargetLevelName = "Level 1",
            Resource = new ResourceResolutionSpec { TypeName = "Col-220x220" }
        };

        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.NoOp, decision.Action);
        Assert.Equal("uid-c1", decision.ExistingUniqueId);
    }

    [Fact]
    public void IdempotentElementResolver_WhenTypeModified_ResolvesToUpdate()
    {
        var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-c1", "h1", ElementExecutionStatus.Created, 5001);

        var query = new FakeExistenceQuery();
        query.Add("uid-c1", 5001, "Col-220x220", "Level 1");

        var item = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = "C1",
            TargetLevelName = "Level 1",
            Resource = new ResourceResolutionSpec { TypeName = "Col-300x300" }
        };

        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.Update, decision.Action);
        Assert.Equal("uid-c1", decision.ExistingUniqueId);
    }

    [Fact]
    public void IdempotentElementResolver_WhenElementDeletedInHost_ResolvesToRecreate()
    {
        var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-c1", "h1", ElementExecutionStatus.Created, 5001);

        var query = new FakeExistenceQuery(); // Empty, element does not exist

        var item = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = "C1"
        };

        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.Recreate, decision.Action);
        Assert.Equal("uid-c1", decision.ExistingUniqueId);
    }
}
