namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetMepAndRebarScript() => """
                    else if (category == "MepFixture")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingInst = doc.GetElement(targetUniqueId) as FamilyInstance;
                            if (existingInst != null)
                            {
                                string reqSym = res.Str("typeName", string.Empty);
                                bool modified = false;
                                if (!string.IsNullOrEmpty(reqSym) && fixtureSymbols.TryGetValue(reqSym, out var newSym) && existingInst.Symbol?.Id != newSym.Id)
                                {
                                    existingInst.Symbol = newSym;
                                    modified = true;
                                }
                                subTx.Commit();
                                records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = modified ? "Updated" : "NoOp", ["targetUniqueId"] = existingInst.UniqueId, ["targetElementId"] = (long)existingInst.Id.Value });
                                continue;
                            }
                        }

                        string lvlName = item.Require("targetLevelName");
                        if (!existingLevels.TryGetValue(lvlName, out var lvl))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_LEVEL_NOT_FOUND", ["errorReason"] = $"Level '{lvlName}' not found." });
                            continue;
                        }

                        string symName = res.Str("typeName", string.Empty);
                        if (string.IsNullOrEmpty(symName) || !fixtureSymbols.TryGetValue(symName, out var sym))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_SYMBOL_NOT_FOUND", ["errorReason"] = $"Plumbing fixture symbol '{symName}' not found in document." });
                            continue;
                        }

                        if (!sym.IsActive) sym.Activate();
                        var loc = geom.Obj("location");
                        var pt = ToXyz(loc.Double("x"), loc.Double("y"), loc.Double("z", 0.0));
                        var inst = doc.Create.NewFamilyInstance(pt, sym, lvl, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        var pComments = inst.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        subTx.Commit();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Created", ["targetUniqueId"] = inst.UniqueId, ["targetElementId"] = (long)inst.Id.Value });
                    }
                    else if (category == "PipeRun")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingPipe = doc.GetElement(targetUniqueId) as Autodesk.Revit.DB.Plumbing.Pipe;
                            if (existingPipe != null)
                            {
                                subTx.Commit();
                                records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "NoOp", ["targetUniqueId"] = existingPipe.UniqueId, ["targetElementId"] = (long)existingPipe.Id.Value });
                                continue;
                            }
                        }

                        string lvlName = item.Require("targetLevelName");
                        if (!existingLevels.TryGetValue(lvlName, out var lvl))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_LEVEL_NOT_FOUND", ["errorReason"] = $"Level '{lvlName}' not found." });
                            continue;
                        }

                        string reqSysType = res.Str("familyName", item.Obj("parameters").Str("SystemType", "Domestic Cold Water"));
                        string reqPipeType = res.Str("typeName", item.Obj("parameters").Str("PipeType", "Standard"));

                        Autodesk.Revit.DB.Plumbing.PipingSystemType pipingSysType = null;
                        if (!string.IsNullOrEmpty(reqSysType)) pipingSysTypes.TryGetValue(reqSysType, out pipingSysType);

                        Autodesk.Revit.DB.Plumbing.PipeType pipeType = null;
                        if (!string.IsNullOrEmpty(reqPipeType)) pipeTypes.TryGetValue(reqPipeType, out pipeType);

                        if (pipingSysType == null || pipeType == null)
                        {
                            subTx.RollBack();
                            string missing = pipingSysType == null && pipeType == null
                                ? $"PipingSystemType '{reqSysType}' and PipeType '{reqPipeType}'"
                                : (pipingSysType == null ? $"PipingSystemType '{reqSysType}'" : $"PipeType '{reqPipeType}'");

                            records.Add(new Dictionary<string, object>
                            {
                                ["sourceKey"] = sourceKey,
                                ["category"] = category,
                                ["mark"] = mark,
                                ["status"] = "Failed",
                                ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
                                ["errorReason"] = $"{missing} not found in document."
                            });
                            continue;
                        }

                        var pts = geom.List("points");
                        XYZ p0 = XYZ.Zero;
                        XYZ p1 = XYZ.Zero;
                        if (pts != null && pts.Count >= 2)
                        {
                            p0 = ToXyz(pts[0].Double("x"), pts[0].Double("y"), pts[0].Double("z", 0));
                            p1 = ToXyz(pts[1].Double("x"), pts[1].Double("y"), pts[1].Double("z", 0));
                        }
                        else
                        {
                            var start = geom.Obj("start");
                            var end = geom.Obj("end");
                            p0 = ToXyz(start.Double("x"), start.Double("y"), start.Double("z", 0));
                            p1 = ToXyz(end.Double("x"), end.Double("y"), end.Double("z", 0));
                        }

                        if (p0.DistanceTo(p1) < 0.01)
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_GEOMETRY_DEGENERATE", ["errorReason"] = "Pipe run length is degenerate." });
                            continue;
                        }

                        var pipe = Autodesk.Revit.DB.Plumbing.Pipe.Create(doc, pipingSysType.Id, pipeType.Id, lvl.Id, p0, p1);
                        var pComments = pipe.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        subTx.Commit();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Created", ["targetUniqueId"] = pipe.UniqueId, ["targetElementId"] = (long)pipe.Id.Value });
                    }
                    else if (category == "Rebar")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingRebar = doc.GetElement(targetUniqueId) as Autodesk.Revit.DB.Structure.Rebar;
                            if (existingRebar != null)
                            {
                                subTx.Commit();
                                records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "NoOp", ["targetUniqueId"] = existingRebar.UniqueId, ["targetElementId"] = (long)existingRebar.Id.Value });
                                continue;
                            }
                        }

                        string hostKey = item.Str("hostElementKey", string.Empty);
                        Element host = null;
                        if (!string.IsNullOrEmpty(hostKey))
                        {
                            createdElements.TryGetValue(hostKey, out host);
                            if (host == null && createdWalls.TryGetValue(hostKey, out var hw)) host = hw;
                            if (host == null)
                            {
                                host = new FilteredElementCollector(doc)
                                    .WhereElementIsNotElementType()
                                    .FirstOrDefault(e => e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == hostKey);
                            }
                        }

                        string barTypeName = res.Require("typeName");
                        if (!rebarBarTypes.TryGetValue(barTypeName, out var barType) &&
                            !rebarBarTypes.TryGetValue($"Rebar Bar {barTypeName}", out barType))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_SYMBOL_NOT_FOUND", ["errorReason"] = $"RebarBarType '{barTypeName}' not found in document." });
                            continue;
                        }

                        if (host == null)
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_HOST_NOT_FOUND", ["errorReason"] = $"Host element '{hostKey}' not found in document." });
                            continue;
                        }

                        var bbox = host.get_BoundingBox(null);
                        var origin = bbox != null ? bbox.Min : XYZ.Zero;
                        double lenFt = Ft(res.Obj("dimensionParametersMm").Double("Length", 3000.0));
                        var p0 = new XYZ(origin.X + Ft(30), origin.Y + Ft(30), origin.Z);
                        var p1 = new XYZ(p0.X, p0.Y, p0.Z + lenFt);
                        var curves = new List<Curve> { Line.CreateBound(p0, p1) };
                        var normal = XYZ.BasisX;

                        var rebar = Autodesk.Revit.DB.Structure.Rebar.CreateFromCurves(
                            doc,
                            Autodesk.Revit.DB.Structure.RebarStyle.Standard,
                            barType,
                            null,
                            null,
                            host,
                            normal,
                            curves,
                            Autodesk.Revit.DB.Structure.RebarHookOrientation.Right,
                            Autodesk.Revit.DB.Structure.RebarHookOrientation.Right,
                            true,
                            true);

                        var pComments = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        subTx.Commit();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Created", ["targetUniqueId"] = rebar.UniqueId, ["targetElementId"] = (long)rebar.Id.Value });
                    }
                    else
                    {
                        subTx.RollBack();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_UNSUPPORTED_CATEGORY", ["errorReason"] = $"Category '{category}' is unsupported by BimPlanRunner." });
                    }
        """;
}
