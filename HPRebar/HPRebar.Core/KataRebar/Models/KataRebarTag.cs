using System;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Comment stamped on every bar Kata Rebar creates, naming its host. A re-run deletes exactly the bars
/// carrying its host's tag: bars drawn by hand or by another tool, and bars of other beams, are never touched,
/// whatever the beam is called.
/// </summary>
public static class KataRebarTag
{
    public const string Prefix = "HPRebar_Kata:";

    public static string ForHost(string hostUniqueId)
    {
        if (string.IsNullOrWhiteSpace(hostUniqueId))
            throw new ArgumentException("A host needs a unique id to be tagged.", nameof(hostUniqueId));

        return Prefix + hostUniqueId.Trim();
    }

    public static bool IsTagFor(string? comment, string hostUniqueId) =>
        !string.IsNullOrWhiteSpace(hostUniqueId)
        && string.Equals(comment?.Trim(), ForHost(hostUniqueId), StringComparison.Ordinal);
}
