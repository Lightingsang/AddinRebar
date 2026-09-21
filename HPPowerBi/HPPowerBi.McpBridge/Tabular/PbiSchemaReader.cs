using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AnalysisServices.Tabular;

namespace HPPowerBi.McpBridge.Tabular;

#region Schema DTOs

public sealed record TabularModelSchema(
    string ModelName,
    int CompatibilityLevel,
    IReadOnlyList<TableSchemaDto> Tables,
    IReadOnlyList<RelationshipSchemaDto> Relationships);

public sealed record TableSchemaDto(
    string Name,
    string? Description,
    bool IsHidden,
    string? DataCategory,
    IReadOnlyList<ColumnSchemaDto> Columns,
    IReadOnlyList<MeasureSchemaDto> Measures,
    IReadOnlyList<PartitionSchemaDto> Partitions,
    IReadOnlyList<HierarchySchemaDto> Hierarchies);

public sealed record ColumnSchemaDto(
    string Name,
    string DataType,
    string Type,
    string? Expression,
    string? FormatString,
    bool IsHidden,
    string? DataCategory,
    string? Description);

public sealed record MeasureSchemaDto(
    string Name,
    string Expression,
    string? FormatString,
    string? Description,
    string? DisplayFolder,
    bool IsHidden);

public sealed record PartitionSchemaDto(
    string Name,
    string Mode,
    string SourceType,
    string? Expression);

public sealed record HierarchySchemaDto(
    string Name,
    IReadOnlyList<string> Levels);

public sealed record RelationshipSchemaDto(
    string Name,
    string FromTable,
    string FromColumn,
    string ToTable,
    string ToColumn,
    bool IsActive,
    string Cardinality,
    string CrossFilteringBehavior);

#endregion

/// <summary>
///     Extracts comprehensive schema metadata from an active AMO-TOM Tabular Model.
/// </summary>
public static class PbiSchemaReader
{
    public static TabularModelSchema ExtractSchema(
        Model model,
        string? tableFilter = null,
        bool includeColumns = true,
        bool includeMeasures = true,
        bool includeRelationships = true)
    {
        ArgumentNullException.ThrowIfNull(model);

        var tables = new List<TableSchemaDto>();

        foreach (Table table in model.Tables)
        {
            if (!string.IsNullOrWhiteSpace(tableFilter) &&
                !string.Equals(table.Name, tableFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var columns = new List<ColumnSchemaDto>();
            if (includeColumns)
            {
                foreach (Column col in table.Columns)
                {
                    string? expression = null;
                    if (col is CalculatedColumn calcCol)
                        expression = calcCol.Expression;

                    columns.Add(new ColumnSchemaDto(
                        Name: col.Name,
                        DataType: col.DataType.ToString(),
                        Type: col.Type.ToString(),
                        Expression: expression,
                        FormatString: col.FormatString,
                        IsHidden: col.IsHidden,
                        DataCategory: col.DataCategory,
                        Description: col.Description));
                }
            }

            var measures = new List<MeasureSchemaDto>();
            if (includeMeasures)
            {
                foreach (Measure m in table.Measures)
                {
                    measures.Add(new MeasureSchemaDto(
                        Name: m.Name,
                        Expression: m.Expression,
                        FormatString: m.FormatString,
                        Description: m.Description,
                        DisplayFolder: m.DisplayFolder,
                        IsHidden: m.IsHidden));
                }
            }

            var partitions = new List<PartitionSchemaDto>();
            foreach (Partition part in table.Partitions)
            {
                string? partExpression = null;
                if (part.Source is MPartitionSource mSource)
                    partExpression = mSource.Expression;
                else if (part.Source is QueryPartitionSource qSource)
                    partExpression = qSource.Query;
                else if (part.Source is CalculatedPartitionSource cSource)
                    partExpression = cSource.Expression;

                partitions.Add(new PartitionSchemaDto(
                    Name: part.Name,
                    Mode: part.Mode.ToString(),
                    SourceType: part.SourceType.ToString(),
                    Expression: partExpression));
            }

            var hierarchies = new List<HierarchySchemaDto>();
            foreach (Hierarchy h in table.Hierarchies)
            {
                var levels = h.Levels.Select(l => l.Column?.Name ?? l.Name).ToList();
                hierarchies.Add(new HierarchySchemaDto(h.Name, levels));
            }

            tables.Add(new TableSchemaDto(
                Name: table.Name,
                Description: table.Description,
                IsHidden: table.IsHidden,
                DataCategory: table.DataCategory,
                Columns: columns,
                Measures: measures,
                Partitions: partitions,
                Hierarchies: hierarchies));
        }

        var relationships = new List<RelationshipSchemaDto>();
        if (includeRelationships)
        {
            foreach (Relationship rel in model.Relationships)
            {
                if (rel is SingleColumnRelationship scr)
                {
                    // If tableFilter is active, only include relationships touching the filtered table
                    if (!string.IsNullOrWhiteSpace(tableFilter) &&
                        !string.Equals(scr.FromTable?.Name, tableFilter, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(scr.ToTable?.Name, tableFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    relationships.Add(new RelationshipSchemaDto(
                        Name: scr.Name ?? $"{scr.FromTable?.Name}_{scr.FromColumn?.Name}_{scr.ToTable?.Name}_{scr.ToColumn?.Name}",
                        FromTable: scr.FromTable?.Name ?? string.Empty,
                        FromColumn: scr.FromColumn?.Name ?? string.Empty,
                        ToTable: scr.ToTable?.Name ?? string.Empty,
                        ToColumn: scr.ToColumn?.Name ?? string.Empty,
                        IsActive: scr.IsActive,
                        Cardinality: $"{scr.FromCardinality}To{scr.ToCardinality}",
                        CrossFilteringBehavior: scr.CrossFilteringBehavior.ToString()));
                }
            }
        }

        return new TabularModelSchema(
            ModelName: model.Name ?? "Model",
            CompatibilityLevel: model.Database?.CompatibilityLevel ?? 0,
            Tables: tables,
            Relationships: relationships);
    }
}
