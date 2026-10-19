// ContinuousBeamRenumbering.cs
// Executable on Revit 2026 via HPGrasshopper MCP or HPRebar MCP
// Target: Mặt Bằng Dầm Sàn Tầng 2 (hoặc bất kỳ View/Level nào theo tham số args)

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

var targetViewName = args?.Str("ViewName", "MẶT BẰNG DẦM SÀN TẦNG 2") ?? "MẶT BẰNG DẦM SÀN TẦNG 2";
var floorSuffix = args?.Str("FloorSuffix", "T2") ?? "T2";
double minWidthMm = args?.Double("MinWidthMm", 200.0) ?? 200.0;
double toleranceMm = args?.Double("ToleranceMm", 60.0) ?? 60.0;
bool isDryRun = args?.Bool("DryRun", dryRun) ?? dryRun;

// Support dual execution: in HPGrasshopper (revitDoc) and HPRebar (doc)
var rDoc = (object)revitDoc as Document ?? (object)doc as Document;
if (rDoc == null)
{
    return "Error: No active Revit Document found.";
}

// Resolve target view
View targetView = null;
var allViews = new FilteredElementCollector(rDoc)
    .OfClass(typeof(View))
    .Cast<View>()
    .Where(v => !v.IsTemplate)
    .ToList();

var normalizedTarget = targetViewName.Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
foreach (var v in allViews)
{
    var n = v.Name.Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
    if (n.Contains(normalizedTarget) || normalizedTarget.Contains(n))
    {
        targetView = v;
        break;
    }
}

if (targetView == null)
{
    targetView = rDoc.ActiveView;
}

var allBeams = new FilteredElementCollector(rDoc, targetView.Id)
    .OfCategory(BuiltInCategory.OST_StructuralFraming)
    .WhereElementIsNotElementType()
    .Cast<FamilyInstance>()
    .ToList();

class BeamSpan {
    public FamilyInstance Instance { get; set; }
    public long Id { get; set; }
    public string Type { get; set; }
    public XYZ P0 { get; set; }
    public XYZ P1 { get; set; }
    public double Length { get; set; }
    public string OldName { get; set; }
    public int OldCount { get; set; }
    public string Dir { get; set; }
    public double PrimaryCoord { get; set; }
    public double MinSec { get; set; }
    public double MaxSec { get; set; }
}

var spans = new List<BeamSpan>();
int skippedSmallCount = 0;

foreach (var b in allBeams)
{
    var typeName = b.Symbol?.Name ?? "";
    if (!typeName.StartsWith("B_200") && !typeName.StartsWith("B_300"))
    {
        skippedSmallCount++;
        continue;
    }

    var curve = (b.Location as LocationCurve)?.Curve;
    if (curve == null) continue;

    var p0 = curve.GetEndPoint(0);
    var p1 = curve.GetEndPoint(1);

    var x0 = p0.X * 304.8;
    var y0 = p0.Y * 304.8;
    var x1 = p1.X * 304.8;
    var y1 = p1.Y * 304.8;

    var dx = Math.Abs(x1 - x0);
    var dy = Math.Abs(y1 - y0);

    string dir = "X";
    double primary = y0;
    double minSec = Math.Min(x0, x1);
    double maxSec = Math.Max(x0, x1);

    if (dy > dx)
    {
        dir = "Y";
        primary = x0;
        minSec = Math.Min(y0, y1);
        maxSec = Math.Max(y0, y1);
    }

    spans.Add(new BeamSpan {
        Instance = b,
        Id = b.Id.Value,
        Type = typeName,
        P0 = p0,
        P1 = p1,
        Length = curve.Length * 304.8,
        OldName = b.LookupParameter("STR_ElementName")?.AsString() ?? "",
        OldCount = b.LookupParameter("STR_ElementCount")?.AsInteger() ?? 0,
        Dir = dir,
        PrimaryCoord = primary,
        MinSec = minSec,
        MaxSec = maxSec
    });
}

// Group into collinear continuous spans
var continuousGroups = new List<List<BeamSpan>>();
var xSpans = spans.Where(s => s.Dir == "X").OrderBy(s => s.PrimaryCoord).ThenBy(s => s.MinSec).ToList();
var ySpans = spans.Where(s => s.Dir == "Y").OrderBy(s => s.PrimaryCoord).ThenBy(s => s.MinSec).ToList();

void GroupCollinear(List<BeamSpan> list)
{
    var visited = new HashSet<long>();
    foreach (var span in list)
    {
        if (visited.Contains(span.Id)) continue;
        var group = new List<BeamSpan> { span };
        visited.Add(span.Id);

        bool addedMore = true;
        while (addedMore)
        {
            addedMore = false;
            foreach (var other in list)
            {
                if (visited.Contains(other.Id)) continue;
                if (Math.Abs(other.PrimaryCoord - group[0].PrimaryCoord) < toleranceMm)
                {
                    bool touches = group.Any(g => 
                        Math.Abs(g.MinSec - other.MaxSec) < 100 ||
                        Math.Abs(g.MaxSec - other.MinSec) < 100 ||
                        (other.MinSec >= g.MinSec - 50 && other.MaxSec <= g.MaxSec + 50) ||
                        (g.MinSec >= other.MinSec - 50 && g.MaxSec <= other.MaxSec + 50)
                    );
                    if (touches)
                    {
                        group.Add(other);
                        visited.Add(other.Id);
                        addedMore = true;
                    }
                }
            }
        }
        continuousGroups.Add(group.OrderBy(g => g.MinSec).ToList());
    }
}

GroupCollinear(xSpans);
GroupCollinear(ySpans);

var beamAssignments = new Dictionary<long, (string Name, int Count)>();

// Assign X-beams
foreach (var g in continuousGroups.Where(g => g[0].Dir == "X"))
{
    var y = g[0].PrimaryCoord;
    var xMax = g.Max(x => x.MaxSec);

    string name = "";
    int count = 1;

    if (Math.Abs(y - 0) < 100) name = (xMax < 21100) ? $"DS1-{floorSuffix}" : $"DS2-{floorSuffix}";
    else if (Math.Abs(y - 2400) < 100) name = (xMax < 21100) ? $"DS3-{floorSuffix}" : $"DS4-{floorSuffix}";
    else if (Math.Abs(y - 4400) < 100) name = $"DS5-{floorSuffix}";
    else if (Math.Abs(y - 6100) < 100 || Math.Abs(y - 7200) < 100) { name = $"DS6-{floorSuffix}"; count = 2; }
    else if (Math.Abs(y - 6700) < 100) name = $"DS10-{floorSuffix}";
    else if (Math.Abs(y - 9800) < 100) name = (xMax < 21100) ? $"DS3A-{floorSuffix}" : $"DS4A-{floorSuffix}";

    foreach (var b in g)
    {
        beamAssignments[b.Id] = (name, count);
    }
}

// Assign Y-beams
foreach (var g in continuousGroups.Where(g => g[0].Dir == "Y"))
{
    var x = g[0].PrimaryCoord;
    var has300 = g.Any(b => b.Type.Contains("300"));

    if (Math.Abs(x - 45019) < 100)
    {
        foreach (var b in g) beamAssignments[b.Id] = ($"DS9-{floorSuffix}", 1);
    }
    else if (has300)
    {
        foreach (var b in g)
        {
            if (b.MinSec < -100 && b.MaxSec <= 50 && b.Type.Contains("200"))
            {
                beamAssignments[b.Id] = ($"DG2-{floorSuffix}", 1);
            }
            else
            {
                beamAssignments[b.Id] = ($"DS8-{floorSuffix}", 6);
            }
        }
    }
    else
    {
        foreach (var b in g)
        {
            beamAssignments[b.Id] = ($"DS7-{floorSuffix}", 7);
        }
    }
}

int updatedCount = 0;
class UpdateRecord {
    public long Id { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
}
var records = new List<UpdateRecord>();

foreach (var kvp in beamAssignments)
{
    var b = spans.First(s => s.Id == kvp.Key).Instance;
    var targetName = kvp.Value.Name;
    var targetCount = kvp.Value.Count;

    var pName = b.LookupParameter("STR_ElementName");
    var pCount = b.LookupParameter("STR_ElementCount");
    var pComments = b.LookupParameter("Comments");

    var curName = pName?.AsString() ?? "";
    var curCount = pCount?.AsInteger() ?? 0;
    var targetComment = $"{targetName} ({targetCount} nhịp)";
    var curComment = pComments?.AsString() ?? "";

    bool changed = (pName != null && !pName.IsReadOnly && curName != targetName) ||
                   (pCount != null && !pCount.IsReadOnly && curCount != targetCount) ||
                   (pComments != null && !pComments.IsReadOnly && curComment != targetComment);

    if (changed)
    {
        if (!isDryRun)
        {
            if (pName != null && !pName.IsReadOnly && curName != targetName)
            {
                pName.Set(targetName);
            }

            if (pCount != null && !pCount.IsReadOnly && curCount != targetCount)
            {
                pCount.Set(targetCount);
            }

            if (pComments != null && !pComments.IsReadOnly && curComment != targetComment)
            {
                pComments.Set(targetComment);
            }
        }
        updatedCount++;
    }

    records.Add(new UpdateRecord {
        Id = kvp.Key,
        Name = targetName,
        Count = targetCount
    });
}

var distribution = records.GroupBy(r => r.Name)
    .Select(g => new {
        Name = g.Key,
        CountValue = g.First().Count,
        TotalSpans = g.Count()
    })
    .OrderBy(x => x.Name)
    .ToList();

return new {
    Success = true,
    DryRun = isDryRun,
    TargetView = targetView.Name,
    TotalBeamsInView = allBeams.Count,
    SkippedSmallBeams = skippedSmallCount,
    ProcessedMainBeams = spans.Count,
    UpdatedBeamCount = updatedCount,
    Distribution = distribution
};
