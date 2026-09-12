double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
XYZ Point(HPRebar.McpBridge.Core.Scripting.ScriptArgs p) => new XYZ(Ft(p.Double("x")), Ft(p.Double("y")), Ft(p.Double("z")));
var created = new List<object>();
var warnings = new List<string>();

Reference AlignedReference(Element e, View view, XYZ direction, XYZ near)
{
    if (e is Grid || e is Level || e is ReferencePlane) return new Reference(e);
    var options = new Options { ComputeReferences = true, View = view };
    Reference best = null; double bestScore = double.MinValue;
    void Scan(GeometryElement geometry)
    {
        foreach (var obj in geometry)
        {
            if (obj is GeometryInstance gi) { Scan(gi.GetInstanceGeometry()); continue; }
            if (obj is not Solid solid || solid.Faces.Size == 0) continue;
            foreach (Face face in solid.Faces)
            {
                if (face is not PlanarFace pf || face.Reference == null) continue;
                if (Math.Abs(pf.FaceNormal.Z) > 0.9) continue;
                double alignment = Math.Abs(pf.FaceNormal.DotProduct(direction));
                if (alignment < 0.95) continue;
                double distance = Math.Abs((pf.Origin - near).DotProduct(pf.FaceNormal));
                double score = alignment - distance * 0.001;
                if (score > bestScore) { bestScore = score; best = face.Reference; }
            }
        }
    }
    var geometryElement = e.get_Geometry(options);
    if (geometryElement != null) Scan(geometryElement);
    return best;
}

Element NearestAt(XYZ point, View view)
{
    var candidates = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType()
        .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory> { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Grids, BuiltInCategory.OST_StructuralColumns, BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralFraming }))
        .ToElements();
    Element best = null; double bestDistance = Ft(500);
    foreach (var e in candidates)
    {
        double d = double.MaxValue;
        if (e is Grid grid) d = grid.Curve.Distance(point);
        else if (e.Location is LocationCurve lc) d = lc.Curve.Distance(point);
        else if (e.Location is LocationPoint lp) d = new XYZ(lp.Point.X, lp.Point.Y, point.Z).DistanceTo(point);
        if (d < bestDistance) { bestDistance = d; best = e; }
    }
    return best;
}

int index = 0;
foreach (var item in args.List("dimensions"))
{
    ct.ThrowIfCancellationRequested();
    long viewId = item.Long("viewId", -1);
    var view = (viewId > 0 ? doc.GetElement(new ElementId(viewId)) as View : null) ?? doc.ActiveView;
    var start = Point(item.Obj("startPoint")); var end = Point(item.Obj("endPoint"));
    if (start.DistanceTo(end) < Ft(10)) throw new ArgumentException($"dimensions[{index}]: start and end are less than 10 mm apart.");
    var direction = (end - start).Normalize();

    var elements = item.Longs("elementIds").Select(id => doc.GetElement(new ElementId(id))).Where(e => e != null).ToList();
    if (elements.Count == 0)
    {
        var a = NearestAt(start, view); var b = NearestAt(end, view);
        if (a == null || b == null || a.Id == b.Id) throw new InvalidOperationException($"dimensions[{index}]: could not auto-detect two different elements within 500 mm of the points — pass elementIds.");
        elements = new List<Element> { a, b };
    }

    var references = new ReferenceArray();
    foreach (var e in elements)
    {
        var r = AlignedReference(e, view, direction, start);
        if (r == null) { warnings.Add($"dimensions[{index}]: no face of element {e.Id.Value} is aligned with the dimension direction — skipped."); continue; }
        references.Append(r);
    }
    if (references.Size < 2) throw new InvalidOperationException($"dimensions[{index}]: fewer than two usable references.");

    XYZ lineStart = start, lineEnd = end;
    if (item.Has("linePoint"))
    {
        var lp = Point(item.Obj("linePoint"));
        var shift = lp - start; shift -= direction * shift.DotProduct(direction);
        lineStart = start + shift; lineEnd = end + shift;
    }
    else
    {
        var side = direction.CrossProduct(XYZ.BasisZ).Normalize() * Ft(1000);
        lineStart = start + side; lineEnd = end + side;
    }

    var dimension = doc.Create.NewDimension(view, Line.CreateBound(lineStart, lineEnd), references);
    long styleId = item.Long("dimensionStyleId", -1);
    if (styleId > 0 && doc.GetElement(new ElementId(styleId)) is DimensionType type) dimension.DimensionType = type;

    created.Add(new { id = dimension.Id.Value, view = view.Name, references = references.Size, value = dimension.ValueString ?? dimension.Value?.ToString(), elementIds = elements.Select(e => e.Id.Value).ToList() });
    index++;
}

return new { created = created.Count, dimensions = created, warnings };
