int limit = Math.Clamp(args.Int("limit", 200), 1, 2000);

var selection = ed.SelectImplied();
if (selection.Status != PromptStatus.OK || selection.Value == null || selection.Value.Count == 0)
{
    log("nothing is selected");
    return new List<object>();
}

var items = new List<object>();
foreach (var id in selection.Value.GetObjectIds().Take(limit))
{
    ct.ThrowIfCancellationRequested();
    if (tr.GetObject(id, OpenMode.ForRead) is not Entity entity) continue;
    items.Add(new { handle = entity.Handle.ToString(), type = id.ObjectClass.DxfName ?? id.ObjectClass.Name, layer = entity.Layer });
}
log($"{selection.Value.Count} selected, returning {items.Count}");
return items;
