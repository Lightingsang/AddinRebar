import json, subprocess, sys, math
from collections import defaultdict

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Extract All Paired Beam Centerlines"):
    req_args = {
        "code": code,
        "transaction": "none",
        "dryRun": False,
        "timeoutSeconds": 60,
        "label": label
    }
    cmd = [
        sys.executable,
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\tools\mcp-call.py",
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe",
        "tools/call",
        "execute_autocad_code",
        json.dumps(req_args),
        "--out",
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_temp.json"
    ]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode != 0:
        print("Subprocess error:", p.stderr)
        return None
    with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_temp.json", "r", encoding="utf-8") as f:
        data = json.load(f)
    if "result" in data and "content" in data["result"]:
        txt = data["result"]["content"][0]["text"]
        try:
            return json.loads(txt)
        except:
            return txt
    return data

code = """
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    var btrMm = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);

    double shiftX = 49268.55;
    double shiftY = 4399.98;

    var hLines = new List<Tuple<double, double, double>>(); // y, x1, x2
    var vLines = new List<Tuple<double, double, double>>(); // x, y1, y2

    foreach (ObjectId id in btrMm)
    {
        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent is Line ln && ln.Layer == "A_Bao_DK_S_0.2" && ln.Length >= 300.0)
        {
            double x1 = ln.StartPoint.X + shiftX;
            double y1 = ln.StartPoint.Y + shiftY;
            double x2 = ln.EndPoint.X + shiftX;
            double y2 = ln.EndPoint.Y + shiftY;

            if (Math.Abs(y1 - y2) < 5.0)
            {
                double y = (y1 + y2) / 2.0;
                hLines.Add(Tuple.Create(y, Math.Min(x1, x2), Math.Max(x1, x2)));
            }
            else if (Math.Abs(x1 - x2) < 5.0)
            {
                double x = (x1 + x2) / 2.0;
                vLines.Add(Tuple.Create(x, Math.Min(y1, y2), Math.Max(y1, y2)));
            }
        }
    }

    var hBeams = new List<object>();
    for (int i = 0; i < hLines.Count; i++)
    {
        for (int j = i + 1; j < hLines.Count; j++)
        {
            double dy = Math.Abs(hLines[i].Item1 - hLines[j].Item1);
            if (dy >= 180.0 && dy <= 350.0)
            {
                double overlapX1 = Math.Max(hLines[i].Item2, hLines[j].Item2);
                double overlapX2 = Math.Min(hLines[i].Item3, hLines[j].Item3);
                if (overlapX2 - overlapX1 >= 400.0)
                {
                    double centerY = (hLines[i].Item1 + hLines[j].Item1) / 2.0;
                    hBeams.Add(new {
                        dir = "X",
                        y = Math.Round(centerY, 1),
                        x1 = Math.Round(overlapX1, 1),
                        x2 = Math.Round(overlapX2, 1),
                        len = Math.Round(overlapX2 - overlapX1, 1),
                        width = Math.Round(dy, 1)
                    });
                }
            }
        }
    }

    var vBeams = new List<object>();
    for (int i = 0; i < vLines.Count; i++)
    {
        for (int j = i + 1; j < vLines.Count; j++)
        {
            double dx = Math.Abs(vLines[i].Item1 - vLines[j].Item1);
            if (dx >= 180.0 && dx <= 350.0)
            {
                double overlapY1 = Math.Max(vLines[i].Item2, vLines[j].Item2);
                double overlapY2 = Math.Min(vLines[i].Item3, vLines[j].Item3);
                if (overlapY2 - overlapY1 >= 400.0)
                {
                    double centerX = (vLines[i].Item1 + vLines[j].Item1) / 2.0;
                    vBeams.Add(new {
                        dir = "Y",
                        x = Math.Round(centerX, 1),
                        y1 = Math.Round(overlapY1, 1),
                        y2 = Math.Round(overlapY2, 1),
                        len = Math.Round(overlapY2 - overlapY1, 1),
                        width = Math.Round(dx, 1)
                    });
                }
            }
        }
    }

    return new { hBeams = hBeams, vBeams = vBeams };
"""

print("Extracting beam pairs from CAD...")
res = run_cad_script(code)
val = res['value']
h_beams = val['hBeams']
v_beams = val['vBeams']
print(f"Extracted {len(h_beams)} H beam segments and {len(v_beams)} V beam segments.")

# Load beam summary tags
with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\gm_beams_plan_summary.json', 'r', encoding='utf-8') as f:
    gm_tags = json.load(f)

# Load verified beam specs
with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_22_grade_beams.json', 'r', encoding='utf-8') as f:
    gm_specs = {b['type']: b for b in json.load(f)['beams']}

print(f"Loaded {len(gm_tags)} GM tags.")

# Deduplicate and consolidate segments along each line
all_segments = []
for hb in h_beams:
    all_segments.append({
        'dir': 'X',
        'x1': hb['x1'], 'y1': hb['y'],
        'x2': hb['x2'], 'y2': hb['y'],
        'midX': (hb['x1'] + hb['x2'])/2,
        'midY': hb['y'],
        'width': hb['width']
    })

for vb in v_beams:
    all_segments.append({
        'dir': 'Y',
        'x1': vb['x'], 'y1': vb['y1'],
        'x2': vb['x'], 'y2': vb['y2'],
        'midX': vb['x'],
        'midY': (vb['y1'] + vb['y2'])/2,
        'width': vb['width']
    })

# Deduplicate overlapping segments on the same line
def deduplicate_segments(segs):
    unique = []
    for s in segs:
        # Check if already covered by an existing segment with same orientation and coords
        is_sub = False
        for u in unique:
            if s['dir'] == u['dir']:
                if s['dir'] == 'X' and abs(s['y1'] - u['y1']) < 5.0:
                    if u['x1'] - 5.0 <= s['x1'] and s['x2'] <= u['x2'] + 5.0:
                        is_sub = True
                        break
                    elif s['x1'] - 5.0 <= u['x1'] and u['x2'] <= s['x2'] + 5.0:
                        # s is larger, replace u
                        u['x1'] = s['x1']
                        u['x2'] = s['x2']
                        is_sub = True
                        break
                elif s['dir'] == 'Y' and abs(s['x1'] - u['x1']) < 5.0:
                    if u['y1'] - 5.0 <= s['y1'] and s['y2'] <= u['y2'] + 5.0:
                        is_sub = True
                        break
                    elif s['y1'] - 5.0 <= u['y1'] and u['y2'] <= s['y2'] + 5.0:
                        u['y1'] = s['y1']
                        u['y2'] = s['y2']
                        is_sub = True
                        break
        if not is_sub:
            unique.append(s)
    return unique

deduped = deduplicate_segments(all_segments)
print(f"Deduplicated to {len(deduped)} distinct beam segments.")

# Match each segment to the closest GM tag
matched_beams = []
unmatched_count = 0
for seg in deduped:
    best_dist = 1e9
    best_tag = None
    for tag in gm_tags:
        tx = tag['x']
        ty = tag['y']
        # Distance from point (tx, ty) to segment (x1, y1)-(x2, y2)
        if seg['dir'] == 'X':
            # Check projection onto X interval
            dx = max(0, seg['x1'] - tx, tx - seg['x2'])
            dy = abs(seg['y1'] - ty)
            dist = math.hypot(dx, dy)
        else:
            dy = max(0, seg['y1'] - ty, ty - seg['y2'])
            dx = abs(seg['x1'] - tx)
            dist = math.hypot(dx, dy)
            
        if dist < best_dist:
            best_dist = dist
            best_tag = tag

    if best_dist < 4000.0 and best_tag is not None:
        tname = best_tag['name']
        spec = gm_specs.get(tname, None)
        b_mm = spec['bMm'] if spec else 300
        h_mm = spec['hMm'] if spec else 400
        # If tag has explicit section
        if best_tag.get('section') == '(300x600)':
            b_mm, h_mm = 300, 600
        elif best_tag.get('section') == '(300x400)':
            b_mm, h_mm = 300, 400
        elif best_tag.get('section') == '(200x300)':
            b_mm, h_mm = 200, 300
        elif best_tag.get('section') == '(200x400)':
            b_mm, h_mm = 200, 400

        matched_beams.append({
            'name': tname,
            'bMm': b_mm,
            'hMm': h_mm,
            'x1': seg['x1'], 'y1': seg['y1'],
            'x2': seg['x2'], 'y2': seg['y2'],
            'len': round(math.hypot(seg['x2'] - seg['x1'], seg['y2'] - seg['y1']), 1),
            'tagDist': round(best_dist, 1)
        })
    else:
        unmatched_count += 1

print(f"Matched {len(matched_beams)} beam spans to GM tags (unmatched: {unmatched_count}).")

# Group by name
by_name = defaultdict(list)
for mb in matched_beams:
    by_name[mb['name']].append(mb)

print("Matched spans per beam type:")
for name, lst in sorted(by_name.items()):
    b = lst[0]
    print(f"  {name:<8}: {len(lst)} spans, b={b['bMm']}, h={b['hMm']}, avg len={sum(x['len'] for x in lst)/len(lst):.1f}mm")

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_extracted_beam_spans.json', 'w', encoding='utf-8') as f:
    json.dump(matched_beams, f, indent=2, ensure_ascii=False)
print("Saved cad_extracted_beam_spans.json")
