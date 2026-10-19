namespace HPBim.Core.Coordination;

using System;
using HPBim.Core.Execution;

public enum PlannedAction
{
    Create = 1,
    Update = 2,
    NoOp = 3,
    Recreate = 4
}

public sealed record ElementState(
    bool Exists,
    long? ElementId,
    string UniqueId,
    string TypeName,
    string LevelName);

public interface IElementExistenceQuery
{
    ElementState? QueryElement(string uniqueId);
}

public sealed record ElementResolutionDecision
{
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public PlannedAction Action { get; init; }
    public string? ExistingUniqueId { get; init; }
    public long? ExistingElementId { get; init; }
    public string Reason { get; init; } = string.Empty;

    public static ElementResolutionDecision Create(CompositeSourceKey key, string reason) => new()
    {
        SourceKey = key,
        Action = PlannedAction.Create,
        Reason = reason
    };

    public static ElementResolutionDecision Update(CompositeSourceKey key, string uniqueId, long? elemId, string reason) => new()
    {
        SourceKey = key,
        Action = PlannedAction.Update,
        ExistingUniqueId = uniqueId,
        ExistingElementId = elemId,
        Reason = reason
    };

    public static ElementResolutionDecision NoOp(CompositeSourceKey key, string uniqueId, long? elemId, string reason) => new()
    {
        SourceKey = key,
        Action = PlannedAction.NoOp,
        ExistingUniqueId = uniqueId,
        ExistingElementId = elemId,
        Reason = reason
    };

    public static ElementResolutionDecision Recreate(CompositeSourceKey key, string missingUniqueId, string reason) => new()
    {
        SourceKey = key,
        Action = PlannedAction.Recreate,
        ExistingUniqueId = missingUniqueId,
        Reason = reason
    };
}

public static class IdempotentElementResolver
{
    public static ElementResolutionDecision Resolve(
        ElementPlanItem planItem,
        BimCoordinationLedger ledger,
        IElementExistenceQuery existenceQuery)
    {
        if (planItem == null) throw new ArgumentNullException(nameof(planItem));
        if (ledger == null) throw new ArgumentNullException(nameof(ledger));
        if (existenceQuery == null) throw new ArgumentNullException(nameof(existenceQuery));

        if (!ledger.TryGetMapping(planItem.SourceKey, out var mapping) || string.IsNullOrEmpty(mapping?.TargetUniqueId))
        {
            return ElementResolutionDecision.Create(planItem.SourceKey, "Not present in coordination ledger.");
        }

        string targetUniqueId = mapping!.TargetUniqueId;
        var elementState = existenceQuery.QueryElement(targetUniqueId);
        if (elementState == null || !elementState.Exists)
        {
            return ElementResolutionDecision.Recreate(
                planItem.SourceKey,
                targetUniqueId,
                $"Element '{targetUniqueId}' found in ledger but missing from host document.");
        }

        if (CheckIfModified(planItem, elementState))
        {
            return ElementResolutionDecision.Update(
                planItem.SourceKey,
                targetUniqueId,
                elementState.ElementId,
                "Parameters or type modified compared to design plan.");
        }

        return ElementResolutionDecision.NoOp(
            planItem.SourceKey,
            targetUniqueId,
            elementState.ElementId,
            "Element exists and matches design specification identically.");
    }

    private static bool CheckIfModified(ElementPlanItem planItem, ElementState elementState)
    {
        if (!string.IsNullOrEmpty(planItem.Resource?.TypeName) &&
            !string.IsNullOrEmpty(elementState.TypeName) &&
            !string.Equals(planItem.Resource?.TypeName, elementState.TypeName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(planItem.TargetLevelName) &&
            !string.IsNullOrEmpty(elementState.LevelName) &&
            !string.Equals(planItem.TargetLevelName, elementState.LevelName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
