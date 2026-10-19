namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;

public static class GrasshopperE2EHarness
{
    public static GrasshopperCanvasSpec CreateSampleTowerGraph()
    {
        var slider = new GrasshopperComponentSpec(
            Guid.NewGuid(), "Number Slider", "FloorCount", "Params", "Input",
            100, 100, Array.Empty<string>(), new[] { "Value" },
            new Dictionary<string, object?> { ["Value"] = 10.0 });

        var panel = new GrasshopperComponentSpec(
            Guid.NewGuid(), "Panel", "TotalHeight", "Params", "Output",
            400, 100, new[] { "Input" }, Array.Empty<string>(),
            new Dictionary<string, object?> { ["Text"] = "38.0 m" });

        var wire = new GrasshopperWireSpec(slider.Id, 0, panel.Id, 0);

        return new GrasshopperCanvasSpec(
            Guid.NewGuid(), "ParametricTower.gh", "Parametric Concrete Tower",
            new List<GrasshopperComponentSpec> { slider, panel },
            new List<GrasshopperWireSpec> { wire });
    }

    public static GrasshopperCanvasSpec CreateSampleRebarGraph()
    {
        var node1 = new GrasshopperComponentSpec(
            Guid.NewGuid(), "RebarCurveGenerator", "Curves", "Rebar", "Core",
            100, 150, new[] { "BeamSpan", "Cover" }, new[] { "LongitudinalCurves" },
            new Dictionary<string, object?> { ["Cover"] = 30.0 });

        var node2 = new GrasshopperComponentSpec(
            Guid.NewGuid(), "StirrupDistributor", "Stirrups", "Rebar", "Core",
            450, 150, new[] { "Curves", "DenseSpacing", "SparseSpacing" }, new[] { "StirrupCurves" },
            new Dictionary<string, object?> { ["DenseSpacing"] = 100.0, ["SparseSpacing"] = 200.0 });

        var wire = new GrasshopperWireSpec(node1.Id, 0, node2.Id, 0);

        return new GrasshopperCanvasSpec(
            Guid.NewGuid(), "BeamRebar.gh", "TCVN 5574:2018 Beam Reinforcement",
            new List<GrasshopperComponentSpec> { node1, node2 },
            new List<GrasshopperWireSpec> { wire });
    }

    public static IReadOnlyList<string> BuildToolSurfaceList()
    {
        var list = new List<string>();
        list.AddRange(GrasshopperHostProfile.Instance.CoreToolNames);
        list.AddRange(ToolValidator.RegistryToolNames);
        list.AddRange(SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).Select(s => s.Name));
        return list.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    public static GrasshopperComponentSpec AddComponentToCanvas(
        GrasshopperE2EBridge bridge, string name, string nickName, string category, double x, double y, IReadOnlyList<string>? messages = null)
    {
        if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
        {
            throw new ArgumentException("Coordinates must be finite numbers.");
        }

        var component = new GrasshopperComponentSpec(
            Guid.NewGuid(), name, nickName, category, "Custom",
            x, y, new[] { "In" }, new[] { "Out" }, new Dictionary<string, object?>(),
            Messages: messages);

        bridge.ActiveCanvas.Components.Add(component);
        return component;
    }

    public static IReadOnlyList<string> ExtractComponentMessages(GrasshopperE2EBridge bridge, Guid? componentId = null)
    {
        var query = bridge.ActiveCanvas.Components.AsEnumerable();
        if (componentId.HasValue) query = query.Where(c => c.Id == componentId.Value);
        return query.Where(c => c.Messages != null).SelectMany(c => c.Messages!).ToList();
    }

    public static GrasshopperWireSpec ConnectPortsOnCanvas(
        GrasshopperE2EBridge bridge, Guid sourceId, int sourcePort, Guid targetId, int targetPort)
    {
        if (sourcePort < 0 || targetPort < 0 || sourcePort > 10 || targetPort > 10)
        {
            throw new IndexOutOfRangeException("Port index is out of bounds.");
        }

        if (sourceId == targetId)
        {
            throw new InvalidOperationException("Cyclic self-connection is not permitted.");
        }

        var wire = new GrasshopperWireSpec(sourceId, sourcePort, targetId, targetPort);
        bridge.ActiveCanvas.Wires.Add(wire);
        return wire;
    }

    public static bool RemoveComponentFromCanvas(GrasshopperE2EBridge bridge, Guid componentId)
    {
        var comp = bridge.ActiveCanvas.Components.FirstOrDefault(c => c.Id == componentId);
        if (comp == null) return false;

        bridge.ActiveCanvas.Components.Remove(comp);
        bridge.ActiveCanvas.Wires.RemoveAll(w => w.SourceComponentId == componentId || w.TargetComponentId == componentId);
        return true;
    }

    public static GrasshopperPlayerResult RunPlayer(
        string filePath,
        Dictionary<string, object>? inputs = null,
        IReadOnlyList<string>? requestedOutputs = null,
        bool headless = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return new GrasshopperPlayerResult(false, 0, 0, new Dictionary<string, object>(), "Definition file path cannot be empty.");

        var ext = Path.GetExtension(filePath);
        if (!string.Equals(ext, ".gh", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".ghx", StringComparison.OrdinalIgnoreCase))
        {
            return new GrasshopperPlayerResult(false, 0, 0, new Dictionary<string, object>(), $"Invalid file extension '{ext}'. Only .gh and .ghx definitions are supported.");
        }

        if (!File.Exists(filePath))
            return new GrasshopperPlayerResult(false, 0, 0, new Dictionary<string, object>(), $"Definition file not found: {filePath}");

        var outputs = new Dictionary<string, object>
        {
            ["TotalHeight"] = 95.0,
            ["TotalArea"] = 4500.0
        };

        if (requestedOutputs != null && requestedOutputs.Count > 0)
        {
            outputs = outputs.Where(kv => requestedOutputs.Contains(kv.Key)).ToDictionary(k => k.Key, v => v.Value);
        }

        return new GrasshopperPlayerResult(true, 15, inputs?.Count ?? 0, outputs);
    }

    public static GrasshopperImageExportResult ExportCanvasImage(
        GrasshopperCanvasSpec? canvas, string mode = "FullCanvas", double zoom = 1.0, string? outputPath = null, bool returnBase64 = true)
    {
        if (canvas == null) return new GrasshopperImageExportResult(false, 0, 0, mode, string.Empty, string.Empty, "Canvas is null.");

        double clampedZoom = Math.Clamp(zoom, 0.1, 5.0);
        int baseWidth = canvas.Components.Count == 0 ? 0 : 1000;
        int baseHeight = canvas.Components.Count == 0 ? 0 : 800;
        int width = Math.Max(200, (int)(baseWidth * clampedZoom));
        int height = Math.Max(200, (int)(baseHeight * clampedZoom));

        var bytes = GenerateMockPngBytes();
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            try
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(outputPath, bytes);
            }
            catch (Exception ex)
            {
                return new GrasshopperImageExportResult(false, 0, 0, mode, outputPath, string.Empty, $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        string base64 = returnBase64 ? Convert.ToBase64String(bytes) : string.Empty;
        return new GrasshopperImageExportResult(true, width, height, mode, outputPath ?? string.Empty, base64);
    }

    public static byte[] GenerateMockPngBytes()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
    }
}
