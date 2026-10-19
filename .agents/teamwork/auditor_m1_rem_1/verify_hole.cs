using System;
using System.Text.Json;
using HPBim.Client.Pipeline;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;

class Program
{
    static void Main()
    {
        string json = "{\"sourceKey\":\"[Doc]#[Column]:C1\",\"category\":\"Column\",\"mark\":\"C1\",\"status\":\"Created\"}";
        using var doc = JsonDocument.Parse(json);
        var elem = doc.RootElement;
        
        // Emulate ParseSingleElementRecord
        string? targetUid = elem.TryGetProperty("targetUniqueId", out var p)
            ? (p.ValueKind == JsonValueKind.String ? p.GetString() : string.Empty) : null;
            
        Console.WriteLine($"targetUid is null: {targetUid == null}");
        
        bool checkEvaluatesTo = targetUid != null && string.IsNullOrWhiteSpace(targetUid);
        Console.WriteLine($"Does (targetUid != null && string.IsNullOrWhiteSpace(targetUid)) trigger? {checkEvaluatesTo}");
    }
}
