string pattern = args.Str("pattern", "*") ?? "*";
bool includeAnonymous = args.Bool("includeAnonymous", false);
var regex = new System.Text.RegularExpressions.Regex(
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var blocks = new List<object>();
foreach (ObjectId id in blockTable)
{
    ct.ThrowIfCancellationRequested();
    var record = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
    if (record.IsLayout || record.IsDependent) continue;
    if (record.IsAnonymous && !includeAnonymous) continue;
    if (!regex.IsMatch(record.Name)) continue;

    var entityCount = 0;
    foreach (ObjectId entityId in record) entityCount++;

    blocks.Add(new
    {
        name = record.Name,
        entityCount,
        referenceCount = record.GetBlockReferenceIds(true, false).Count,
        hasAttributes = record.HasAttributeDefinitions,
        isAnonymous = record.IsAnonymous,
        isXref = record.IsFromExternalReference,
    });
}
log($"{blocks.Count} block definitions match '{pattern}'");
return blocks;
