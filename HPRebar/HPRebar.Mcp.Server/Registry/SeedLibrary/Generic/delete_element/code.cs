var requested = args.Longs("elementIds").Distinct().ToList();
if (requested.Count == 0) throw new ArgumentException("elementIds is empty.");
var existing = new List<ElementId>(); var missing = new List<long>();
foreach (var id in requested)
{
    var eid = new ElementId(id);
    if (doc.GetElement(eid) != null) existing.Add(eid); else missing.Add(id);
}
if (existing.Count == 0) throw new ArgumentException("None of the ids exist in the document.");

var summary = existing.Select(id => { var e = doc.GetElement(id); return new { id = id.Value, name = e.Name, category = e.Category?.Name }; }).ToList();
var deleted = doc.Delete(existing);

return new { requested = requested.Count, deleted = existing.Count, removedIncludingDependents = deleted.Count, missing, elements = summary };
