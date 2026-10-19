namespace HPGrasshopper.McpBridge.Tests.Fixtures;

using System;
using System.Collections.Generic;
using System.IO;
using GH_IO.Serialization;

/// <summary>
///     Programmatically builds, serializes, and inspects Grasshopper definition archives (.gh and .ghx)
///     using pure managed GH_IO without requiring native Rhino runtime P/Invoke.
/// </summary>
public static class FixtureBuilder
{
    public sealed record DefinitionInspection(
        string Name,
        int ObjectCount,
        IReadOnlyList<string> ObjectNames,
        IReadOnlyList<string> NickNames);

    private static (GH_Archive archive, GH_Chunk def) CreateCleanArchive(string name, string description, Guid definitionId)
    {
        var archive = new GH_Archive();
        archive.CreateNewRoot(true);
        var root = archive.GetRootNode
            ?? throw new InvalidOperationException("Failed to get root node");
        var def = (GH_Chunk)root.CreateChunk("Definition");

        var props = def.CreateChunk("DefinitionProperties");
        props.SetString("Name", name);
        props.SetString("Description", description);
        props.SetGuid("DefinitionId", definitionId);

        return (archive, def);
    }

    public static GH_Archive CreateMathSliderArchive()
    {
        var (archive, def) = CreateCleanArchive(
            "MathSlider",
            "Arithmetic definition: 2 sliders (A, B) -> Add -> Panel (Result)",
            Guid.Parse("11111111-2222-3333-4444-555555555551"));

        var objs = def.CreateChunk("DefinitionObjects");
        objs.SetInt32("ObjectCount", 4);
        AddObjectChunk(objs, 0, "Number Slider", "A", 10.0, "Param_Number");
        AddObjectChunk(objs, 1, "Number Slider", "B", 25.0, "Param_Number");
        AddObjectChunk(objs, 2, "Addition", "+", 0.0, "Component_Add");
        AddObjectChunk(objs, 3, "Panel", "Result", 35.0, "Param_String");

        return archive;
    }

    public static GH_Archive CreateCurveGeneratorArchive()
    {
        var (archive, def) = CreateCleanArchive(
            "CurveGenerator",
            "Parametric curve: Radius, PointCount, Closed -> GeneratedCurve, CurveLength",
            Guid.Parse("22222222-3333-4444-5555-666666666662"));

        var objs = def.CreateChunk("DefinitionObjects");
        objs.SetInt32("ObjectCount", 5);
        AddObjectChunk(objs, 0, "Number Slider", "Radius", 10.0, "Param_Number");
        AddObjectChunk(objs, 1, "Integer Slider", "PointCount", 12.0, "Param_Integer");
        AddObjectChunk(objs, 2, "Boolean Toggle", "Closed", 1.0, "Param_Boolean");
        AddObjectChunk(objs, 3, "Curve", "GeneratedCurve", 0.0, "Param_Curve");
        AddObjectChunk(objs, 4, "Number", "CurveLength", 62.8318, "Param_Number");

        return archive;
    }

    public static GH_Archive CreateRevitDirectShapeSyncMockArchive()
    {
        var (archive, def) = CreateCleanArchive(
            "RevitDirectShapeSyncMock",
            "Parametric direct shape sync definition mock",
            Guid.Parse("33333333-4444-5555-6666-777777777773"));

        var objs = def.CreateChunk("DefinitionObjects");
        objs.SetInt32("ObjectCount", 7);
        AddObjectChunk(objs, 0, "Panel", "CategoryName", 0.0, "Param_String", "Generic Models");
        AddObjectChunk(objs, 1, "Number Slider", "BoxWidth", 2000.0, "Param_Number");
        AddObjectChunk(objs, 2, "Number Slider", "BoxHeight", 4000.0, "Param_Number");
        AddObjectChunk(objs, 3, "Boolean Toggle", "DryRun", 0.0, "Param_Boolean");
        AddObjectChunk(objs, 4, "Panel", "SolidSummary", 0.0, "Param_String", "Box [2000 x 2000 x 4000 mm]");
        AddObjectChunk(objs, 5, "Number", "Volume", 16.0, "Param_Number");
        AddObjectChunk(objs, 6, "Panel", "SyncStatus", 0.0, "Param_String", "Ready for Revit DirectShape Bake");

        return archive;
    }

    public static GH_Archive CreateContinuousBeamRenumberingArchive(string? scriptPath = null)
    {
        var (archive, def) = CreateCleanArchive(
            "ContinuousBeamRenumbering",
            "Continuous Beam Renumbering and Element Counting Definition for Revit Framing via Rhino.Inside.Revit and HPGrasshopper MCP",
            Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc"));

        var objs = def.CreateChunk("DefinitionObjects");
        objs.SetInt32("ObjectCount", 7);

        var panelGuid = Guid.Parse("59e0b89a-e487-49f8-bab8-b5bab16be14c");
        var sliderGuid = Guid.Parse("57da07bd-ecab-415d-9d86-af36d7073abc");
        var toggleGuid = Guid.Parse("2e78987b-9dfb-42a2-8b76-3923ac8bd91a");

        AddObjectChunk(objs, 0, "Panel", "ViewName", 0.0, "Param_String", "MẶT BẰNG DẦM SÀN TẦNG 2", panelGuid);
        AddObjectChunk(objs, 1, "Panel", "FloorSuffix", 0.0, "Param_String", "T2", panelGuid);
        AddObjectChunk(objs, 2, "Number Slider", "MinWidthMm", 200.0, "Param_Number", null, sliderGuid);
        AddObjectChunk(objs, 3, "Number Slider", "ToleranceMm", 60.0, "Param_Number", null, sliderGuid);
        AddObjectChunk(objs, 4, "Boolean Toggle", "DryRun", 0.0, "Param_Boolean", null, toggleGuid);

        string? scriptContent = null;
        if (!string.IsNullOrWhiteSpace(scriptPath) && File.Exists(scriptPath))
        {
            scriptContent = File.ReadAllText(scriptPath);
        }

        AddObjectChunk(objs, 5, "ContinuousBeamRenumberingComponent", "RenumberScript", 0.0, "Component_Script", scriptContent);
        AddObjectChunk(objs, 6, "Panel", "RenumberSummary", 0.0, "Param_String", "Continuous Beam Renumbering: Ready to execute.", panelGuid);

        return archive;
    }

    private static void AddObjectChunk(
        GH_IWriter parent,
        int index,
        string name,
        string nickName,
        double numVal,
        string typeTag,
        string? stringVal = null,
        Guid? componentGuid = null)
    {
        var chunk = parent.CreateChunk("Object", index);
        chunk.SetString("Name", name);
        chunk.SetString("NickName", nickName);
        chunk.SetString("TypeTag", typeTag);
        chunk.SetDouble("NumericValue", numVal);
        chunk.SetGuid("InstanceGuid", Guid.NewGuid());
        if (componentGuid.HasValue)
        {
            chunk.SetGuid("GUID", componentGuid.Value);
        }
        if (!string.IsNullOrEmpty(stringVal))
        {
            chunk.SetString("StringValue", stringVal);
            chunk.SetString("UserText", stringVal);
        }
    }

    public static void SaveFixturePair(string outputDirectory, string fixtureName, GH_Archive archive)
    {
        Directory.CreateDirectory(outputDirectory);
        var xmlPath = Path.Combine(outputDirectory, fixtureName + ".ghx");
        archive.WriteToFile(xmlPath, true, false);

        var binPath = Path.Combine(outputDirectory, fixtureName + ".gh");
        archive.WriteToFile(binPath, true, true);
    }

    public static GH_Archive LoadArchive(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Fixture file not found: " + filePath);

        var archive = new GH_Archive();
        if (!archive.ReadFromFile(filePath))
            throw new InvalidOperationException("Failed deserializing archive from: " + filePath);

        return archive;
    }

    public static GH_Chunk FindDefinitionChunk(GH_Archive archive)
    {
        var root = archive.GetRootNode
            ?? throw new InvalidOperationException("Archive missing root node");

        if (root.FindChunk("Definition") is GH_Chunk def) return def;

        foreach (var item in root.Chunks)
        {
            if (item is GH_Chunk chunk)
            {
                if (string.Equals(chunk.Name, "Definition", StringComparison.OrdinalIgnoreCase))
                    return chunk;
                if (chunk.FindChunk("Definition") is GH_Chunk subDef)
                    return subDef;
            }
        }

        throw new InvalidOperationException("Archive missing Definition chunk");
    }

    public static DefinitionInspection InspectDefinition(GH_Archive archive)
    {
        var def = FindDefinitionChunk(archive);
        var props = def.FindChunk("DefinitionProperties") as GH_Chunk;
        var name = props?.GetString("Name") ?? "Unnamed";

        var objs = def.FindChunk("DefinitionObjects") as GH_Chunk;
        var count = objs?.GetInt32("ObjectCount") ?? 0;

        var objNames = new List<string>();
        var nickNames = new List<string>();

        for (var i = 0; i < count; i++)
        {
            if (objs?.FindChunk("Object", i) is GH_Chunk objChunk)
            {
                objNames.Add(objChunk.GetString("Name"));
                nickNames.Add(objChunk.GetString("NickName"));
            }
        }

        return new DefinitionInspection(name, count, objNames, nickNames);
    }
}
