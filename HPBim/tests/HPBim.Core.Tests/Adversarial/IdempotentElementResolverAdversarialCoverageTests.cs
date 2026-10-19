namespace HPBim.Core.Tests.Adversarial;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class IdempotentElementResolverAdversarialCoverageTests
{
    private sealed class DynamicExistenceQuery : IElementExistenceQuery
    {
        private readonly Dictionary<string, ElementState> _elements = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string uid, long id, string typeName, string levelName, bool exists = true) =>
            _elements[uid] = new ElementState(exists, id, uid, typeName, levelName);

        public void Remove(string uid) => _elements.Remove(uid);

        public ElementState? QueryElement(string uniqueId) =>
            _elements.TryGetValue(uniqueId, out var state) ? state : null;
    }

    [Fact]
    public void Resolve_TombstonedEmptyTargetUniqueId_ResolvesToCreate()
    {
        var key = new CompositeSourceKey("Doc1", CoordinationCategory.Column, "C1");
        var emptyUidMapping = new LedgerElementMapping
        {
            SourceKey = key,
            TargetUniqueId = string.Empty,
            Category = CoordinationCategory.Column
        };
        var ledger = new BimCoordinationLedger().RegisterMapping(emptyUidMapping);
        var query = new DynamicExistenceQuery();

        var item = new ElementPlanItem { SourceKey = key, Category = CoordinationCategory.Column, Mark = "C1" };
        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.Create, decision.Action);
    }

    [Fact]
    public void Resolve_ElementMissingInHostDocument_ResolvesToRecreate()
    {
        var key = new CompositeSourceKey("Doc1", CoordinationCategory.Column, "C2");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-missing-404", "h1", ElementExecutionStatus.Created, 101);
        var query = new DynamicExistenceQuery();
        query.Register("uid-missing-404", 101, "Col-220x220", "Level 1", exists: false);

        var item = new ElementPlanItem { SourceKey = key, Category = CoordinationCategory.Column, Mark = "C2" };
        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.Recreate, decision.Action);
        Assert.Equal("uid-missing-404", decision.ExistingUniqueId);
        Assert.Contains("missing from host document", decision.Reason);
    }

    [Fact]
    public void Resolve_QueryReturnsNull_ResolvesToRecreate()
    {
        var key = new CompositeSourceKey("Doc1", CoordinationCategory.Beam, "B1");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-b1-deleted", "h1", ElementExecutionStatus.Created, 201);
        var query = new DynamicExistenceQuery(); // Empty, returns null for all queries

        var item = new ElementPlanItem { SourceKey = key, Category = CoordinationCategory.Beam, Mark = "B1" };
        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.Recreate, decision.Action);
        Assert.Equal("uid-b1-deleted", decision.ExistingUniqueId);
    }

    [Fact]
    public void Resolve_MultiHandleProvenanceCollisions_ResolvesConsistently()
    {
        var keyWithProv1 = new CompositeSourceKey("Doc1", CoordinationCategory.Column, "C3", new[] { "P1", "P2" });
        var keyWithProv2 = new CompositeSourceKey("Doc1", CoordinationCategory.Column, "C3", new[] { "P3", "P4" });

        var ledger = new BimCoordinationLedger().UpdateMapping(keyWithProv1, "uid-c3", "h1", ElementExecutionStatus.Created, 301);
        var query = new DynamicExistenceQuery();
        query.Register("uid-c3", 301, "Col-220x220", "Level 1");

        var item1 = new ElementPlanItem
        {
            SourceKey = keyWithProv1,
            Category = CoordinationCategory.Column,
            Mark = "C3",
            TargetLevelName = "Level 1",
            Resource = new ResourceResolutionSpec { TypeName = "Col-220x220" }
        };

        var item2 = new ElementPlanItem
        {
            SourceKey = keyWithProv2,
            Category = CoordinationCategory.Column,
            Mark = "C3",
            TargetLevelName = "Level 1",
            Resource = new ResourceResolutionSpec { TypeName = "Col-220x220" }
        };

        var decision1 = IdempotentElementResolver.Resolve(item1, ledger, query);
        var decision2 = IdempotentElementResolver.Resolve(item2, ledger, query);

        Assert.Equal(PlannedAction.NoOp, decision1.Action);
        Assert.Equal(PlannedAction.NoOp, decision2.Action);
        Assert.Equal(decision1.ExistingUniqueId, decision2.ExistingUniqueId);
    }

    [Fact]
    public void Resolve_CaseInsensitiveTypeNameAndLevel_ResolvesToNoOpWithoutUpdate()
    {
        var key = new CompositeSourceKey("Doc1", CoordinationCategory.Wall, "W1");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-w1", "h1", ElementExecutionStatus.Created, 401);
        var query = new DynamicExistenceQuery();
        query.Register("uid-w1", 401, "wall-220-brick", "level 1");

        var item = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Wall,
            Mark = "W1",
            TargetLevelName = "LEVEL 1", // Casing difference
            Resource = new ResourceResolutionSpec { TypeName = "WALL-220-BRICK" } // Casing difference
        };

        var decision = IdempotentElementResolver.Resolve(item, ledger, query);

        Assert.Equal(PlannedAction.NoOp, decision.Action);
        Assert.Equal("uid-w1", decision.ExistingUniqueId);
        Assert.Equal(401, decision.ExistingElementId);
    }

    [Fact]
    public void Resolve_MissingResourceOrTypeName_GracefulNoOp()
    {
        var key = new CompositeSourceKey("Doc1", CoordinationCategory.Slab, "S1");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-s1", "h1", ElementExecutionStatus.Created, 501);
        var query = new DynamicExistenceQuery();
        query.Register("uid-s1", 501, "Slab-150mm", "Level 1");

        var itemNoResource = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Slab,
            Mark = "S1",
            TargetLevelName = "Level 1",
            Resource = null!
        };

        var decision = IdempotentElementResolver.Resolve(itemNoResource, ledger, query);
        Assert.Equal(PlannedAction.NoOp, decision.Action);
    }

    [Fact]
    public void Resolve_FullRecreateToNoOpLifecycle_EndToEndSimulation()
    {
        var key = new CompositeSourceKey("Doc1", CoordinationCategory.Column, "C5");
        var ledger = new BimCoordinationLedger().UpdateMapping(key, "uid-c5-old", "hash-1", ElementExecutionStatus.Created, 601);
        var query = new DynamicExistenceQuery(); // host has nothing for old uid

        var item = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = "C5",
            TargetLevelName = "Level 1",
            Resource = new ResourceResolutionSpec { TypeName = "Col-220x220" }
        };

        // Round 1: Element missing in host -> Recreate
        var dec1 = IdempotentElementResolver.Resolve(item, ledger, query);
        Assert.Equal(PlannedAction.Recreate, dec1.Action);

        // Simulation: Recreation commits uid-c5-new
        string newUid = "uid-c5-new";
        long newElemId = 602;
        ledger = ledger.UpdateMapping(key, newUid, "hash-2", ElementExecutionStatus.Created, newElemId);
        query.Register(newUid, newElemId, "Col-220x220", "Level 1");

        // Round 2: Now matches document -> NoOp
        var dec2 = IdempotentElementResolver.Resolve(item, ledger, query);
        Assert.Equal(PlannedAction.NoOp, dec2.Action);
        Assert.Equal(newUid, dec2.ExistingUniqueId);
        Assert.Equal(newElemId, dec2.ExistingElementId);
    }
}
