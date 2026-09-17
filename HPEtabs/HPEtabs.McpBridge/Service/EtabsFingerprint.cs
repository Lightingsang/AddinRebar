using ETABSv1;
using HPRebar.Mcp.Contracts.Messages;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     What the bridge can tell about the model cheaply before and after a run: the names on the receivers the
///     spike proved answer <c>GetNameList</c>, the stories, the lock flag and the file name. Diffing two of them
///     gives <c>Changed</c> as additions and deletions only — ETABS has no change tracking, and a <c>Set*</c> on
///     an existing object leaves its name in place, so <c>Modified</c> is always 0 and the tool description
///     says so. A read-only run that sees a difference reports it as a log line (another writer), never as an error.
/// </summary>
public sealed record EtabsFingerprint(IReadOnlyDictionary<string, IReadOnlySet<string>?> Names, bool IsLocked, string? FileName)
{
    public static readonly string[] Receivers = ["PointObj", "FrameObj", "AreaObj", "LoadPatterns", "LoadCases", "RespCombo", "PropFrame", "PropMaterial", "Story"];

    /// <summary>One read per receiver on the STA worker; a receiver that refuses is recorded as unknown (null) and never diffed.</summary>
    public static EtabsFingerprint Take(cSapModel sapModel)
    {
        var names = new Dictionary<string, IReadOnlySet<string>?>(StringComparer.Ordinal)
        {
            ["PointObj"] = NameList((ref int n, ref string[]? l) => sapModel.PointObj.GetNameList(ref n, ref l)),
            ["FrameObj"] = NameList((ref int n, ref string[]? l) => sapModel.FrameObj.GetNameList(ref n, ref l)),
            ["AreaObj"] = NameList((ref int n, ref string[]? l) => sapModel.AreaObj.GetNameList(ref n, ref l)),
            ["LoadPatterns"] = NameList((ref int n, ref string[]? l) => sapModel.LoadPatterns.GetNameList(ref n, ref l)),
            ["LoadCases"] = NameList((ref int n, ref string[]? l) => sapModel.LoadCases.GetNameList(ref n, ref l)),
            ["RespCombo"] = NameList((ref int n, ref string[]? l) => sapModel.RespCombo.GetNameList(ref n, ref l)),
            ["PropFrame"] = NameList((ref int n, ref string[]? l) => sapModel.PropFrame.GetNameList(ref n, ref l)),
            ["PropMaterial"] = NameList((ref int n, ref string[]? l) => sapModel.PropMaterial.GetNameList(ref n, ref l)),
            ["Story"] = Stories(sapModel),
        };

        bool locked;
        try { locked = sapModel.GetModelIsLocked(); }
        catch (Exception exception) when (!EtabsAttachment.IsDisconnectError(exception)) { locked = false; }

        string? file;
        try { file = EtabsContextReader.ModelFile(sapModel.GetModelFilename(true)); }
        catch (Exception exception) when (!EtabsAttachment.IsDisconnectError(exception)) { file = null; }

        return new EtabsFingerprint(names, locked, file);
    }

    /// <summary>Additions and deletions per receiver, plus the notes a caller may log (lock or file changes).</summary>
    public static (ChangedCounts changed, IReadOnlyList<string> notes) Diff(EtabsFingerprint before, EtabsFingerprint after)
    {
        var added = 0;
        var deleted = 0;
        var notes = new List<string>();

        foreach (var receiver in before.Names.Keys.Union(after.Names.Keys, StringComparer.Ordinal))
        {
            // Unknown on either side (the receiver refused): nothing to say about it.
            if (!before.Names.TryGetValue(receiver, out var was) || was is null || !after.Names.TryGetValue(receiver, out var now) || now is null) continue;
            var plus = now.Count(n => !was.Contains(n));
            var minus = was.Count(n => !now.Contains(n));
            added += plus;
            deleted += minus;
            if (plus + minus > 0) notes.Add($"{receiver}: +{plus} -{minus}");
        }

        if (before.IsLocked != after.IsLocked) notes.Add($"model lock: {before.IsLocked} → {after.IsLocked}");
        if (!string.Equals(before.FileName, after.FileName, StringComparison.OrdinalIgnoreCase)) notes.Add($"model file: {System.IO.Path.GetFileName(before.FileName)} → {System.IO.Path.GetFileName(after.FileName)}");

        return (new ChangedCounts(added, 0, deleted), notes);
    }

    private delegate int NameListCall(ref int count, ref string[]? names);

    private static IReadOnlySet<string>? NameList(NameListCall call)
    {
        try
        {
            var count = 0;
            string[]? names = null;
            if (call(ref count, ref names) != 0) return null;
            return names is null ? new HashSet<string>() : new HashSet<string>(names, StringComparer.Ordinal);
        }
        catch (Exception exception) when (!EtabsAttachment.IsDisconnectError(exception))
        {
            return null;
        }
    }

    private static IReadOnlySet<string>? Stories(cSapModel sapModel)
    {
        try
        {
            double baseElevation = 0;
            var count = 0;
            string[]? names = null;
            double[]? elevations = null, heights = null, spliceHeights = null;
            bool[]? isMaster = null, splice = null;
            string[]? similar = null;
            int[]? color = null;
            var ret = sapModel.Story.GetStories_2(ref baseElevation, ref count, ref names, ref elevations, ref heights, ref isMaster, ref similar, ref splice, ref spliceHeights, ref color);
            if (ret != 0) return null;
            return names is null ? new HashSet<string>() : new HashSet<string>(names, StringComparer.Ordinal);
        }
        catch (Exception exception) when (!EtabsAttachment.IsDisconnectError(exception))
        {
            return null;
        }
    }
}
