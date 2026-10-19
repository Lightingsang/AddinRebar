namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using Serilog;

/// <summary>
///     Level 2 Safety: Manages Grasshopper canvas mutation ledger and undo boundaries.
///     Enforces GH_UndoRecord push, native actions, and compensating rollback for dryRun=true or failures.
/// </summary>
public sealed class CanvasUndoScope : ICanvasUndoScope
{
    private readonly GH_Document _document;
    private readonly GH_UndoRecord _undoRecord;
    private readonly List<Guid> _addedGuids = new();
    private readonly List<RemovedComponentSnapshot> _removedSnapshots = new();
    private readonly List<(IGH_Param Target, IGH_Param Source)> _disconnectedWires = new();
    private readonly List<(IGH_Param Target, IGH_Param Source)> _connectedWires = new();
    private HashSet<Guid> _baselineGuids = new();
    private HashSet<Guid> SafeBaselineGuids => _baselineGuids ??= new();
    private List<string> _rollbackErrors = new();
    private bool _isCompleted;
    private bool _eventsAttached;

    private List<string> SafeErrors => _rollbackErrors ??= new();
    public bool IsCommitted => _isCompleted;
    public bool RolledBackCleanly => _isCompleted && SafeErrors.Count == 0;
    public IReadOnlyList<string> RollbackErrors => SafeErrors;

    public CanvasUndoScope(GH_Document document, string actionName = "MCP Grasshopper Canvas Action")
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _undoRecord = new GH_UndoRecord(actionName);
        if (_document.Objects != null)
        {
            foreach (var obj in _document.Objects)
                if (obj != null) SafeBaselineGuids.Add(obj.InstanceGuid);
        }
        try { _document.UndoServer?.PushUndoRecord(_undoRecord); }
        catch (Exception ex) { Log.Debug(ex, "Failed to push undo record onto GH_Document.UndoServer"); }

        try
        {
            _document.ObjectsAdded += OnObjectsAdded;
            _document.ObjectsDeleted += OnObjectsDeleted;
            _eventsAttached = true;
        }
        catch (Exception ex) { Log.Debug(ex, "Could not attach document object listeners to GH_Document"); }
    }

    private void DetachEvents()
    {
        if (!_eventsAttached || _document == null) return;
        try
        {
            _document.ObjectsAdded -= OnObjectsAdded;
            _document.ObjectsDeleted -= OnObjectsDeleted;
        }
        catch (Exception ex) { Log.Debug(ex, "Error detaching object listeners from GH_Document"); }
        finally { _eventsAttached = false; }
    }

    private void OnObjectsAdded(object sender, GH_DocObjectEventArgs e)
    {
        if (e?.Objects == null) return;
        foreach (var obj in e.Objects)
        {
            if (obj != null && _addedGuids != null && !_addedGuids.Contains(obj.InstanceGuid))
            {
                _addedGuids.Add(obj.InstanceGuid);
                AddAction(new GH_AddObjectAction(obj));
            }
        }
    }

    private void OnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
    {
        if (e?.Objects == null) return;
        foreach (var obj in e.Objects) if (obj != null) TrackRemovedObject(obj);
    }

    private void AddAction(IGH_UndoAction action)
    {
        try { _undoRecord?.AddAction(action); }
        catch (Exception ex) { Log.Debug(ex, "Failed adding undo action to GH_UndoRecord"); }
    }

    public void TrackAddedObject(Guid instanceGuid)
    {
        if (_addedGuids != null && !_addedGuids.Contains(instanceGuid))
        {
            _addedGuids.Add(instanceGuid);
            var obj = _document?.FindObject(instanceGuid, true);
            if (obj != null) AddAction(new GH_AddObjectAction(obj));
        }
    }

    public void TrackRemovedObject(IGH_DocumentObject obj)
    {
        if (obj == null) return;
        if (_addedGuids != null && _addedGuids.Contains(obj.InstanceGuid) && !SafeBaselineGuids.Contains(obj.InstanceGuid))
            return;

        SafeBaselineGuids.Add(obj.InstanceGuid);
        if (_removedSnapshots != null && !_removedSnapshots.Any(s => s.InstanceGuid == obj.InstanceGuid))
        {
            var severedWires = CaptureObjectWires(obj);
            var archiveBytes = CaptureArchiveSnapshot(obj);
            _removedSnapshots.Add(new RemovedComponentSnapshot(obj, severedWires, archiveBytes));
            try { AddAction(new GH_RemoveObjectAction(obj)); }
            catch (Exception ex) { Log.Debug(ex, "Failed creating GH_RemoveObjectAction for {Guid}", obj.InstanceGuid); }
        }
    }

    public void TrackModifiedObject(IGH_DocumentObject obj)
    {
        if (obj == null) return;
        SafeBaselineGuids.Add(obj.InstanceGuid);
        if (_removedSnapshots != null && !_removedSnapshots.Any(s => s.InstanceGuid == obj.InstanceGuid))
        {
            var severedWires = CaptureObjectWires(obj);
            var archiveBytes = CaptureArchiveSnapshot(obj);
            _removedSnapshots.Add(new RemovedComponentSnapshot(obj, severedWires, archiveBytes));
        }
    }

    public void TrackConnectedWire(IGH_Param target, IGH_Param source)
    {
        _connectedWires?.Add((target, source));
        try { AddAction(new GH_WireAction(target)); }
        catch (Exception ex) { Log.Debug(ex, "Failed creating GH_WireAction"); }
    }

    public void TrackDisconnectedWire(IGH_Param target, IGH_Param source)
    {
        _disconnectedWires?.Add((target, source));
        try { AddAction(new GH_WireAction(target)); }
        catch (Exception ex) { Log.Debug(ex, "Failed creating GH_WireAction"); }
    }

    public CanvasRollbackResult Rollback()
    {
        if (_isCompleted)
            return SafeErrors.Count == 0 ? CanvasRollbackResult.Clean() : CanvasRollbackResult.Failed(SafeErrors);

        DetachEvents();
        try
        {
            RollbackWires();
            RollbackAddedComponents();
            RollbackRemovedComponents();
            PopNativeUndoRecord();

            _document?.NewSolution(false);
            if (SafeErrors.Count == 0)
                Log.Information("CanvasUndoScope: Rolled back {AddedCount} added, {RemovedCount} removed objects cleanly", _addedGuids?.Count ?? 0, _removedSnapshots?.Count ?? 0);
            else
                Log.Warning("CanvasUndoScope: Rollback completed with {ErrorCount} partial restore error(s)", SafeErrors.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CanvasUndoScope: Critical unhandled failure during rollback execution");
            SafeErrors.Add($"Critical rollback failure: {ex.Message}");
        }
        finally { _isCompleted = true; }

        return SafeErrors.Count == 0 ? CanvasRollbackResult.Clean() : CanvasRollbackResult.Failed(SafeErrors);
    }

    private void RollbackWires() { RollbackConnectedWires(); RollbackDisconnectedWires(); }

    private void RollbackConnectedWires()
    {
        if (_connectedWires == null) return;
        foreach (var (target, source) in _connectedWires)
        {
            try { target.RemoveSource(source); }
            catch (Exception ex) { Log.Warning(ex, "Failed to disconnect wire"); SafeErrors.Add($"Disconnect wire failed ({target.InstanceGuid} -> {source.InstanceGuid}): {ex.Message}"); }
        }
    }

    private void RollbackDisconnectedWires()
    {
        if (_disconnectedWires == null) return;
        foreach (var (target, source) in _disconnectedWires)
        {
            try { if (!target.Sources.Contains(source)) target.AddSource(source); }
            catch (Exception ex) { Log.Warning(ex, "Failed to restore wire"); SafeErrors.Add($"Restore wire failed ({target.InstanceGuid} <- {source.InstanceGuid}): {ex.Message}"); }
        }
    }

    private void RollbackAddedComponents()
    {
        if (_document == null || _addedGuids == null) return;
        foreach (var guid in _addedGuids)
        {
            if (SafeBaselineGuids.Contains(guid)) continue;
            try
            {
                var obj = _document.FindObject(guid, true);
                if (obj != null) _document.RemoveObject(obj, false);
            }
            catch (Exception ex) { Log.Warning(ex, "Failed to remove component"); SafeErrors.Add($"Remove added component failed ({guid}): {ex.Message}"); }
        }
    }

    private void RollbackRemovedComponents()
    {
        if (_document == null || _removedSnapshots == null) return;
        for (var i = _removedSnapshots.Count - 1; i >= 0; i--)
        {
            var snapshot = _removedSnapshots[i];
            if (_addedGuids != null && _addedGuids.Contains(snapshot.InstanceGuid) && !SafeBaselineGuids.Contains(snapshot.InstanceGuid))
                continue;

            var existing = _document.FindObject(snapshot.InstanceGuid, true);
            if (existing != null && existing != snapshot.Object)
            {
                try { _document.RemoveObject(existing, false); existing = null; }
                catch (Exception ex) { Log.Warning(ex, "Failed to remove replaced object instance"); }
            }

            if (snapshot.ArchiveBytes != null)
            {
                try
                {
                    var chunk = new GH_LooseChunk("ObjectSnapshot");
                    chunk.Deserialize_Binary(snapshot.ArchiveBytes);
                    snapshot.Object.Read(chunk);
                }
                catch (Exception ex) { Log.Warning(ex, "Failed deserializing archive bytes"); SafeErrors.Add($"Deserialize archive failed ({snapshot.InstanceGuid}): {ex.Message}"); }
            }

            if (existing == null)
            {
                try { _document.AddObject(snapshot.Object, false); }
                catch (Exception ex) { Log.Warning(ex, "Failed to re-add removed object"); SafeErrors.Add($"Re-add removed object failed ({snapshot.InstanceGuid}): {ex.Message}"); }
            }
        }

        foreach (var snapshot in _removedSnapshots)
        {
            if (_addedGuids != null && _addedGuids.Contains(snapshot.InstanceGuid) && !SafeBaselineGuids.Contains(snapshot.InstanceGuid))
                continue;
            foreach (var (target, source) in snapshot.SeveredWires)
            {
                try { if (!target.Sources.Contains(source)) target.AddSource(source); }
                catch (Exception ex) { Log.Warning(ex, "Failed restoring wire"); SafeErrors.Add($"Restore severed wire failed ({snapshot.InstanceGuid}): {ex.Message}"); }
            }
        }
    }

    private void PopNativeUndoRecord()
    {
        try { if (_document?.UndoServer != null && _undoRecord != null) _document.UndoServer.RemoveRecord(_undoRecord.Guid); }
        catch (Exception ex) { Log.Debug(ex, "Failed removing undo record from UndoServer during rollback"); }
    }

    private static List<(IGH_Param Target, IGH_Param Source)> CaptureObjectWires(IGH_DocumentObject obj)
    {
        var wires = new List<(IGH_Param Target, IGH_Param Source)>();
        if (obj is IGH_Component comp)
        {
            foreach (var input in comp.Params.Input) foreach (var source in input.Sources) wires.Add((input, source));
            foreach (var output in comp.Params.Output) foreach (var recipient in output.Recipients) wires.Add((recipient, output));
        }
        else if (obj is IGH_Param param)
        {
            foreach (var source in param.Sources) wires.Add((param, source));
            foreach (var recipient in param.Recipients) wires.Add((recipient, param));
        }
        return wires;
    }

    private static byte[]? CaptureArchiveSnapshot(IGH_DocumentObject obj)
    {
        try
        {
            var chunk = new GH_LooseChunk("ObjectSnapshot");
            if (obj.Write(chunk)) return chunk.Serialize_Binary();
        }
        catch (Exception ex) { Log.Debug(ex, "Failed capturing binary archive chunk for object {Guid}", obj.InstanceGuid); }
        return null;
    }

    public void Commit() { DetachEvents(); _isCompleted = true; }

    public void Dispose() { DetachEvents(); if (!_isCompleted) Rollback(); }
}
