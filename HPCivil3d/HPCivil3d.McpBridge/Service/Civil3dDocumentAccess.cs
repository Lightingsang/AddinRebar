using Autodesk.Civil.ApplicationServices;
using Serilog;

namespace HPCivil3d.McpBridge.Service;

/// <summary>
///     The one place that asks Civil 3D for its active document. <see cref="CivilApplication.ActiveDocument"/>
///     is a static getter on the Civil API. Verified live in Civil 3D 2026: every drawing opened there — even one
///     made from acad.dwt — answers with a CivilDocument (Civil settings are created lazily), so null is only the
///     defensive path for a getter that throws; the script then sees `civil == null` and the context reports
///     `isCivilDocument: false`.
/// </summary>
public static class Civil3dDocumentAccess
{
    public static CivilDocument? TryGetActive()
    {
        try
        {
            return CivilApplication.ActiveDocument;
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "CivilApplication.ActiveDocument threw; treating the drawing as a non-Civil document");
            return null;
        }
    }

    /// <summary>Which Autodesk product hosts the bridge — Civil3D when the bundle loaded where it should.</summary>
    public static string ProductName()
    {
        try
        {
            return CivilApplication.ActiveProduct.ToString();
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "CivilApplication.ActiveProduct threw");
            return "Unknown";
        }
    }
}
