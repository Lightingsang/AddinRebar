namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetStructureScript() =>
        GetLevelsAndGridsScript() +
        GetColumnsScript() +
        GetBeamsScript();

    private static string GetLevelsAndGridsScript() => """
                    if (category == "Level")
                    {
                        double elevationM = geom.Double("elevationM");
                        double elevationFt = elevationM * (1000.0 / 304.8);
                        Level level;
                        bool created = false;

                        if (!existingLevels.TryGetValue(mark, out level))
                        {
                            level = Level.Create(doc, elevationFt);
                            level.Name = mark;
                            existingLevels[mark] = level;
                            created = true;
                        }

                        var pStory = level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY);
                        if (pStory != null && !pStory.IsReadOnly) pStory.Set(1);

                        bool createView = item.Bool("createAssociatedView", true);
                        if (createView && planType != null && !existingViewLevelIds.Contains(level.Id))
                        {
                            string viewName = mark;
                            if (existingViewNames.Contains(viewName)) viewName = $"{mark} - Plan";

                            var newView = ViewPlan.Create(doc, planType.Id, level.Id);
                            newView.Name = viewName;
                            existingViewLevelIds.Add(level.Id);
                            existingViewNames.Add(viewName);
                        }

                        subTx.Commit();
                        records.Add(new Dictionary<string, object>
                        {
                            ["sourceKey"] = sourceKey,
                            ["category"] = category,
                            ["mark"] = mark,
                            ["status"] = created ? "Created" : "NoOp",
                            ["targetUniqueId"] = level.UniqueId,
                            ["targetElementId"] = (long)level.Id.Value
                        });
                    }
                    else if (category == "Grid")
                    {
                        if (existingGrids.TryGetValue(mark, out var grid))
                        {
                            subTx.Commit();
                            records.Add(new Dictionary<string, object>
                            {
                                ["sourceKey"] = sourceKey,
                                ["category"] = category,
                                ["mark"] = mark,
                                ["status"] = "NoOp",
                                ["targetUniqueId"] = grid.UniqueId,
                                ["targetElementId"] = (long)grid.Id.Value
                            });
                            continue;
                        }

                        var start = geom.Obj("start");
                        var end = geom.Obj("end");
                        var p0 = ToXyz(start.Double("x"), start.Double("y"), 0);
                        var p1 = ToXyz(end.Double("x"), end.Double("y"), 0);

                        if (p0.DistanceTo(p1) < 0.001)
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object>
                            {
                                ["sourceKey"] = sourceKey,
                                ["category"] = category,
                                ["mark"] = mark,
                                ["status"] = "Failed",
                                ["errorCode"] = "ERR_GEOMETRY_DEGENERATE",
                                ["errorReason"] = "Grid length is less than 1.0mm."
                            });
                            continue;
                        }

                        var newGrid = Grid.Create(doc, Line.CreateBound(p0, p1));
                        newGrid.Name = mark;
                        existingGrids[mark] = newGrid;

                        subTx.Commit();
                        records.Add(new Dictionary<string, object>
                        {
                            ["sourceKey"] = sourceKey,
                            ["category"] = category,
                            ["mark"] = mark,
                            ["status"] = "Created",
                            ["targetUniqueId"] = newGrid.UniqueId,
                            ["targetElementId"] = (long)newGrid.Id.Value
                        });
                    }
        """;

    private static string GetBeamsScript() => """
                    else if (category == "Beam" || category == "GradeBeam")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingBeam = doc.GetElement(targetUniqueId) as FamilyInstance;
                            if (existingBeam != null)
                            {
                                string reqSym = res.Str("typeName", string.Empty);
                                bool modified = false;
                                if (!string.IsNullOrEmpty(reqSym) && beamSymbols.TryGetValue(reqSym, out var newSym) && existingBeam.Symbol?.Id != newSym.Id)
                                {
                                    existingBeam.Symbol = newSym;
                                    modified = true;
                                }

                                if (!string.IsNullOrEmpty(mark)) createdElements[mark] = existingBeam;
                                subTx.Commit();
                                records.Add(new Dictionary<string, object>
                                {
                                    ["sourceKey"] = sourceKey,
                                    ["category"] = category,
                                    ["mark"] = mark,
                                    ["status"] = modified ? "Updated" : "NoOp",
                                    ["targetUniqueId"] = existingBeam.UniqueId,
                                    ["targetElementId"] = (long)existingBeam.Id.Value
                                });
                                continue;
                            }
                        }

                        string lvlName = item.Require("targetLevelName");
                        if (!existingLevels.TryGetValue(lvlName, out var lvl))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object>
                            {
                                    ["sourceKey"] = sourceKey,
                                    ["category"] = category,
                                    ["mark"] = mark,
                                    ["status"] = "Failed",
                                    ["errorCode"] = "ERR_LEVEL_NOT_FOUND",
                                    ["errorReason"] = $"Level '{lvlName}' not found."
                            });
                            continue;
                        }

                        string symName = res.Require("typeName");
                        if (!beamSymbols.TryGetValue(symName, out var sym))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object>
                            {
                                    ["sourceKey"] = sourceKey,
                                    ["category"] = category,
                                    ["mark"] = mark,
                                    ["status"] = "Failed",
                                    ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
                                    ["errorReason"] = $"Beam symbol '{symName}' not found in document."
                            });
                            continue;
                        }

                        if (!sym.IsActive) sym.Activate();
                        var start = geom.Obj("start");
                        var end = geom.Obj("end");
                        var p0 = ToXyz(start.Double("x"), start.Double("y"), 0);
                        var p1 = ToXyz(end.Double("x"), end.Double("y"), 0);
                        var curve = Line.CreateBound(p0, p1);

                        var beamInst = doc.Create.NewFamilyInstance(curve, sym, lvl, Autodesk.Revit.DB.Structure.StructuralType.Beam);
                        var pComments = beamInst.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        if (!string.IsNullOrEmpty(mark)) createdElements[mark] = beamInst;
                        subTx.Commit();

                        records.Add(new Dictionary<string, object>
                        {
                            ["sourceKey"] = sourceKey,
                            ["category"] = category,
                            ["mark"] = mark,
                            ["status"] = "Created",
                            ["targetUniqueId"] = beamInst.UniqueId,
                            ["targetElementId"] = (long)beamInst.Id.Value
                        });
                    }
        """;
}
