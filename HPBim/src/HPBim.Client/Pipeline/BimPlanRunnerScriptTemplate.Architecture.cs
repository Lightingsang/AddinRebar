namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetArchitectureScript() => """
                    else if (category == "Wall")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingWall = doc.GetElement(targetUniqueId) as Wall;
                            if (existingWall != null)
                            {
                                string reqType = res.Str("typeName", string.Empty);
                                bool modified = false;
                                if (!string.IsNullOrEmpty(reqType) && wallTypes.TryGetValue(reqType, out var newWt) && existingWall.WallType?.Id != newWt.Id)
                                {
                                    existingWall.WallType = newWt;
                                    modified = true;
                                }
                                if (!string.IsNullOrEmpty(mark)) { createdWalls[mark] = existingWall; createdElements[mark] = existingWall; }
                                subTx.Commit();
                                records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = modified ? "Updated" : "NoOp", ["targetUniqueId"] = existingWall.UniqueId, ["targetElementId"] = (long)existingWall.Id.Value });
                                continue;
                            }
                        }

                        string lvlName = item.Require("targetLevelName");
                        if (!existingLevels.TryGetValue(lvlName, out var lvl))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_LEVEL_NOT_FOUND", ["errorReason"] = $"BaseLevel '{lvlName}' not found." });
                            continue;
                        }

                        string typeName = res.Require("typeName");
                        if (!wallTypes.TryGetValue(typeName, out var wt))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_SYMBOL_NOT_FOUND", ["errorReason"] = $"WallType '{typeName}' not found in document." });
                            continue;
                        }

                        var start = geom.Obj("start");
                        var end = geom.Obj("end");
                        var p0 = ToXyz(start.Double("x"), start.Double("y"), 0);
                        var p1 = ToXyz(end.Double("x"), end.Double("y"), 0);
                        var curve = Line.CreateBound(p0, p1);

                        double wallHeightFt = Ft(3000.0);
                        string topLvlName = item.Str("targetTopLevelName", string.Empty);
                        if (!string.IsNullOrEmpty(topLvlName) && existingLevels.TryGetValue(topLvlName, out var topLvl))
                        {
                            wallHeightFt = Math.Max(Ft(500.0), topLvl.Elevation - lvl.Elevation);
                        }

                        var wall = Wall.Create(doc, curve, wt.Id, lvl.Id, wallHeightFt, 0.0, false, false);
                        var pComments = wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        createdWalls[mark] = wall;
                        createdElements[mark] = wall;
                        subTx.Commit();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Created", ["targetUniqueId"] = wall.UniqueId, ["targetElementId"] = (long)wall.Id.Value });
                    }
                    else if (category == "Slab")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingFloor = doc.GetElement(targetUniqueId) as Floor;
                            if (existingFloor != null)
                            {
                                string reqType = res.Str("typeName", string.Empty);
                                bool modified = false;
                                if (!string.IsNullOrEmpty(reqType) && floorTypes.TryGetValue(reqType, out var newFt) && existingFloor.FloorType?.Id != newFt.Id)
                                {
                                    existingFloor.FloorType = newFt;
                                    modified = true;
                                }
                                if (!string.IsNullOrEmpty(mark)) createdElements[mark] = existingFloor;
                                subTx.Commit();
                                records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = modified ? "Updated" : "NoOp", ["targetUniqueId"] = existingFloor.UniqueId, ["targetElementId"] = (long)existingFloor.Id.Value });
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

                        string typeName = res.Require("typeName");
                        if (!floorTypes.TryGetValue(typeName, out var ft))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_SYMBOL_NOT_FOUND", ["errorReason"] = $"FloorType '{typeName}' not found in document." });
                            continue;
                        }

                        var outer = geom.List("outerLoop");
                        if (outer == null || outer.Count < 3)
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_GEOMETRY_DEGENERATE", ["errorReason"] = "Slab outer boundary has fewer than 3 vertices." });
                            continue;
                        }

                        var loop = new CurveLoop();
                        for (int i = 0; i < outer.Count; i++)
                        {
                            var cur = outer[i];
                            var nxt = outer[(i + 1) % outer.Count];
                            var p0 = ToXyz(cur.Double("x"), cur.Double("y"), 0);
                            var p1 = ToXyz(nxt.Double("x"), nxt.Double("y"), 0);
                            loop.Append(Line.CreateBound(p0, p1));
                        }

                        var floor = Floor.Create(doc, new List<CurveLoop> { loop }, ft.Id, lvl.Id);
                        var pComments = floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        if (!string.IsNullOrEmpty(mark)) createdElements[mark] = floor;
                        subTx.Commit();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Created", ["targetUniqueId"] = floor.UniqueId, ["targetElementId"] = (long)floor.Id.Value });
                    }
                    else if (category == "Opening")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingInst = doc.GetElement(targetUniqueId) as FamilyInstance;
                            if (existingInst != null)
                            {
                                string reqSym = res.Str("typeName", string.Empty);
                                bool modified = false;
                                if (!string.IsNullOrEmpty(reqSym) && doorWinSymbols.TryGetValue(reqSym, out var newSym) && existingInst.Symbol?.Id != newSym.Id)
                                {
                                    existingInst.Symbol = newSym;
                                    modified = true;
                                }
                                subTx.Commit();
                                records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = modified ? "Updated" : "NoOp", ["targetUniqueId"] = existingInst.UniqueId, ["targetElementId"] = (long)existingInst.Id.Value });
                                continue;
                            }
                        }

                        string hostKey = item.Str("hostElementKey", string.Empty);
                        if (string.IsNullOrEmpty(hostKey) || !createdWalls.TryGetValue(hostKey, out var hostWall))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_HOST_NOT_FOUND", ["errorReason"] = $"Host wall '{hostKey}' not found." });
                            continue;
                        }

                        string symName = res.Require("typeName");
                        if (!doorWinSymbols.TryGetValue(symName, out var sym))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_SYMBOL_NOT_FOUND", ["errorReason"] = $"Opening symbol '{symName}' not found in document." });
                            continue;
                        }

                        if (!sym.IsActive) sym.Activate();
                        var locCurve = hostWall.Location as LocationCurve;
                        if (locCurve?.Curve == null)
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_INVALID_GEOMETRY", ["errorReason"] = $"Host wall '{hostKey}' has no valid location curve." });
                            continue;
                        }

                        double distFt = Ft(geom.Double("distanceAlongWallMm") + (geom.Double("widthMm") / 2.0));
                        double param = Math.Clamp(distFt / locCurve.Curve.Length, 0.05, 0.95);
                        var centerPt = locCurve.Curve.Evaluate(param, true);
                        double sillFt = Ft(geom.Double("sillHeightMm"));
                        var pt = new XYZ(centerPt.X, centerPt.Y, centerPt.Z + sillFt);
                        var lvl = doc.GetElement(hostWall.LevelId) as Level;

                        var inst = doc.Create.NewFamilyInstance(pt, sym, hostWall, lvl, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        var pComments = inst.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        subTx.Commit();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Created", ["targetUniqueId"] = inst.UniqueId, ["targetElementId"] = (long)inst.Id.Value });
                    }
                    else if (category == "Room")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingRoom = doc.GetElement(targetUniqueId) as Autodesk.Revit.DB.Architecture.Room;
                            if (existingRoom != null)
                            {
                                bool modified = false;
                                if (!string.IsNullOrEmpty(mark) && existingRoom.Name != mark)
                                {
                                    existingRoom.Name = mark;
                                    modified = true;
                                }
                                subTx.Commit();
                                records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = modified ? "Updated" : "NoOp", ["targetUniqueId"] = existingRoom.UniqueId, ["targetElementId"] = (long)existingRoom.Id.Value });
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

                        var loc = geom.Obj("location");
                        var pt = new UV(Ft(loc.Double("x")), Ft(loc.Double("y")));
                        doc.Regenerate();
                        var room = doc.Create.NewRoom(lvl, pt);
                        if (room == null)
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_ROOM_CREATION_FAILED", ["errorReason"] = $"Room '{mark}' could not be placed at ({loc.Double("x")}, {loc.Double("y")})." });
                            continue;
                        }

                        room.Name = mark;
                        var pComments = room.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        subTx.Commit();
                        records.Add(new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Created", ["targetUniqueId"] = room.UniqueId, ["targetElementId"] = (long)room.Id.Value });
                    }
        """;
}
