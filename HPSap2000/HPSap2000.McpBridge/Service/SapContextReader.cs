using System.IO;
using SAP2000v1;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     Builds the session snapshot the AI asks for before scripting. Runs on the STA worker.
/// </summary>
public static class SapContextReader
{
    private const int MaxSelection = 50;

    public static ContextResult Read(SapAttachment attachment, bool includeSelection, bool executionEnabled, bool destructiveEnabled,
        string hostVersion, bool quiescent)
    {
        var result = new ContextResult
        {
            RevitVersion = hostVersion,
            Host = PipeNaming.Sap2000Host,
            HostVersion = hostVersion,
            ExecutionEnabled = executionEnabled,
            Units = new UnitsInfo("m"),
        };

        if (!attachment.Attached)
        {
            result.Sap2000 = new Sap2000Info(false, 0, null, false, null, null, destructiveEnabled, 0, 0, 0);
            return result;
        }

        var (_, sapModel) = attachment.Require();

        // One unguarded call first to detect dead COM channel
        var presentUnits = sapModel.GetPresentUnits().ToString();

        var fileName = ModelFile(Safe(() => sapModel.GetModelFilename(true)));
        var hasFile = fileName is not null;
        var isLocked = Safe(() => (bool?)sapModel.GetModelIsLocked()) ?? false;

        result.DocTitle = hasFile ? Path.GetFileName(fileName) : null;
        result.DocPath = fileName;
        result.IsReadOnly = isLocked;
        result.IsModifiable = quiescent && hasFile;
        result.ActiveView = new ViewInfo(0, result.DocTitle ?? "Untitled", "Model");
        result.OpenDocs = result.DocTitle is null ? [] : [result.DocTitle];
        result.Sap2000 = new Sap2000Info(
            IsAttached: true,
            AttachedPid: attachment.Pid,
            OapiVersion: attachment.OapiVersion,
            IsLocked: isLocked,
            PresentUnits: presentUnits,
            DatabaseUnits: Safe(() => sapModel.GetDatabaseUnits().ToString()),
            DestructiveOperationsEnabled: destructiveEnabled,
            PointCount: CountOf(() => { int n = 0; string[]? names = null; return sapModel.PointObj.GetNameList(ref n, ref names) == 0 ? n : -1; }),
            FrameCount: CountOf(() => { int n = 0; string[]? names = null; return sapModel.FrameObj.GetNameList(ref n, ref names) == 0 ? n : -1; }),
            AreaCount: CountOf(() => { int n = 0; string[]? names = null; return sapModel.AreaObj.GetNameList(ref n, ref names) == 0 ? n : -1; }));

        if (includeSelection) result.Selection = ReadSelection(sapModel);

        return result;
    }

    /// <summary>Selected objects as {id = running index, category = object type, name = unique name}.</summary>
    private static IReadOnlyList<ElementInfo> ReadSelection(cSapModel sapModel)
    {
        try
        {
            int count = 0;
            int[]? types = null;
            string[]? names = null;
            if (sapModel.SelectObj.GetSelected(ref count, ref types, ref names) != 0 || names is null) return [];

            return names.Take(MaxSelection)
                .Select((name, index) => new ElementInfo(index, ObjectTypeName(types is not null && index < types.Length ? types[index] : 0), name))
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Object type codes of cSelect.GetSelected: 1 point, 2 frame, 3 cable, 4 tendon, 5 area, 6 solid, 7 link.</summary>
    private static string ObjectTypeName(int type) => type switch
    {
        1 => "Point", 2 => "Frame", 3 => "Cable", 4 => "Tendon", 5 => "Area", 6 => "Solid", 7 => "Link", _ => "Object",
    };

    /// <summary>
    ///     The model's `.SDB` path, or null when nothing is open.
    /// </summary>
    public static string? ModelFile(string? reported)
    {
        if (string.IsNullOrWhiteSpace(reported) || !Path.IsPathRooted(reported)) return null;
        var ext = Path.GetExtension(reported);
        if (string.Equals(ext, ".$2k", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ext, ".$sb", StringComparison.OrdinalIgnoreCase))
        {
            return Path.ChangeExtension(reported, ".SDB");
        }
        return reported;
    }

    private static int CountOf(Func<int> read)
    {
        try { return Math.Max(0, read()); }
        catch { return 0; }
    }

    private static T? Safe<T>(Func<T?> read)
    {
        try { return read(); }
        catch { return default; }
    }
}
