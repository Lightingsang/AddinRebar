namespace HPDynamo.McpBridge.Coordinator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using Serilog;

/// <summary>
///     Extracts runtime session facts from Revit and Dynamo for get_dynamo_context.
/// </summary>
public static class DynamoContextExtractor
{
    public static ContextResult Extract(
        object? uiapp,
        DynamoBridgeCoordinator coordinator,
        bool includeSelection,
        bool executionEnabled)
    {
        if (uiapp is null)
        {
            return new ContextResult
            {
                Host = PipeNaming.DynamoHost,
                HostVersion = "2026",
                RevitVersion = "2026",
                DocTitle = coordinator.ActiveDocumentTitle,
                DocPath = null,
                IsFamily = false,
                IsReadOnly = false,
                IsModifiable = false,
                Units = new UnitsInfo("m"),
                OpenDocs = coordinator.ActiveDocumentTitle != null ? [coordinator.ActiveDocumentTitle] : [],
                ExecutionEnabled = executionEnabled,
                Selection = [],
                Dynamo = BuildDynamoInfo(coordinator)
            };
        }

        return ExtractFromRevit(uiapp, coordinator, includeSelection, executionEnabled);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ContextResult ExtractFromRevit(
        object uiapp,
        DynamoBridgeCoordinator coordinator,
        bool includeSelection,
        bool executionEnabled)
    {
        var app = (UIApplication)uiapp;
        var uidoc = app.ActiveUIDocument;
        var doc = uidoc?.Document;

        var dynamoInfo = BuildDynamoInfo(coordinator);
        var selectedElements = includeSelection && doc != null && uidoc != null
            ? ExtractSelectedElements(uidoc, doc)
            : Array.Empty<ElementInfo>();

        var openDocs = new List<string>();
        if (app.Application?.Documents != null)
        {
            foreach (Document d in app.Application.Documents)
            {
                if (!string.IsNullOrEmpty(d.Title)) openDocs.Add(d.Title);
            }
        }

        var version = app.Application?.VersionNumber ?? "2026";
        return new ContextResult
        {
            Host = PipeNaming.DynamoHost,
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
            Dynamo = dynamoInfo
        };
    }

    private static DynamoInfo BuildDynamoInfo(DynamoBridgeCoordinator coordinator)
    {
        bool isLive = coordinator.IsLiveCanvasAttached;
        var ws = coordinator.ActiveWorkspace;
        var vm = coordinator.ActiveViewModel;

        string? dynamoVersion = typeof(DynamoModel).Assembly.GetName().Version?.ToString();
        string? activeWsName = ws?.Name;
        string? activeWsPath = string.IsNullOrEmpty(ws?.FileName) ? null : ws.FileName;
        string runType = ws is HomeWorkspaceModel home ? home.RunSettings.RunType.ToString() : "Manual";

        int nodeCount = ws != null ? ws.Nodes.Count() : 0;
        int wireCount = ws != null ? ws.Connectors.Count() : 0;
        int selectedNodeCount = vm?.CurrentSpaceViewModel?.Nodes.Count(n => n.IsSelected) ?? 0;
        bool hasUnsavedChanges = ws?.HasUnsavedChanges ?? false;

        return new DynamoInfo(
            IsLive: isLive,
            DynamoVersion: dynamoVersion,
            ActiveWorkspaceName: activeWsName,
            ActiveWorkspacePath: activeWsPath,
            RunType: runType,
            NodeCount: nodeCount,
            WireCount: wireCount,
            SelectedNodeCount: selectedNodeCount,
            HasUnsavedChanges: hasUnsavedChanges);
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
            // Unit lookup can fail on non-standard templates; fallback to standard meters
            Log.Debug(ex, "Length unit lookup failed, defaulting to meters");
            return "autodesk.spec.unit:meters-1.0.0";
        }
    }
}
