using Autodesk.AutoCAD.DatabaseServices;
using HPRebar.Mcp.Contracts.Messages;

namespace HPAutoCad.McpBridge.Service;

/// <summary>
///     Counts what a script changed while the bridge's transactions are still open. Two things make this
///     indirect: ObjectAppended and ObjectModified only fire when the outermost transaction commits (a
///     dry run would always count zero), and holding on to any DBObject wrapper past the transaction
///     ends with a finaliser crash. So: every appended object consumed a handle — HANDSEED before and
///     after gives "added" whatever transaction it went through; ObjectOpenedForModify fires immediately
///     and gives the candidates for "modified" by ObjectId (a struct, nothing to finalise); asking each
///     candidate's ObjectId whether it is erased splits off "deleted" (ObjectErased, like the commit
///     events, only fires when the outermost transaction ends). Approximate by construction: a container
///     opened for write to receive an entity is not a modification the AI asked for, so symbol tables
///     and block table records are left out, and an erase without a prior open for write is not seen.
/// </summary>
public sealed class DatabaseChangeCounter : IDisposable
{
    private readonly Database _database;
    private readonly long _handseedBefore;
    private readonly HashSet<ObjectId> _touched = [];

    private DatabaseChangeCounter(Database database)
    {
        _database = database;
        _handseedBefore = database.Handseed.Value;
        _database.ObjectOpenedForModify += OnOpenedForModify;
    }

    /// <summary>Subscribe before the script runs; dispose once the outer transaction has ended.</summary>
    public static DatabaseChangeCounter Begin(Database database) => new DatabaseChangeCounter(database);

    /// <summary>Read while the script's transaction is still open — erased state is only visible until then.</summary>
    public ChangedCounts Counts
    {
        get
        {
            var appended = (int)Math.Max(0, _database.Handseed.Value - _handseedBefore);
            int modified = 0, deleted = 0, erasedNew = 0;

            foreach (var id in _touched)
            {
                var erased = IsErased(id);
                if (IsNew(id))
                {
                    if (erased) erasedNew++; // created and erased in the same run: nothing changed
                }
                else if (erased) deleted++;
                else modified++;
            }

            return new ChangedCounts(Math.Max(0, appended - erasedNew), modified, deleted);
        }
    }

    private bool IsNew(ObjectId id) => id.Handle.Value >= _handseedBefore;

    private static bool IsErased(ObjectId id)
    {
        try { return id.IsErased; }
        catch { return false; }
    }

    private void OnOpenedForModify(object? sender, ObjectEventArgs e)
    {
        if (e.DBObject is SymbolTable or BlockTableRecord) return;

        _touched.Add(e.DBObject.ObjectId);
    }

    public void Dispose() => _database.ObjectOpenedForModify -= OnOpenedForModify;
}
