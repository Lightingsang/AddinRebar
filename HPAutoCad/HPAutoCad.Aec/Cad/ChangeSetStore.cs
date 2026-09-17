using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.ChangeSets;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The change-set ledgers of the process, one per open drawing, keyed by the drawing's native database (the managed
///     <see cref="Database"/> wrapper is a fresh object on every request, the native object lives as long as the document) and checked
///     against the drawing's fingerprint GUID (a later drawing at a recycled address gets a fresh ledger) so a set can never be replayed
///     into another document. Lives as long as the bridge (the engine assembly stays loaded between requests); a drawing being closed
///     releases its snapshots and drops its ledger, and the bridge's terminate drops them all.
/// </summary>
public static class ChangeSetStore
{
    private static readonly Dictionary<IntPtr, (string Fingerprint, ChangeSetLedger Ledger)> Ledgers = new();
    private static readonly object Gate = new();
    private static bool _hooked;

    public static ChangeSetLedger For(Database db)
    {
        lock (Gate)
        {
            if (!_hooked) Hook();
            var fingerprint = Fingerprint(db);
            if (Ledgers.TryGetValue(db.UnmanagedObject, out var entry) && entry.Fingerprint != fingerprint)
            {
                entry.Ledger.ReleaseAll();
                Ledgers.Remove(db.UnmanagedObject);
            }

            if (!Ledgers.TryGetValue(db.UnmanagedObject, out entry)) Ledgers[db.UnmanagedObject] = entry = (fingerprint, new ChangeSetLedger(DocumentName(db)));
            entry.Ledger.Document = DocumentName(db);
            return entry.Ledger;
        }
    }

    /// <summary>Forgets a drawing's sets (its snapshots first); a no-op for a drawing without any.</summary>
    public static void Drop(Database db)
    {
        lock (Gate)
        {
            if (!Ledgers.Remove(db.UnmanagedObject, out var entry)) return;
            entry.Ledger.ReleaseAll();
        }
    }

    /// <summary>Forgets every drawing's sets — the bridge is terminating, so no clone reaches the finaliser thread.</summary>
    public static void DropAll()
    {
        lock (Gate)
        {
            foreach (var entry in Ledgers.Values) entry.Ledger.ReleaseAll();
            Ledgers.Clear();
        }
    }

    private static string Fingerprint(Database db)
    {
        try { return db.FingerprintGuid ?? ""; }
        catch { return ""; }
    }

    private static string DocumentName(Database db)
    {
        try { return Path.GetFileName(db.Filename) is { Length: > 0 } name ? name : "(unnamed drawing)"; }
        catch { return "(unnamed drawing)"; }
    }

    private static void Hook()
    {
        _hooked = true;
        try
        {
            AcadApp.DocumentManager.DocumentToBeDestroyed += (_, e) =>
            {
                try { if (e.Document?.Database is { } db) Drop(db); }
                catch { /* a closing document must never be blocked by bookkeeping */ }
            };
        }
        catch
        {
            // No document manager (a core console, a test host): ledgers then live as long as the process.
        }
    }
}
