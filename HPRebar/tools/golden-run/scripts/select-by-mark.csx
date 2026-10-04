// Golden-run selection — selects the fixture elements a feature runs on, so its command skips the pick prompt.
// Run through the Revit MCP: execute_revit_code, transaction "none".
// args: runDir (the document must live under it — never a user model), marks = list of Mark values.

string runDir = args.Str("runDir", "");
string docPath = doc.PathName ?? "";
if (runDir.Length == 0 || !docPath.StartsWith(runDir, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Refusing to select in '" + docPath + "': it is not under the golden-run folder '" + runDir + "'.");
}

var marks = args.Strings("marks");
if (marks.Count == 0)
{
    throw new ArgumentException("Give at least one Mark in 'marks'.");
}

var byMark = new FilteredElementCollector(doc)
    .WhereElementIsNotElementType()
    .ToElements()
    .Select(e => new { Element = e, Mark = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "" })
    .Where(x => marks.Contains(x.Mark))
    .ToList();

var missing = marks.Where(m => byMark.All(x => x.Mark != m)).ToList();
if (missing.Count > 0)
{
    throw new ArgumentException("Marks not found in the fixture: " + string.Join(", ", missing));
}

uidoc.Selection.SetElementIds(byMark.Select(x => x.Element.Id).ToList());

return new
{
    selected = byMark.Count,
    marks = byMark.Select(x => x.Mark).OrderBy(m => m, StringComparer.Ordinal).ToList()
};
