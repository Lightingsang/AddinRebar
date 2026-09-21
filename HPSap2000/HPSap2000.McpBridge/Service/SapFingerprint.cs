using SAP2000v1;
using HPRebar.Mcp.Contracts.Messages;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     What the bridge can tell about the model cheaply before and after a run: names on receivers,
///     lock flag, and file name. Diffing produces additions and deletions only.
/// </summary>
public sealed record SapFingerprint(IReadOnlyDictionary<string, IReadOnlySet<string>?> Names, bool IsLocked, string? FileName)
{
    public static readonly string[] Receivers = ["PointObj", "FrameObj", "AreaObj", "LoadPatterns", "LoadCases", "RespCombo", "PropFrame", "PropMaterial", "GroupDef"];

    public static SapFingerprint Take(cSapModel sapModel)
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
            ["GroupDef"] = NameList((ref int n, ref string[]? l) => sapModel.GroupDef.GetNameList(ref n, ref l)),
        };

        bool locked;
        try { locked = sapModel.GetModelIsLocked(); }
        catch (Exception exception) when (!SapAttachment.IsDisconnectError(exception)) { locked = false; }

        string? file;
        try { file = SapContextReader.ModelFile(sapModel.GetModelFilename(true)); }
        catch (Exception exception) when (!SapAttachment.IsDisconnectError(exception)) { file = null; }

        return new SapFingerprint(names, locked, file);
    }

    public static (ChangedCounts changed, IReadOnlyList<string> notes) Diff(SapFingerprint before, SapFingerprint after)
    {
        var added = 0;
        var deleted = 0;
        var notes = new List<string>();

        foreach (var receiver in before.Names.Keys.Union(after.Names.Keys, StringComparer.Ordinal))
        {
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
        catch (Exception exception) when (!SapAttachment.IsDisconnectError(exception))
        {
            return null;
        }
    }
}
