namespace HPGrasshopper.McpBridge.Coordinator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Grasshopper.Kernel;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using Rhino;
using Serilog;

/// <summary>
///     Extracts runtime session facts from Revit and Grasshopper for get_grasshopper_context.
/// </summary>
public static class GrasshopperContextExtractor
{
    public static ContextResult Extract(
        object? uiapp,
        GrasshopperBridgeCoordinator coordinator,
        bool includeSelection,
        bool executionEnabled)
    {
        if (uiapp is null)
        {
            return ExtractNullUiApp(coordinator, executionEnabled);
        }

        var res = ExtractFromRevit(uiapp, coordinator, includeSelection, executionEnabled);
        coordinator.UpdateCachedUiContext(res);
        return res;
    }

    private static ContextResult ExtractNullUiApp(
        GrasshopperBridgeCoordinator coordinator,
        bool executionEnabled)
    {
        bool isDocumentMatched = coordinator.CachedUiContext != null &&
            (coordinator.ActiveDocumentTitle == null ||
             string.Equals(coordinator.ActiveDocumentTitle, coordinator.CachedUiContext.DocTitle, StringComparison.OrdinalIgnoreCase));

        if (isDocumentMatched && coordinator.CachedUiContext != null)
        {
            var cached = coordinator.CachedUiContext;
            coordinator.MarkContextStale();
            return new ContextResult
            {
                Host = cached.Host,
                HostVersion = cached.HostVersion,
                RevitVersion = cached.RevitVersion,
                DocTitle = coordinator.ActiveDocumentTitle ?? cached.DocTitle,
                DocPath = coordinator.ActiveDocumentPath ?? cached.DocPath,
                IsFamily = cached.IsFamily,
                IsReadOnly = cached.IsReadOnly,
                IsModifiable = false,
                Units = cached.Units,
                OpenDocs = cached.OpenDocs,
                ExecutionEnabled = executionEnabled,
                Selection = cached.Selection,
                Grasshopper = cached.Grasshopper
            };
        }

        coordinator.MarkContextUnavailable();
        return new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            HostVersion = "2026",
            RevitVersion = "2026",
            DocTitle = coordinator.ActiveDocumentTitle,
            DocPath = coordinator.ActiveDocumentPath,
            IsFamily = false,
            IsReadOnly = false,
            IsModifiable = false,
            Units = new UnitsInfo("unavailable"),
            OpenDocs = coordinator.ActiveDocumentTitle != null ? [coordinator.ActiveDocumentTitle] : [],
            ExecutionEnabled = executionEnabled,
            Selection = [],
            Grasshopper = BuildGrasshopperFallbackInfo(coordinator)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ContextResult ExtractFromRevit(
        object uiapp,
        GrasshopperBridgeCoordinator coordinator,
        bool includeSelection,
        bool executionEnabled)
    {
        var app = (UIApplication)uiapp;
        var uidoc = app.ActiveUIDocument;
        var doc = uidoc?.Document;

        var ghDoc = Grasshopper.Instances.ActiveDocument;
        coordinator.UpdateActiveSnapshot(
            doc?.Title,
            string.IsNullOrEmpty(doc?.PathName) ? null : doc.PathName,
            ghDoc?.DocumentID,
            ghDoc?.DisplayName,
            ghDoc?.FilePath);

        var ghInfo = BuildGrasshopperInfo();
        var selectedElements = includeSelection && doc != null && uidoc != null
            ? ExtractSelectedElements(uidoc, doc)
            : Array.Empty<ElementInfo>();

        var openDocs = BuildOpenDocsList(app);
        var version = app.Application?.VersionNumber ?? "2026";
        return new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            HostVersion = version,
            RevitVersion = version,
            DocTitle = doc?.Title,
            DocPath = string.IsNullOrEmpty(doc?.PathName) ? null : doc.PathName,
            IsFamily = doc?.IsFamilyDocument ?? false,
            IsReadOnly = doc?.IsReadOnly ?? false,
            IsModifiable = doc?.IsModifiable ?? false,
            Units = new UnitsInfo(doc != null ? GetLengthUnitName(doc) : "m"),
            OpenDocs = openDocs,
            ExecutionEnabled = executionEnabled,
            Selection = selectedElements,
            Grasshopper = ghInfo
        };
    }

    private static List<string> BuildOpenDocsList(UIApplication app)
    {
        var openDocs = new List<string>();
        if (app.Application?.Documents != null)
        {
            foreach (Document d in app.Application.Documents)
            {
                if (!string.IsNullOrEmpty(d.Title)) openDocs.Add(d.Title);
            }
        }
        return openDocs;
    }

    private static GrasshopperInfo BuildGrasshopperFallbackInfo(GrasshopperBridgeCoordinator coordinator)
    {
        return new GrasshopperInfo(
            IsEditorLoaded: false,
            GrasshopperVersion: typeof(GH_Document).Assembly.GetName().Version?.ToString(),
            RhinoVersion: typeof(RhinoDoc).Assembly.GetName().Version?.ToString(),
            ActiveDefinitionName: coordinator.ActiveDefinitionName,
            ActiveDefinitionPath: coordinator.ActiveDefinitionPath,
            ComponentCount: 0,
            WireCount: 0,
            SelectedComponentCount: 0,
            HasUnsavedChanges: false,
            IsRhinoInsideRevitActive: true);
    }

    private static GrasshopperInfo BuildGrasshopperInfo()
    {
        try
        {
            var isEditorLoaded = Grasshopper.Instances.DocumentEditor != null;
            var doc = Grasshopper.Instances.ActiveDocument;
            var (wireCount, selectedCount) = CountGraphMetrics(doc);

            return new GrasshopperInfo(
                IsEditorLoaded: isEditorLoaded,
                GrasshopperVersion: typeof(GH_Document).Assembly.GetName().Version?.ToString(),
                RhinoVersion: typeof(RhinoDoc).Assembly.GetName().Version?.ToString(),
                ActiveDefinitionName: doc?.DisplayName,
                ActiveDefinitionPath: doc?.FilePath,
                ComponentCount: doc?.ObjectCount ?? 0,
                WireCount: wireCount,
                SelectedComponentCount: selectedCount,
                HasUnsavedChanges: doc?.IsModified ?? false,
                IsRhinoInsideRevitActive: true);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to query active Grasshopper runtime context");
            return new GrasshopperInfo(false, null, null, null, null, 0, 0, 0, false, false);
        }
    }

    private static (int wireCount, int selectedCount) CountGraphMetrics(GH_Document? doc)
    {
        if (doc is null) return (0, 0);

        int wireCount = 0;
        int selectedCount = 0;
        IGH_DocumentObject[] objects;
        lock (doc)
        {
            objects = doc.Objects.ToArray();
        }

        foreach (var obj in objects)
        {
            if (obj == null) continue;
            if (obj.Attributes?.Selected == true) selectedCount++;
            if (obj is IGH_Component comp)
            {
                var inputs = comp.Params?.Input;
                if (inputs != null)
                {
                    foreach (var inp in inputs)
                    {
                        if (inp?.Sources != null) wireCount += inp.Sources.Count;
                    }
                }
            }
            else if (obj is IGH_Param param && param.Sources != null)
            {
                wireCount += param.Sources.Count;
            }
        }
        return (wireCount, selectedCount);
    }

    private static IReadOnlyList<ElementInfo> ExtractSelectedElements(UIDocument uidoc, Document doc)
    {
        var list = new List<ElementInfo>();
        var selection = uidoc.Selection.GetElementIds();
        foreach (var id in selection)
        {
            var elem = doc.GetElement(id);
            if (elem != null)
            {
                list.Add(new ElementInfo(
                    id.Value,
                    elem.Category?.Name ?? "Uncategorized",
                    elem.Name));
            }
        }
        return list;
    }

    private static string GetLengthUnitName(Document doc)
    {
        try
        {
            var formatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Length);
            return formatOptions.GetUnitTypeId().TypeId;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Length unit lookup failed, defaulting to meters");
            return "autodesk.spec.unit:meters-1.0.0";
        }
    }
}
