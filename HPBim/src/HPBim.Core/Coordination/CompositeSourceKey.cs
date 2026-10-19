namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

/// <summary>
/// Immutable value object representing a Composite Source Key: [DocIdentifier]#[Category]:[PrimaryHandle]
/// Provides deterministic identity, case-insensitive comparison, and provenance handle association.
/// </summary>
[JsonConverter(typeof(CompositeSourceKeyJsonConverter))]
public sealed record CompositeSourceKey : IEquatable<CompositeSourceKey>, IComparable<CompositeSourceKey>
{
    private static readonly Regex BracketPattern = new(
        @"^\[([^\]]*)\]\s*#\s*\[([^\]]+)\]\s*:\s*(.+)$",
        RegexOptions.Compiled);

    private static readonly Regex PlainPattern = new(
        @"^([^#]*)\s*#\s*([^:]+)\s*:\s*(.+)$",
        RegexOptions.Compiled);

    public string DocIdentifier { get; init; } = string.Empty;
    public CoordinationCategory Category { get; init; }
    public string PrimaryHandle { get; init; } = string.Empty;
    public IReadOnlyList<string> ProvenanceHandles { get; init; } = Array.Empty<string>();

    public static readonly CompositeSourceKey Empty = new();

    public CompositeSourceKey() { }

    public CompositeSourceKey(
        string docId,
        CoordinationCategory category,
        string primaryHandle,
        IReadOnlyList<string>? provenance = null)
    {
        DocIdentifier = docId ?? string.Empty;
        Category = category;
        PrimaryHandle = primaryHandle ?? string.Empty;
        ProvenanceHandles = provenance ?? Array.Empty<string>();
    }

    public static CompositeSourceKey Create(
        string docId,
        CoordinationCategory category,
        string handle,
        params string[] provenance) =>
        new(docId, category, handle, provenance);

    public static CompositeSourceKey Parse(string formattedKey, bool allowEmptyDoc = false)
    {
        if (TryParse(formattedKey, out var result, allowEmptyDoc)) return result!;
        throw new FormatException(
            $"Invalid CompositeSourceKey format: '{formattedKey}'. Expected format: [DocIdentifier]#[Category]:[PrimaryHandle]");
    }

    public static bool TryParse(string? formattedKey, out CompositeSourceKey? result) =>
        TryParse(formattedKey, out result, allowEmptyDoc: true);

    public static bool TryParse(string? formattedKey, out CompositeSourceKey? result, bool allowEmptyDoc)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(formattedKey)) return false;

        string trimmed = formattedKey!.Trim();
        var match = BracketPattern.Match(trimmed);
        if (match.Success)
        {
            return TryConstructKey(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, allowEmptyDoc, out result);
        }

        var plainMatch = PlainPattern.Match(trimmed);
        if (plainMatch.Success)
        {
            return TryConstructKey(plainMatch.Groups[1].Value, plainMatch.Groups[2].Value, plainMatch.Groups[3].Value, allowEmptyDoc, out result);
        }

        return false;
    }

    private static bool TryConstructKey(
        string rawDoc,
        string rawCat,
        string rawHandle,
        bool allowEmptyDoc,
        out CompositeSourceKey? result)
    {
        result = null;
        string doc = rawDoc.Trim();
        string handle = rawHandle.Trim().TrimStart('[').TrimEnd(']');

        if (!allowEmptyDoc && string.IsNullOrWhiteSpace(doc)) return false;
        if (string.IsNullOrWhiteSpace(handle)) return false;

        if (!Enum.TryParse<CoordinationCategory>(rawCat.Trim(), ignoreCase: true, out var category) ||
            !Enum.IsDefined(typeof(CoordinationCategory), category))
        {
            return false;
        }

        result = new CompositeSourceKey(doc, category, handle);
        return true;
    }

    public override string ToString() => $"[{DocIdentifier}]#[{Category}]:{PrimaryHandle}";

    public string PlainKey => $"{DocIdentifier}#{Category}:{PrimaryHandle}";

    public bool Equals(CompositeSourceKey? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return string.Equals(DocIdentifier, other.DocIdentifier, StringComparison.OrdinalIgnoreCase) &&
               Category == other.Category &&
               string.Equals(PrimaryHandle, other.PrimaryHandle, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = StringComparer.OrdinalIgnoreCase.GetHashCode(DocIdentifier ?? string.Empty);
            hash = (hash * 397) ^ (int)Category;
            hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(PrimaryHandle ?? string.Empty);
            return hash;
        }
    }


    public int CompareTo(CompositeSourceKey? other)
    {
        if (other is null) return 1;
        int docComp = string.Compare(DocIdentifier, other.DocIdentifier, StringComparison.OrdinalIgnoreCase);
        if (docComp != 0) return docComp;

        int catComp = Category.CompareTo(other.Category);
        if (catComp != 0) return catComp;

        return string.Compare(PrimaryHandle, other.PrimaryHandle, StringComparison.OrdinalIgnoreCase);
    }
}
