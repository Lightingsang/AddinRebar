namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Thrown when an element violates Fail-Fast preconditions (missing elevation, cross section, or supports),
/// enforcing the Prime Directive: halt immediately without speculation.
/// </summary>
public sealed class FailFastCoordinationException : Exception
{
    public string ElementId { get; }
    public IReadOnlyList<string> Violations { get; }

    public FailFastCoordinationException(string elementId, IEnumerable<string> violations)
        : base(FormatMessage(elementId, violations))
    {
        ElementId = elementId ?? string.Empty;
        Violations = violations != null
            ? new List<string>(violations).AsReadOnly()
            : (IReadOnlyList<string>)Array.Empty<string>();
    }

    public FailFastCoordinationException(string elementId, string singleViolation)
        : this(elementId, new[] { singleViolation })
    {
    }

    public FailFastCoordinationException(string message)
        : this("Coordination", message)
    {
    }

    private static string FormatMessage(string elementId, IEnumerable<string>? violations)
    {
        string idText = string.IsNullOrWhiteSpace(elementId) ? "Unknown" : elementId;
        string violationList = violations != null ? string.Join("; ", violations) : "Chưa đủ cơ sở để kết luận.";
        return $"[FailFastGate] Cấu kiện '{idText}' không đủ cơ sở để kết luận: {violationList}";
    }
}
