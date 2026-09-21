using System;
using Microsoft.AnalysisServices.Tabular;
using Serilog;

namespace HPPowerBi.McpBridge.Tabular;

public sealed record MeasureMutationResult(
    string TableName,
    string MeasureName,
    string Action,
    string? Expression,
    string? Snapshot = null);

/// <summary>
///     Provides CRUD operations for DAX measures on the AMO-TOM Model.
///     Changes are staged on TOM objects and committed to Power BI Desktop via model.SaveChanges().
/// </summary>
public static class PbiMeasureService
{
    /// <summary>
    ///     Creates a new measure or updates an existing measure in the specified table.
    /// </summary>
    public static MeasureMutationResult CreateOrUpdateMeasure(
        Model model,
        string tableName,
        string measureName,
        string expression,
        string? formatString = null,
        string? displayFolder = null,
        string? description = null,
        bool? isHidden = null,
        string? snapshotName = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(measureName))
            throw new ArgumentException("Measure name cannot be null or whitespace.", nameof(measureName));
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("DAX expression cannot be null or whitespace.", nameof(expression));

        var table = model.Tables[tableName]
            ?? throw new ArgumentException($"Table '{tableName}' was not found in the model.", nameof(tableName));

        var existingMeasure = table.Measures.Find(measureName);
        var isNew = existingMeasure == null;

        if (isNew)
        {
            var newMeasure = new Measure
            {
                Name = measureName,
                Expression = expression,
            };

            if (formatString != null) newMeasure.FormatString = formatString;
            if (displayFolder != null) newMeasure.DisplayFolder = displayFolder;
            if (description != null) newMeasure.Description = description;
            if (isHidden.HasValue) newMeasure.IsHidden = isHidden.Value;

            table.Measures.Add(newMeasure);
            Log.Information("Adding new measure '{Measure}' to table '{Table}'", measureName, tableName);
        }
        else
        {
            existingMeasure!.Expression = expression;
            if (formatString != null) existingMeasure.FormatString = formatString;
            if (displayFolder != null) existingMeasure.DisplayFolder = displayFolder;
            if (description != null) existingMeasure.Description = description;
            if (isHidden.HasValue) existingMeasure.IsHidden = isHidden.Value;

            Log.Information("Updating existing measure '{Measure}' in table '{Table}'", measureName, tableName);
        }

        model.SaveChanges();
        Log.Information("Committed measure mutation via model.SaveChanges()");

        return new MeasureMutationResult(
            TableName: tableName,
            MeasureName: measureName,
            Action: isNew ? "Created" : "Updated",
            Expression: expression,
            Snapshot: snapshotName);
    }

    /// <summary>
    ///     Deletes an existing measure from the specified table.
    /// </summary>
    public static bool DeleteMeasure(
        Model model,
        string tableName,
        string measureName,
        string? snapshotName = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(measureName))
            throw new ArgumentException("Measure name cannot be null or whitespace.", nameof(measureName));

        var table = model.Tables[tableName]
            ?? throw new ArgumentException($"Table '{tableName}' was not found in the model.", nameof(tableName));

        var measure = table.Measures.Find(measureName);
        if (measure == null)
        {
            Log.Warning("Measure '{Measure}' was not found in table '{Table}' for deletion", measureName, tableName);
            return false;
        }

        table.Measures.Remove(measure);
        model.SaveChanges();
        Log.Information("Deleted measure '{Measure}' from table '{Table}' and committed changes", measureName, tableName);
        return true;
    }
}
