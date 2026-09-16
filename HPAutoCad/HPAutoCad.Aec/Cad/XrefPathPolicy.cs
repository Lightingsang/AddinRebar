namespace HPAutoCad.Aec.Cad;

/// <summary>
///     What <c>manage_xrefs attach</c> accepts as a path: a fully qualified <c>.dwg</c> (drive or UNC root — never drive-relative
///     <c>C:x.dwg</c> or root-relative <c>\x.dwg</c>, which resolve against the process' working directory), with no <c>..</c>
///     segment, and not the drawing being edited. Pure, so the rule is unit-tested; existence on disk is checked by the caller.
/// </summary>
public static class XrefPathPolicy
{
    /// <summary>Null when the path is acceptable, otherwise the reason it is not.</summary>
    public static string? Refuse(string? path, string? currentDrawing = null)
    {
        var p = (path ?? "").Trim();
        if (p.Length == 0) return "attach needs path: an absolute path to a .dwg file.";
        if (!Path.IsPathFullyQualified(p)) return $"path '{p}' is not fully qualified (use D:\\folder\\file.dwg or \\\\server\\share\\file.dwg; drive-relative and root-relative paths are refused).";
        if (!string.Equals(Path.GetExtension(p), ".dwg", StringComparison.OrdinalIgnoreCase)) return $"path '{p}' is not a .dwg file.";
        if (p.Split('\\', '/').Any(segment => segment == "..")) return $"path '{p}' must not contain '..' segments.";
        if (!string.IsNullOrEmpty(currentDrawing) && string.Equals(Path.GetFullPath(p), Path.GetFullPath(currentDrawing), StringComparison.OrdinalIgnoreCase)) return "a drawing cannot reference itself.";
        return null;
    }

    /// <summary>Whether the path is a UNC location — reads from it block AutoCAD's thread for the share's timeout when the server is unreachable.</summary>
    public static bool IsUnc(string path) => path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal);
}
