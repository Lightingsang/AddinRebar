using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.ChangeSets;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The original state of every entity a commit touches, taken the moment it is first opened for write
///     (<see cref="Database.ObjectOpenedForModify"/> fires before anything changes — the bridge counts changes the same way) as a
///     non-resident clone. Entities created in the run (handle at or past HANDSEED when the run began) are not snapshotted: a rollback
///     erases them. Owned by the set's <see cref="CommitRecord"/> until the undo can no longer be wanted.
/// </summary>
public sealed class SnapshotBag : IDisposable
{
    /// <summary>Clones held per commit; beyond it the commit still goes through but a rollback cannot restore every modification.</summary>
    public const int MaxSnapshots = 2000;

    private readonly Database _db;
    private readonly long _handseedBefore;
    private bool _listening;

    public SnapshotBag(Database db)
    {
        _db = db;
        _handseedBefore = db.Handseed.Value;
        _db.ObjectOpenedForModify += OnOpened;
        _listening = true;
    }

    public Dictionary<string, Entity> Clones { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The cap was hit: entities opened after it have no snapshot.</summary>
    public bool Overflow { get; private set; }

    /// <summary>Entities whose clone failed (a type that cannot be cloned): counted, not snapshotted.</summary>
    public int Unsnapshotted { get; private set; }

    public bool Complete => !Overflow && Unsnapshotted == 0;

    public bool IsNew(Handle handle) => handle.Value >= _handseedBefore;

    /// <summary>Stops taking snapshots (the ops have run) but keeps the clones for a later rollback.</summary>
    public void StopListening()
    {
        if (!_listening) return;
        _db.ObjectOpenedForModify -= OnOpened;
        _listening = false;
    }

    public void Dispose()
    {
        StopListening();
        foreach (var clone in Clones.Values)
        {
            try { clone.Dispose(); }
            catch { /* a clone that is already gone */ }
        }

        Clones.Clear();
    }

    private void OnOpened(object? sender, ObjectEventArgs e)
    {
        if (e.DBObject is not Entity entity || IsNew(entity.Handle)) return;
        var handle = entity.Handle.ToString();
        if (Clones.ContainsKey(handle)) return;
        if (Clones.Count >= MaxSnapshots) { Overflow = true; return; }
        try { Clones[handle] = (Entity)entity.Clone(); }
        catch { Unsnapshotted++; }
    }

    /// <summary>
    ///     A reading of everything the write tools can change on an entity — layer, colour, linetype, lineweight, visibility, the
    ///     geometry and text per type, dynamic block state — so "reads as its snapshot" means the modification is not there.
    /// </summary>
    public static string Fingerprint(Entity e)
    {
        var sb = new StringBuilder();
        sb.Append(e.Layer).Append('|').Append(e.Color).Append('|').Append(e.Linetype).Append('|').Append(e.LinetypeScale.ToString("0.###", CultureInfo.InvariantCulture)).Append('|').Append(e.LineWeight).Append('|').Append(e.Visible);
        switch (e)
        {
            case AttributeReference a: Add(sb, a.Tag, a.TextString, a.Height, a.Rotation, a.Position, a.TextStyleName); break;
            case DBText t: Add(sb, t.TextString, t.Height, t.Rotation, t.Position, t.TextStyleName, t.WidthFactor); break;
            case MText m: Add(sb, m.Contents, m.TextHeight, m.Rotation, m.Location, m.TextStyleName, m.Width); break;
            case Dimension d: Add(sb, d.DimensionText, d.DimensionStyleName, d.TextPosition, d.Measurement); break;
            case Hatch h: Add(sb, h.PatternName, h.PatternScale, h.PatternAngle, h.HatchStyle, h.Associative, h.NumberOfLoops, h.PatternType); break;
            case BlockReference b:
                Add(sb, b.Name, b.Position, b.Rotation, b.ScaleFactors);
                foreach (DynamicBlockReferenceProperty p in b.DynamicBlockReferencePropertyCollection) Add(sb, p.PropertyName, p.Value);
                break;
            case Line l: Add(sb, l.StartPoint, l.EndPoint); break;
            case Circle c: Add(sb, c.Center, c.Radius, c.Normal); break;
            case Arc a: Add(sb, a.Center, a.Radius, a.StartAngle, a.EndAngle, a.Normal); break;
            case Polyline pl:
                Add(sb, pl.NumberOfVertices, pl.Closed, pl.ConstantWidth, pl.Elevation);
                for (var i = 0; i < Math.Min(pl.NumberOfVertices, 256); i++) Add(sb, pl.GetPoint2dAt(i), pl.GetBulgeAt(i));
                break;
            case Xline x: Add(sb, x.BasePoint, x.UnitDir); break;
            case Ray r: Add(sb, r.BasePoint, r.UnitDir); break;
            case Curve c: Add(sb, c.StartPoint, c.EndPoint); break;
        }

        try
        {
            var x = e.GeometricExtents;
            Add(sb, x.MinPoint, x.MaxPoint);
        }
        catch
        {
            sb.Append("|?");
        }

        return sb.ToString();
    }

    private static void Add(StringBuilder sb, params object?[] values)
    {
        foreach (var v in values)
        {
            sb.Append('|');
            sb.Append(v switch
            {
                double d => d.ToString("0.###", CultureInfo.InvariantCulture),
                Point3d p => string.Create(CultureInfo.InvariantCulture, $"{p.X:0.###},{p.Y:0.###},{p.Z:0.###}"),
                Point2d p => string.Create(CultureInfo.InvariantCulture, $"{p.X:0.###},{p.Y:0.###}"),
                Vector3d p => string.Create(CultureInfo.InvariantCulture, $"{p.X:0.###},{p.Y:0.###},{p.Z:0.###}"),
                Scale3d s => string.Create(CultureInfo.InvariantCulture, $"{s.X:0.###},{s.Y:0.###},{s.Z:0.###}"),
                null => "",
                _ => Convert.ToString(v, CultureInfo.InvariantCulture),
            });
        }
    }
}

/// <summary>
///     Notices a commit or a rollback whose run was undone (a dryRun request, <c>U</c>): the engine cannot see the flag, the drawing
///     can — <see cref="UndoRule"/> reads the handles class by class. A committed set whose work is gone is pending again; a rolled-back
///     set whose work is back is committed again; a rolled-back set whose undo held may release its snapshots.
/// </summary>
public static class ChangeSetVerifier
{
    public const string CommitUndoneNote = "the commit was rolled back by its request (dryRun) or undone: the set is pending again";
    public const string RollbackUndoneNote = "the rollback was rolled back by its request (dryRun) or undone: the set is committed again, its undo still available";

    public static void Verify(Database db, Transaction tr, ChangeSetLedger ledger, ChangeSet set)
    {
        if (set.Commit is not { } c || !c.Touched) return;
        switch (set.State)
        {
            case ChangeSetState.Committed when UndoRule.WorkIsGone(Evidence(db, tr, c)):
                ledger.RevertToPending(set, CommitUndoneNote);
                break;
            case ChangeSetState.RolledBack when c.Snapshots is not null || c.CreatedHandles.Count + c.DeletedHandles.Count > 0:
                if (UndoRule.WorkIsGone(Evidence(db, tr, c))) ledger.ConfirmRolledBack(set);
                else if (c.Snapshots is not null || c.ModifiedHandles.Count == 0) ledger.RevertToCommitted(set, RollbackUndoneNote); // without snapshots a modified set cannot be undone again: leave it
                break;
        }
    }

    /// <summary>What the drawing shows for each handle the commit touched; an aborted run leaves what it appended as erased objects (handles resolve until the drawing closes): gone either way.</summary>
    public static UndoEvidence Evidence(Database db, Transaction tr, CommitRecord c)
    {
        var bag = c.Snapshots as SnapshotBag;
        var modifiedAsSnapshot = 0;
        foreach (var handle in c.ModifiedHandles)
            if (bag is not null && bag.Clones.TryGetValue(handle, out var clone) && Unchanged(db, tr, handle, clone)) modifiedAsSnapshot++;
        return new UndoEvidence(c.CreatedHandles.Count, c.CreatedHandles.Count(h => !Exists(db, h) || IsErased(db, h)),
            c.DeletedHandles.Count, c.DeletedHandles.Count(h => Exists(db, h) && !IsErased(db, h)),
            bag is null ? 0 : c.ModifiedHandles.Count, modifiedAsSnapshot);
    }

    public static bool Exists(Database db, string handle)
    {
        try { return HandleResolver.Normalize(handle) is { } h && db.TryGetObjectId(new Handle(long.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture)), out var id) && !id.IsNull; }
        catch { return false; }
    }

    public static bool IsErased(Database db, string handle)
    {
        try { return HandleResolver.Normalize(handle) is { } h && db.TryGetObjectId(new Handle(long.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture)), out var id) && id.IsErased; }
        catch { return false; }
    }

    private static bool Unchanged(Database db, Transaction tr, string handle, Entity clone)
    {
        try
        {
            var id = HandleResolver.Resolve(db, handle, out _);
            if (id.IsNull) return false;
            var live = (Entity)tr.GetObject(id, OpenMode.ForRead);
            return SnapshotBag.Fingerprint(live) == SnapshotBag.Fingerprint(clone);
        }
        catch
        {
            return false;
        }
    }
}
