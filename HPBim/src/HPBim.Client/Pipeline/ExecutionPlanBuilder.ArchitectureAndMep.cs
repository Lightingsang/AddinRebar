namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public static partial class ExecutionPlanBuilder
{
    private static void BuildSlabItems(BimModel model, HashSet<string> levelNames, List<ElementPlanItem> items)
    {
        if (model.Slabs == null) return;
        foreach (var slab in model.Slabs)
        {
            if (!levelNames.Contains(slab.Level))
            {
                throw new FailFastCoordinationException($"[ERR_UNRESOLVED_LEVEL] Slab '{slab.Id}' references unresolved Level '{slab.Level}'.");
            }

            string typeName = TypeResolutionRules.GetFloorTypeName(slab.ThicknessMm);
            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.Slab, slab.Id),
                Category = CoordinationCategory.Slab,
                Mark = slab.Id,
                TargetLevelName = slab.Level,
                IsStructural = true,
                Resource = new ResourceResolutionSpec
                {
                    FamilyName = "Floor",
                    TypeName = typeName,
                    RequiresProceduralCreation = true,
                    DimensionParametersMm = new Dictionary<string, double> { { "Thickness", slab.ThicknessMm } }
                },
                Geometry = new ProfileGeometrySpec
                {
                    OuterLoop = slab.Boundary.Vertices.Select(p => new Point3D(p.X, p.Y, 0)).ToList(),
                    InnerLoops = slab.Voids?.Select(v => (IReadOnlyList<Point3D>)v.Vertices.Select(p => new Point3D(p.X, p.Y, 0)).ToList()).ToList() ?? new List<IReadOnlyList<Point3D>>(),
                    ThicknessMm = slab.ThicknessMm,
                    ElevationMm = slab.ElevationOffsetMm
                }
            });
        }
    }

    private static void BuildWallItems(BimModel model, HashSet<string> levelNames, List<ElementPlanItem> items)
    {
        if (model.Walls == null) return;
        foreach (var wall in model.Walls)
        {
            if (!levelNames.Contains(wall.BaseLevel))
            {
                throw new FailFastCoordinationException($"[ERR_UNRESOLVED_LEVEL] Wall '{wall.Id}' references unresolved BaseLevel '{wall.BaseLevel}'.");
            }

            string typeName = TypeResolutionRules.GetWallTypeName(wall.ThicknessMm);
            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.Wall, wall.Id),
                Category = CoordinationCategory.Wall,
                Mark = wall.Id,
                TargetLevelName = wall.BaseLevel,
                TargetTopLevelName = wall.TopLevel,
                Resource = new ResourceResolutionSpec
                {
                    FamilyName = "Basic Wall",
                    TypeName = typeName,
                    RequiresProceduralCreation = true,
                    DimensionParametersMm = new Dictionary<string, double> { { "Thickness", wall.ThicknessMm } }
                },
                Geometry = new LineGeometrySpec
                {
                    Start = new Point3D(wall.StartPoint.X, wall.StartPoint.Y, 0),
                    End = new Point3D(wall.EndPoint.X, wall.EndPoint.Y, 0)
                }
            });
        }
    }

    private static void BuildOpeningItems(BimModel model, List<ElementPlanItem> items)
    {
        if (model.Walls == null) return;
        foreach (var wall in model.Walls)
        {
            if (wall.Openings == null) continue;
            foreach (var op in wall.Openings)
            {
                string typeName = op.Kind == OpeningKind.Door
                    ? TypeResolutionRules.GetDoorSymbolName(op.WidthMm, op.HeightMm)
                    : TypeResolutionRules.GetWindowSymbolName(op.WidthMm, op.HeightMm);

                items.Add(new ElementPlanItem
                {
                    SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.Opening, op.Id),
                    Category = CoordinationCategory.Opening,
                    Mark = op.Id,
                    HostElementKey = wall.Id,
                    Resource = new ResourceResolutionSpec
                    {
                        TypeName = typeName,
                        RequiresProceduralCreation = true,
                        DimensionParametersMm = new Dictionary<string, double>
                        {
                            { "Width", op.WidthMm },
                            { "Height", op.HeightMm }
                        }
                    },
                    Geometry = new OpeningGeometrySpec
                    {
                        HostWallSourceKey = wall.Id,
                        DistanceAlongWallMm = op.DistanceAlongWallMm,
                        SillHeightMm = op.SillHeightMm,
                        WidthMm = op.WidthMm,
                        HeightMm = op.HeightMm,
                        Kind = op.Kind
                    }
                });
            }
        }
    }

    private static void BuildRoomItems(BimModel model, HashSet<string> levelNames, List<ElementPlanItem> items)
    {
        if (model.Rooms == null) return;
        foreach (var room in model.Rooms)
        {
            if (!levelNames.Contains(room.Level))
            {
                throw new FailFastCoordinationException($"[ERR_UNRESOLVED_LEVEL] Room '{room.Name}' references unresolved Level '{room.Level}'.");
            }

            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.Room, room.Name),
                Category = CoordinationCategory.Room,
                Mark = room.Name,
                TargetLevelName = room.Level,
                Geometry = new PointGeometrySpec
                {
                    Location = new Point3D(room.TagLocation.X, room.TagLocation.Y, 0),
                    RotationDegrees = room.TagRotationDeg
                }
            });
        }
    }

    private static void BuildMepItems(BimModel model, HashSet<string> levelNames, List<ElementPlanItem> items)
    {
        if (model.Fixtures != null)
        {
            foreach (var fix in model.Fixtures)
            {
                if (!levelNames.Contains(fix.Level))
                {
                    throw new FailFastCoordinationException($"[ERR_UNRESOLVED_LEVEL] Fixture '{fix.Id}' references unresolved Level '{fix.Level}'.");
                }
                items.Add(new ElementPlanItem
                {
                    SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.MepFixture, fix.Id),
                    Category = CoordinationCategory.MepFixture,
                    Mark = fix.Id,
                    TargetLevelName = fix.Level,
                    Geometry = new PointGeometrySpec
                    {
                        Location = new Point3D(fix.Location.X, fix.Location.Y, 0),
                        RotationDegrees = fix.RotationDeg
                    }
                });
            }
        }

        BuildPipeItems(model, items);
    }

    private static void BuildPipeItems(BimModel model, List<ElementPlanItem> items)
    {
        if (model.Pipes == null) return;
        foreach (var pipe in model.Pipes)
        {
            var startPt = pipe.Points != null && pipe.Points.Count > 0 ? pipe.Points[0] : new Point3D(0, 0, 0);
            var endPt = pipe.Points != null && pipe.Points.Count > 1 ? pipe.Points[1] : startPt;

            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.PipeRun, pipe.Id),
                Category = CoordinationCategory.PipeRun,
                Mark = pipe.Id,
                TargetLevelName = pipe.Level ?? string.Empty,
                Resource = new ResourceResolutionSpec
                {
                    FamilyName = TypeResolutionRules.GetPipingSystemTypeName(pipe.SystemType),
                    TypeName = TypeResolutionRules.GetPipeTypeName(pipe.Material)
                },
                Geometry = new LineGeometrySpec
                {
                    Start = startPt,
                    End = endPt
                },
                Parameters = new Dictionary<string, object>
                {
                    ["SystemType"] = TypeResolutionRules.GetPipingSystemTypeName(pipe.SystemType),
                    ["PipeType"] = TypeResolutionRules.GetPipeTypeName(pipe.Material),
                    ["DiameterMm"] = pipe.DiameterMm
                }
            });
        }
    }

    private static void BuildRebarItems(BimModel model, List<ElementPlanItem> items)
    {
        if (model.Rebars == null) return;
        foreach (var rb in model.Rebars)
        {
            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.Rebar, rb.Id),
                Category = CoordinationCategory.Rebar,
                Mark = rb.Id,
                HostElementKey = rb.HostElementId,
                Resource = new ResourceResolutionSpec
                {
                    TypeName = $"d{rb.DiameterMm:F0}",
                    DimensionParametersMm = new Dictionary<string, double>
                    {
                        { "Diameter", rb.DiameterMm },
                        { "Length", rb.LengthMm }
                    }
                }
            });
        }
    }
}
