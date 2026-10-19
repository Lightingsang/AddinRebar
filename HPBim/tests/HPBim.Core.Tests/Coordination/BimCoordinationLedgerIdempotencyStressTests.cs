namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class BimCoordinationLedgerIdempotencyStressTests
{
    private sealed class FakeExistenceQuery : IElementExistenceQuery
    {
        private readonly Dictionary<string, ElementState> _elements = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string uid, long id, string typeName, string levelName, bool exists = true) =>
            _elements[uid] = new ElementState(exists, id, uid, typeName, levelName);

        public ElementState? QueryElement(string uniqueId) =>
            _elements.TryGetValue(uniqueId, out var state) ? state : new ElementState(false, null, uniqueId, string.Empty, string.Empty);
    }

    [Fact]
    public void StressTest_BimCoordinationLedger_ImmutabilityPreservedAcrossModifications()
    {
        var ledgerV1 = new BimCoordinationLedger();
        var key1 = new CompositeSourceKey("Doc1", CoordinationCategory.Column, "C1");
        var mapping1 = LedgerElementMapping.Create(key1, "uid-1", 101, CoordinationCategory.Column, "C1", "h1");

        var ledgerV2 = ledgerV1.RegisterMapping(mapping1);
        Assert.NotSame(ledgerV1, ledgerV2);
        Assert.False(ledgerV1.TryGetTarget(key1, out _));
        Assert.True(ledgerV2.TryGetTarget(key1, out var uidV2));
        Assert.Equal("uid-1", uidV2);

        var ledgerV3 = ledgerV2.UpdateMapping(key1, "uid-1-updated", "h2", ElementExecutionStatus.Updated, 102);
        Assert.NotSame(ledgerV2, ledgerV3);
        ledgerV2.TryGetTarget(key1, out var uidFromV2);
        Assert.Equal("uid-1", uidFromV2);
        ledgerV3.TryGetTarget(key1, out var uidFromV3);
        Assert.Equal("uid-1-updated", uidFromV3);

        var ledgerV4 = ledgerV3.RemoveMapping(key1);
        Assert.NotSame(ledgerV3, ledgerV4);
        Assert.True(ledgerV3.TryGetTarget(key1, out _));
        Assert.False(ledgerV4.TryGetTarget(key1, out _));
    }

    [Fact]
    public void StressTest_BimCoordinationLedger_CaseInsensitiveLookups()
    {
        var ledger = new BimCoordinationLedger();
        var keyUpper = new CompositeSourceKey("PROJECT_DOC", CoordinationCategory.Column, "COL_01");
        ledger = ledger.UpdateMapping(keyUpper, "uid-col-1", "hash-1", ElementExecutionStatus.Created, 501);

        var keyLower = new CompositeSourceKey("project_doc", CoordinationCategory.Column, "col_01");
        Assert.True(ledger.TryGetTarget(keyLower, out var uid1));
        Assert.Equal("uid-col-1", uid1);

        var keyMixed = new CompositeSourceKey("Project_Doc", CoordinationCategory.Column, "Col_01");
        Assert.True(ledger.TryGetMapping(keyMixed, out var mapping));
        Assert.NotNull(mapping);
        Assert.Equal(501, mapping?.TargetElementId);
    }

    [Fact]
    public void StressTest_SyncFromExecutionRecords_BulkSyncWithFailedIsolation()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("StressSyncProject");

        var records = new List<ElementExecutionRecord>(50);
        for (int i = 1; i <= 30; i++)
        {
            var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, $"C{i}");
            records.Add(new ElementExecutionRecord
            {
                SourceKey = key,
                Category = CoordinationCategory.Column,
                Mark = $"C{i}",
                Status = ElementExecutionStatus.Created,
                TargetUniqueId = $"uid-c{i}",
                TargetElementId = 1000 + i
            });
        }

        // Add 5 Failed records
        for (int i = 31; i <= 35; i++)
        {
            var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, $"C{i}");
            records.Add(new ElementExecutionRecord
            {
                SourceKey = key,
                Category = CoordinationCategory.Column,
                Mark = $"C{i}",
                Status = ElementExecutionStatus.Failed,
                ErrorReason = "Simulated creation error"
            });
        }

        service.SyncFromExecutionRecords(records, "hash-sync-1");

        // The 30 created items must exist in ledger
        for (int i = 1; i <= 30; i++)
        {
            var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, $"C{i}");
            Assert.True(service.TryGetTarget(key, out var uid));
            Assert.Equal($"uid-c{i}", uid);
        }

        // The 5 failed items must NOT exist in ledger
        for (int i = 31; i <= 35; i++)
        {
            var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, $"C{i}");
            Assert.False(service.TryGetTarget(key, out _));
        }
    }

    [Fact]
    public void StressTest_IdempotentElementResolver_Full4PathMatrix()
    {
        var ledger = new BimCoordinationLedger();
        var query = new FakeExistenceQuery();

        // 1. Not in ledger -> Create
        var item1 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1"
        };
        var dec1 = IdempotentElementResolver.Resolve(item1, ledger, query);
        Assert.Equal(PlannedAction.Create, dec1.Action);

        // 2. In ledger, exists in host, identical -> NoOp
        var key2 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C2");
        ledger = ledger.UpdateMapping(key2, "uid-c2", "h1", ElementExecutionStatus.Created, 202);
        query.Add("uid-c2", 202, "Col-220x220", "Level 1");
        var item2 = new ElementPlanItem
        {
            SourceKey = key2,
            Category = CoordinationCategory.Column,
            Mark = "C2",
            TargetLevelName = "Level 1",
            Resource = new ResourceResolutionSpec { TypeName = "Col-220x220" }
        };
        var dec2 = IdempotentElementResolver.Resolve(item2, ledger, query);
        Assert.Equal(PlannedAction.NoOp, dec2.Action);
        Assert.Equal("uid-c2", dec2.ExistingUniqueId);

        // 3. In ledger, exists in host, type modified -> Update
        var item3 = item2 with { Resource = new ResourceResolutionSpec { TypeName = "Col-300x300" } };
        var dec3 = IdempotentElementResolver.Resolve(item3, ledger, query);
        Assert.Equal(PlannedAction.Update, dec3.Action);

        // 4. In ledger, deleted in host -> Recreate
        var key4 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C4");
        ledger = ledger.UpdateMapping(key4, "uid-c4", "h1", ElementExecutionStatus.Created, 204);
        var item4 = new ElementPlanItem { SourceKey = key4, Category = CoordinationCategory.Column, Mark = "C4" };
        var dec4 = IdempotentElementResolver.Resolve(item4, ledger, query);
        Assert.Equal(PlannedAction.Recreate, dec4.Action);
        Assert.Equal("uid-c4", dec4.ExistingUniqueId);
    }

    [Fact]
    public void StressTest_IdempotentElementResolver_LevelChangeTriggersUpdate()
    {
        var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C5");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-c5", "h1", ElementExecutionStatus.Created, 205);
        var query = new FakeExistenceQuery();
        query.Add("uid-c5", 205, "Col-220x220", "Level 1");

        var itemChangedLevel = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = "C5",
            TargetLevelName = "Level 2",
            Resource = new ResourceResolutionSpec { TypeName = "Col-220x220" }
        };

        var decision = IdempotentElementResolver.Resolve(itemChangedLevel, ledger, query);
        Assert.Equal(PlannedAction.Update, decision.Action);
    }

    [Fact]
    public void StressTest_IdempotentElementResolver_NullGuardAssertions()
    {
        var ledger = new BimCoordinationLedger();
        var query = new FakeExistenceQuery();
        var item = new ElementPlanItem();

        Assert.Throws<ArgumentNullException>(() => IdempotentElementResolver.Resolve(null!, ledger, query));
        Assert.Throws<ArgumentNullException>(() => IdempotentElementResolver.Resolve(item, null!, query));
        Assert.Throws<ArgumentNullException>(() => IdempotentElementResolver.Resolve(item, ledger, null!));
    }
}
