namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Validation;

/// <summary>
/// Pre-resolves domain models into an immutable, single-source-of-truth BimExecutionPlan.
/// Enforces topological ordering and strict zero-fallback resolution.
/// </summary>
public static partial class ExecutionPlanBuilder
{
    public static BimExecutionPlan Build(
        BimModel model,
        DocumentIdentitySnapshot? targetDoc = null,
        ExecutionOptions? options = null)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        var validation = BimModelSchemaValidator.ValidateModel(model);
        if (!validation.IsValid) throw new SchemaValidationException(validation.Errors);

        options ??= new ExecutionOptions();
        targetDoc ??= new DocumentIdentitySnapshot();

        var levelNames = new HashSet<string>(model.Levels.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);
        var rawItems = CollectAllPlanItems(model, options, levelNames);
        var sequencedItems = rawItems
            .Select((item, idx) => item with { SequenceOrder = idx + 1 })
            .ToList();

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = BimExecutionPlan.ComputePlanHash(targetDoc, sequencedItems, model.ProjectName),
            SourceModelName = model.ProjectName,
            TargetDoc = targetDoc,
            Items = sequencedItems,
            Options = options
        };
    }

    private static List<ElementPlanItem> CollectAllPlanItems(
        BimModel model,
        ExecutionOptions options,
        HashSet<string> levelNames)
    {
        var rawItems = new List<ElementPlanItem>();
        BuildLevelItems(model, options, rawItems);
        BuildGridItems(model, rawItems);
        BuildColumnItems(model, levelNames, rawItems);
        BuildBeamItems(model, levelNames, rawItems);
        BuildSlabItems(model, levelNames, rawItems);
        BuildWallItems(model, levelNames, rawItems);
        BuildOpeningItems(model, rawItems);
        BuildRoomItems(model, levelNames, rawItems);
        BuildMepItems(model, levelNames, rawItems);
        BuildRebarItems(model, rawItems);
        return rawItems;
    }

    private static void BuildLevelItems(BimModel model, ExecutionOptions options, List<ElementPlanItem> items)
    {
        foreach (var lvl in model.Levels)
        {
            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.Level, lvl.Name),
                Category = CoordinationCategory.Level,
                Mark = lvl.Name,
                TargetLevelName = lvl.Name,
                CreateAssociatedView = options.CreatePlanViews,
                Resource = new ResourceResolutionSpec { TypeName = lvl.Name },
                Geometry = new LevelGeometrySpec
                {
                    ElevationM = lvl.ElevationM,
                    FloorNumber = lvl.FloorNumber,
                    IsBuildingStory = true
                }
            });
        }
    }

    private static void BuildGridItems(BimModel model, List<ElementPlanItem> items)
    {
        if (model.Grids == null) return;
        foreach (var grid in model.Grids)
        {
            items.Add(new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create(model.ProjectName, CoordinationCategory.Grid, grid.Name),
                Category = CoordinationCategory.Grid,
                Mark = grid.Name,
                Resource = new ResourceResolutionSpec { TypeName = "Grid" },
                Geometry = new LineGeometrySpec
                {
                    Start = new Point3D(grid.StartPoint.X, grid.StartPoint.Y, 0),
                    End = new Point3D(grid.EndPoint.X, grid.EndPoint.Y, 0)
                }
            });
        }
    }

    private static void BuildColumnItems(BimModel model, HashSet<string> levelNames, List<ElementPlanItem> items)
    {
        if (model.Columns == null) return;
        foreach (var col in model.Columns)
        {
            if (!levelNames.Contains(col.BaseLevel))
            {
                throw new FailFastCoordinationException($"[ERR_UNRESOLVED_LEVEL] Column '{col.Id}' references unresolved BaseLevel '{col.BaseLevel}'.");
            }

            items.Add(CreateColumnPlanItem(model.ProjectName, col));
        }
    }

    private static ElementPlanItem CreateColumnPlanItem(string projectName, ColumnDef col) =>
        new()
        {
            SourceKey = CompositeSourceKey.Create(projectName, CoordinationCategory.Column, col.Id),
            Category = CoordinationCategory.Column,
            Mark = col.Id,
            TargetLevelName = col.BaseLevel,
            TargetTopLevelName = col.TopLevel,
            IsStructural = true,
            Resource = new ResourceResolutionSpec
            {
                FamilyName = "M_Concrete-Rectangular-Column",
                TypeName = TypeResolutionRules.GetColumnSymbolName(col.WidthMm, col.DepthMm),
                RequiresProceduralCreation = true,
                DimensionParametersMm = new Dictionary<string, double>
                {
                    { "b", col.WidthMm },
                    { "h", col.DepthMm },
                    { "Width", col.WidthMm },
                    { "Depth", col.DepthMm }
                }
            },
            Geometry = new PointGeometrySpec
            {
                Location = new Point3D(col.CenterLocation.X, col.CenterLocation.Y, 0),
                RotationDegrees = col.RotationDeg
            }
        };

    private static void BuildBeamItems(BimModel model, HashSet<string> levelNames, List<ElementPlanItem> items)
    {
        if (model.Beams == null) return;
        foreach (var beam in model.Beams)
        {
            if (!levelNames.Contains(beam.Level))
            {
                throw new FailFastCoordinationException($"[ERR_UNRESOLVED_LEVEL] Beam '{beam.Id}' references unresolved Level '{beam.Level}'.");
            }

            items.Add(CreateBeamPlanItem(model.ProjectName, beam));
        }
    }

    private static ElementPlanItem CreateBeamPlanItem(string projectName, BeamDef beam) =>
        new()
        {
            SourceKey = CompositeSourceKey.Create(projectName, CoordinationCategory.Beam, beam.Id),
            Category = CoordinationCategory.Beam,
            Mark = beam.Id,
            TargetLevelName = beam.Level,
            IsStructural = true,
            Resource = new ResourceResolutionSpec
            {
                FamilyName = "M_Concrete-Rectangular Beam",
                TypeName = TypeResolutionRules.GetBeamSymbolName(beam.WidthMm, beam.HeightMm),
                RequiresProceduralCreation = true,
                DimensionParametersMm = new Dictionary<string, double>
                {
                    { "b", beam.WidthMm },
                    { "h", beam.HeightMm },
                    { "Width", beam.WidthMm },
                    { "Height", beam.HeightMm }
                }
            },
            Geometry = new LineGeometrySpec
            {
                Start = new Point3D(beam.StartPoint.X, beam.StartPoint.Y, 0),
                End = new Point3D(beam.EndPoint.X, beam.EndPoint.Y, 0)
            }
        };
}
