import subprocess, json, time

exe = r"HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

def run_update_batch(target_group, dry_run=False):
    csharp = """
    var group = args.Str("group", "DK.b");
    var collector = new Autodesk.Revit.DB.FilteredElementCollector(doc)
        .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
        .WhereElementIsNotElementType()
        .ToElements();

    var tang1 = collector
        .Where(e => (e.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsValueString() ?? "") == "Tầng 1")
        .Cast<Autodesk.Revit.DB.FamilyInstance>()
        .ToList();

    var symbols = new Autodesk.Revit.DB.FilteredElementCollector(doc)
        .OfClass(typeof(Autodesk.Revit.DB.FamilySymbol))
        .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
        .Cast<Autodesk.Revit.DB.FamilySymbol>()
        .Where(s => s.FamilyName.Contains("ConcreteRectangular"))
        .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

    var sym100x300 = symbols["B_100x300"];
    var sym200x300 = symbols["B_200x300"];

    var updated = new List<string>();

    foreach (var b in tang1)
    {
        var mark = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
        bool match = false;
        Autodesk.Revit.DB.FamilySymbol targetSym = null;
        double zOffFt = 0.0;

        if (group == "DK.b" && mark.Contains("DK.b"))
        {
            match = true;
            targetSym = sym200x300;
        }
        else if (group == "DK.a'" && mark.Contains("DK.a'"))
        {
            match = true;
            targetSym = sym100x300;
            zOffFt = -900.0 / 304.8;
        }
        else if (group == "DK.a" && mark.Contains("DK.a") && !mark.Contains("DK.a'"))
        {
            match = true;
            targetSym = sym100x300;
        }

        if (match && targetSym != null)
        {
            string oldT = b.Symbol.Name;
            if (b.GetTypeId() != targetSym.Id)
            {
                b.ChangeTypeId(targetSym.Id);
            }

            if (group == "DK.a'")
            {
                var pZOff = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Z_OFFSET_VALUE);
                if (pZOff != null && !pZOff.IsReadOnly) pZOff.Set(zOffFt);
            }

            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(b, 0);
            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(b, 1);

            updated.Add(b.Id.Value.ToString() + " | " + mark + " | " + oldT + " -> " + targetSym.Name);
        }
    }

    return new {
        group = group,
        count = updated.Count,
        beams = updated
    };
    """

    proc = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def send(msg):
        proc.stdin.write((json.dumps(msg) + "\n").encode('utf-8'))
        proc.stdin.flush()
        while True:
            line = proc.stdout.readline()
            if not line: break
            try:
                data = json.loads(line.decode('utf-8'))
                if data.get("id") == msg.get("id"):
                    return data
            except:
                pass

    send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0"}}})
    t0 = time.time()
    res = send({
        "jsonrpc": "2.0",
        "id": 2,
        "method": "tools/call",
        "params": {
            "name": "execute_dynamo_code",
            "arguments": {
                "code": csharp,
                "args": {"group": target_group},
                "dryRun": dry_run,
                "timeoutSeconds": 60
            }
        }
    })
    elapsed = time.time() - t0
    proc.terminate()
    return res, elapsed

if __name__ == "__main__":
    for grp in ["DK.b", "DK.a'", "DK.a"]:
        print(f"=== UPDATING GROUP: {grp} ===")
        res, elapsed = run_update_batch(grp, dry_run=False)
        raw = res["result"]["content"][0]["text"]
        data = json.loads(raw)
        print(f"  Result in {elapsed:.2f}s:")
        print("  IsError:", data.get("isError"))
        if data.get("isError"):
            print("  Message:", data.get("message"))
        else:
            val = data.get("value")
            print(f"  Successfully updated {val.get('count')} beams in group {val.get('group')}!")
            for bm in val.get('beams', []):
                print(f"    {bm}")
