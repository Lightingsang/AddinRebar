namespace HPDynamo.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPDynamo.E2E.Tests.Contracts;

/// <summary>
/// Comprehensive test harness providing utilities, mock data fixtures, and
/// pipeline automation for HPDynamo MCP E2E tests.
/// </summary>
public static class DynamoE2ETestHarness
{
    public static DynamoGraphSpec CreateSampleRebarGraph()
    {
        var node1 = new DynamoNodeSpec(
            Id: Guid.NewGuid(),
            Name: "Point.ByCoordinates",
            NodeType: "DSCoreNodes.Point",
            X: 100, Y: 100,
            InPorts: new[] { "x", "y", "z" },
            OutPorts: new[] { "Point" },
            Properties: new Dictionary<string, object?> { ["x"] = 0, ["y"] = 0, ["z"] = 0 });

        var node2 = new DynamoNodeSpec(
            Id: Guid.NewGuid(),
            Name: "RebarBarType.ByName",
            NodeType: "Revit.Elements.RebarBarType",
            X: 400, Y: 100,
            InPorts: new[] { "name" },
            OutPorts: new[] { "rebarBarType" },
            Properties: new Dictionary<string, object?> { ["name"] = "20M" });

        var wire = new DynamoWireSpec(node1.Id, 0, node2.Id, 0);

        return new DynamoGraphSpec(
            Uuid: Guid.NewGuid(),
            Name: "ParametricBeamRebar",
            Description: "TCVN 5574:2018 Beam Reinforcement",
            Nodes: new List<DynamoNodeSpec> { node1, node2 },
            Wires: new List<DynamoWireSpec> { wire });
    }

    public static IReadOnlyList<string> BuildToolSurfaceList()
    {
        var list = new List<string>();
        list.AddRange(DynamoE2EContracts.CoreTools);
        list.AddRange(DynamoE2EContracts.RegistryMetaTools);
        list.AddRange(DynamoE2EContracts.EmbeddedSeeds);
        // Suppress inspect_type
        return list.Distinct().OrderBy(s => s).ToList();
    }

    public static DynamoNodeSpec AddNodeToCanvas(
        DynamoE2EFakeBridge bridge, string name, string nodeType, double x, double y)
    {
        if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
        {
            throw new ArgumentException("Coordinates must be finite numbers.");
        }

        var node = new DynamoNodeSpec(
            Id: Guid.NewGuid(),
            Name: name,
            NodeType: nodeType,
            X: x, Y: y,
            InPorts: new[] { "input" },
            OutPorts: new[] { "output" },
            Properties: new Dictionary<string, object?>());

        bridge.ActiveCanvasWorkspace.Nodes.Add(node);
        return node;
    }

    public static DynamoWireSpec ConnectPortsOnCanvas(
        DynamoE2EFakeBridge bridge, Guid sourceId, int sourcePort, Guid targetId, int targetPort)
    {
        if (sourcePort < 0 || targetPort < 0 || sourcePort > 10 || targetPort > 10)
        {
            throw new IndexOutOfRangeException("Port index is out of bounds.");
        }

        if (sourceId == targetId)
        {
            throw new InvalidOperationException("Cyclic self-connection is not permitted.");
        }

        var wire = new DynamoWireSpec(sourceId, sourcePort, targetId, targetPort);
        bridge.ActiveCanvasWorkspace.Wires.Add(wire);
        return wire;
    }

    public static byte[] GenerateMockGraphImage()
    {
        // 8-byte standard PNG header signature
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
    }
}
