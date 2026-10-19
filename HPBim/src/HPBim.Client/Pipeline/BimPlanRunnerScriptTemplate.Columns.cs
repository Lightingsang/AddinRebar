namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetColumnsScript() => """
                    else if (category == "Column")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingCol = doc.GetElement(targetUniqueId) as FamilyInstance;
                            if (existingCol != null)
                            {
                                string reqSym = res.Str("typeName", string.Empty);
                                bool modified = false;
                                if (!string.IsNullOrEmpty(reqSym) && columnSymbols.TryGetValue(reqSym, out var newSym) && existingCol.Symbol?.Id != newSym.Id)
                                {
                                    existingCol.Symbol = newSym;
                                    modified = true;
                                }

                                string topLvlUpdate = item.Str("targetTopLevelName", string.Empty);
                                if (!string.IsNullOrEmpty(topLvlUpdate) && existingLevels.TryGetValue(topLvlUpdate, out var topLvlMatch))
                                {
                                    var pTopParam = existingCol.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                                    if (pTopParam != null && !pTopParam.IsReadOnly && pTopParam.AsElementId() != topLvlMatch.Id)
                                    {
                                        pTopParam.Set(topLvlMatch.Id);
                                        modified = true;
                                    }
                                }

                                if (!string.IsNullOrEmpty(mark)) createdElements[mark] = existingCol;
                                subTx.Commit();
                                records.Add(new Dictionary<string, object>
                                {
                                    ["sourceKey"] = sourceKey,
                                    ["category"] = category,
                                    ["mark"] = mark,
                                    ["status"] = modified ? "Updated" : "NoOp",
                                    ["targetUniqueId"] = existingCol.UniqueId,
                                    ["targetElementId"] = (long)existingCol.Id.Value
                                });
                                continue;
                            }
                        }

                        string lvlName = item.Require("targetLevelName");
                        if (!existingLevels.TryGetValue(lvlName, out var baseLvl))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object>
                            {
                                ["sourceKey"] = sourceKey,
                                ["category"] = category,
                                ["mark"] = mark,
                                ["status"] = "Failed",
                                ["errorCode"] = "ERR_LEVEL_NOT_FOUND",
                                ["errorReason"] = $"BaseLevel '{lvlName}' not found."
                            });
                            continue;
                        }

                        string topLvlName = item.Str("targetTopLevelName", string.Empty);
                        Level topLvl = null;
                        if (!string.IsNullOrEmpty(topLvlName))
                        {
                            if (!existingLevels.TryGetValue(topLvlName, out topLvl))
                            {
                                subTx.RollBack();
                                records.Add(new Dictionary<string, object>
                                {
                                    ["sourceKey"] = sourceKey,
                                    ["category"] = category,
                                    ["mark"] = mark,
                                    ["status"] = "Failed",
                                    ["errorCode"] = "ERR_LEVEL_NOT_FOUND",
                                    ["errorReason"] = $"TopLevel '{topLvlName}' not found."
                                });
                                continue;
                            }
                        }

                        string symName = res.Require("typeName");
                        if (!columnSymbols.TryGetValue(symName, out var sym))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object>
                            {
                                ["sourceKey"] = sourceKey,
                                ["category"] = category,
                                ["mark"] = mark,
                                ["status"] = "Failed",
                                ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
                                ["errorReason"] = $"Column symbol '{symName}' not found in document."
                            });
                            continue;
                        }

                        if (!sym.IsActive) sym.Activate();
                        var loc = geom.Obj("location");
                        var pt = ToXyz(loc.Double("x"), loc.Double("y"), 0);

                        var colInst = doc.Create.NewFamilyInstance(pt, sym, baseLvl, Autodesk.Revit.DB.Structure.StructuralType.Column);
                        if (topLvl != null)
                        {
                            var pTop = colInst.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                            if (pTop != null && !pTop.IsReadOnly) pTop.Set(topLvl.Id);
                        }

                        double rotDeg = geom.Double("rotationDegrees", geom.Double("rotationDeg", 0.0));
                        if (Math.Abs(rotDeg) > 1e-4)
                        {
                            var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
                            ElementTransformUtils.RotateElement(doc, colInst.Id, axis, rotDeg * (Math.PI / 180.0));
                        }

                        var pComments = colInst.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        if (!string.IsNullOrEmpty(mark)) createdElements[mark] = colInst;
                        subTx.Commit();

                        records.Add(new Dictionary<string, object>
                        {
                            ["sourceKey"] = sourceKey,
                            ["category"] = category,
                            ["mark"] = mark,
                            ["status"] = "Created",
                            ["targetUniqueId"] = colInst.UniqueId,
                            ["targetElementId"] = (long)colInst.Id.Value
                        });
                    }
        """;
}
