using System;
using System.Linq;
using Microsoft.AnalysisServices.Tabular;
using Serilog;

namespace HPPowerBi.McpBridge.Tabular;

public sealed record RelationshipMutationResult(
    string FromTable,
    string FromColumn,
    string ToTable,
    string ToColumn,
    string Action,
    bool IsActive,
    string? Snapshot = null);

/// <summary>
///     Manages relationships between tables in the AMO-TOM Model.
///     Supports creating, updating active state, and deleting single-column relationships.
/// </summary>
public static class PbiRelationshipService
{
    /// <summary>
    ///     Creates, activates, or deletes a relationship between two tables in the model.
    /// </summary>
    public static RelationshipMutationResult ManageRelationship(
        Model model,
        string action,
        string fromTable,
        string fromColumn,
        string toTable,
        string toColumn,
        bool isActive = true,
        string crossFilteringBehavior = "OneDirection",
        string? snapshotName = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or whitespace.", nameof(action));
        if (string.IsNullOrWhiteSpace(fromTable))
            throw new ArgumentException("FromTable cannot be null or whitespace.", nameof(fromTable));
        if (string.IsNullOrWhiteSpace(fromColumn))
            throw new ArgumentException("FromColumn cannot be null or whitespace.", nameof(fromColumn));
        if (string.IsNullOrWhiteSpace(toTable))
            throw new ArgumentException("ToTable cannot be null or whitespace.", nameof(toTable));
        if (string.IsNullOrWhiteSpace(toColumn))
            throw new ArgumentException("ToColumn cannot be null or whitespace.", nameof(toColumn));

        var tFrom = model.Tables[fromTable]
            ?? throw new ArgumentException($"FromTable '{fromTable}' was not found in model.", nameof(fromTable));
        var cFrom = tFrom.Columns[fromColumn]
            ?? throw new ArgumentException($"FromColumn '{fromColumn}' was not found in table '{fromTable}'.", nameof(fromColumn));
        var tTo = model.Tables[toTable]
            ?? throw new ArgumentException($"ToTable '{toTable}' was not found in model.", nameof(toTable));
        var cTo = tTo.Columns[toColumn]
            ?? throw new ArgumentException($"ToColumn '{toColumn}' was not found in table '{toTable}'.", nameof(toColumn));

        var normalizedAction = action.Trim().ToLowerInvariant();

        if (normalizedAction is not ("create" or "delete" or "set_active" or "activate" or "deactivate"))
        {
            throw new ArgumentException($"Unknown relationship action '{action}'. Supported actions: 'create', 'delete', 'set_active'.", nameof(action));
        }

        // Find existing relationship matching from/to columns
        var existing = model.Relationships.OfType<SingleColumnRelationship>()
            .FirstOrDefault(r =>
                string.Equals(r.FromTable?.Name, fromTable, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.FromColumn?.Name, fromColumn, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.ToTable?.Name, toTable, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.ToColumn?.Name, toColumn, StringComparison.OrdinalIgnoreCase));

        if (normalizedAction == "delete")
        {
            if (existing == null)
            {
                throw new InvalidOperationException($"Relationship between '{fromTable}[{fromColumn}]' and '{toTable}[{toColumn}]' was not found for deletion.");
            }

            model.Relationships.Remove(existing);
            model.SaveChanges();
            Log.Information("Deleted relationship '{Rel}' and committed changes", existing.Name);

            return new RelationshipMutationResult(
                FromTable: fromTable,
                FromColumn: fromColumn,
                ToTable: toTable,
                ToColumn: toColumn,
                Action: "Deleted",
                IsActive: false,
                Snapshot: snapshotName);
        }

        if (normalizedAction is "set_active" or "activate" or "deactivate")
        {
            if (existing == null)
            {
                throw new InvalidOperationException($"Relationship between '{fromTable}[{fromColumn}]' and '{toTable}[{toColumn}]' was not found.");
            }

            existing.IsActive = isActive;
            model.SaveChanges();
            Log.Information("Updated relationship '{Rel}' active state to {Active}", existing.Name, isActive);

            return new RelationshipMutationResult(
                FromTable: fromTable,
                FromColumn: fromColumn,
                ToTable: toTable,
                ToColumn: toColumn,
                Action: "Updated",
                IsActive: isActive,
                Snapshot: snapshotName);
        }

        // Action is "create" (or update if already exists)
        var crossFilter = string.Equals(crossFilteringBehavior, "BothDirections", StringComparison.OrdinalIgnoreCase)
            ? CrossFilteringBehavior.BothDirections
            : CrossFilteringBehavior.OneDirection;

        if (existing != null)
        {
            existing.IsActive = isActive;
            existing.CrossFilteringBehavior = crossFilter;
            model.SaveChanges();
            Log.Information("Updated existing relationship '{Rel}'", existing.Name);

            return new RelationshipMutationResult(
                FromTable: fromTable,
                FromColumn: fromColumn,
                ToTable: toTable,
                ToColumn: toColumn,
                Action: "Updated",
                IsActive: isActive,
                Snapshot: snapshotName);
        }

        var relName = $"{fromTable}_{fromColumn}_{toTable}_{toColumn}";
        var newRel = new SingleColumnRelationship
        {
            Name = relName,
            FromColumn = cFrom,
            ToColumn = cTo,
            IsActive = isActive,
            CrossFilteringBehavior = crossFilter,
        };

        model.Relationships.Add(newRel);
        model.SaveChanges();
        Log.Information("Created new relationship '{Rel}' and committed changes", relName);

        return new RelationshipMutationResult(
            FromTable: fromTable,
            FromColumn: fromColumn,
            ToTable: toTable,
            ToColumn: toColumn,
            Action: "Created",
            IsActive: isActive,
            Snapshot: snapshotName);
    }
}
