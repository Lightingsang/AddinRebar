using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     What the engine gained for a fifth host — Civil 3D, an AutoCAD vertical on the same acad.exe — and what it
///     must keep for the first four: the `civil3d` constants, a script contract that is the AutoCAD one plus the
///     `civil` global and the Civil namespaces, a guard profile that is a strict superset of AutoCAD's (rebuilds,
///     data shortcuts, survey, dialogs, COM interop and file-path members on top), an analyzer profile with the
///     AutoCAD transaction rule, and a context slot that travels beside the AutoCAD block and is dropped when unused.
/// </summary>
public sealed class Civil3dProfileTests
{
    private static readonly string[] CivilNamespaces =
    [
        "Autodesk.Civil", "Autodesk.Civil.ApplicationServices", "Autodesk.Civil.DatabaseServices",
        "Autodesk.Civil.DatabaseServices.Styles", "Autodesk.Civil.Settings",
    ];

    [Fact]
    public void Civil3d_constants_produce_the_pipe_prefix_imports_and_globals()
    {
        Assert.Equal("hpcivil3d-mcp-2026", PipeNaming.For(PipeNaming.Civil3dHost, 2026));
        Assert.Equal("hpcivil3d-mcp-2026", PipeNaming.For("Civil3D", 2026));
        // The generic branch already produced this name before the constant existed; the case only documents it.
        Assert.Equal("hp" + PipeNaming.Civil3dHost + "-mcp-2026", PipeNaming.For(PipeNaming.Civil3dHost, 2026));
        Assert.Equal("hpautocad-mcp-2026", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        Assert.Equal("civil3d.execute", JsonRpcMethods.For(JsonRpcMethods.Civil3dPrefix, JsonRpcMethods.ExecuteSuffix));
        Assert.Equal("execute", JsonRpcMethods.Suffix("civil3d.execute"));
        Assert.Equal(JsonRpcMethods.Suffix("autocad.execute"), JsonRpcMethods.Suffix("civil3d.execute"));
        Assert.Equal("hpcivil3d-mcp-2026", new BridgeOptions { HostId = "civil3d", HostVersion = 2026 }.PipeName);
        Assert.Equal("hpcivil3d-mcp-2026", Civil3d().PipeName(2026));
        Assert.Equal("civil3d.analyze", Civil3d().Method(JsonRpcMethods.AnalyzeSuffix));
    }

    [Fact]
    public void Civil3d_imports_are_the_autocad_set_plus_the_civil_namespaces_without_the_aec_facade_or_denied_namespaces()
    {
        var autocadCore = HostScriptContracts.AutocadImports.Where(ns => ns != "HPAutoCad.Aec").ToArray();
        Assert.All(autocadCore, ns => Assert.Contains(ns, HostScriptContracts.Civil3dImports));
        Assert.All(CivilNamespaces, ns => Assert.Contains(ns, HostScriptContracts.Civil3dImports));
        Assert.DoesNotContain("HPAutoCad.Aec", HostScriptContracts.Civil3dImports);
        Assert.DoesNotContain("Autodesk.Civil.DataShortcuts", HostScriptContracts.Civil3dImports);
        Assert.DoesNotContain("Autodesk.Civil.AeccUiMgd", HostScriptContracts.Civil3dImports);
        Assert.DoesNotContain(HostScriptContracts.Civil3dImports, ns => ns.StartsWith("Autodesk.AECC.Interop", StringComparison.Ordinal));
        Assert.Equal(HostScriptContracts.Civil3dImports.Length, HostScriptContracts.Civil3dImports.Distinct().Count());
    }

    [Fact]
    public void Civil3d_globals_are_the_autocad_globals_plus_civil_in_the_same_order()
    {
        Assert.Equal(["doc", "db", "ed", "app", "tr", "units", "civil", "ct", "log", "progress", "args"], HostScriptContracts.Civil3dGlobals);
        Assert.Equal(HostScriptContracts.AutocadGlobals, HostScriptContracts.Civil3dGlobals.Where(g => g != "civil").ToArray());
    }

    [Theory]
    // Civil-specific denials
    [InlineData("civil.CorridorCollection.RebuildAll(); return 1;", ".RebuildAll")]
    [InlineData("var c = (Corridor)tr.GetObject(id, OpenMode.ForWrite); c.Rebuild(); return 1;", ".Rebuild")]
    [InlineData("var s = (TinSurface)tr.GetObject(id, OpenMode.ForWrite); s.RebuildSnapshot(); return 1;", ".RebuildSnapshot")]
    [InlineData("DataShortcuts.SetWorkingFolder(\"C:\\\\x\"); return 1;", "DataShortcuts")]
    [InlineData("var f = Autodesk.Civil.DataShortcuts.DataShortcuts.GetWorkingFolder(); return f;", "Autodesk.Civil.DataShortcuts")]
    [InlineData("var p = CivilApplication.SurveyProjects; return p.Count;", ".SurveyProjects")]
    [InlineData("s.ExportToDEM(\"f.dem\", \"UTM84-48N\", 1.0, default); return 1;", ".ExportToDEM")]
    [InlineData("var id = TinSurface.CreateFromLandXML(db, \"S\", \"f.xml\"); return 1;", ".CreateFromLandXML")]
    [InlineData("var id = TinSurface.CreateFromTin(db, \"f.tin\"); return 1;", ".CreateFromTin")]
    [InlineData("style.ExportTo(db2, default); return 1;", ".ExportTo")]
    [InlineData("CogoPointCollection.ExportPoints(\"C:\\pts.csv\", default, ObjectId.Null); return 1;", ".ExportPoints")]
    [InlineData("CogoPointCollection.ImportPoints(\"C:\\pts.csv\", default, ObjectId.Null); return 1;", ".ImportPoints")]
    [InlineData("var id = GridSurface.CreateFromDEM(db, \"f.dem\"); return 1;", ".CreateFromDEM")]
    [InlineData("string f = \"x\"; s.CreateSolidsAtDepthToFile(1.0, \"0\", 1, ref f); return 1;", ".CreateSolidsAtDepthToFile")]
    [InlineData("string f = \"x\"; s.CreateSolidsAtSurfaceToFile(id, \"0\", 1, ref f); return 1;", ".CreateSolidsAtSurfaceToFile")]
    [InlineData("var d = new Autodesk.Civil.AeccUiMgd.Roadway.Dialog(); return 1;", "Autodesk.Civil.AeccUiMgd")]
    [InlineData("Autodesk.AECC.Interop.Land.AeccApplication a = null; return 1;", "Autodesk.AECC.Interop")]
    // everything AutoCAD denies still applies
    [InlineData("var r = ed.GetPoint(\"p\"); return 1;", ".GetPoint")]
    [InlineData("var t = db.TransactionManager.StartTransaction(); return 1;", ".StartTransaction")]
    [InlineData("doc.SendStringToExecute(\"_LINE \", true, false, false); return 1;", ".SendStringToExecute")]
    [InlineData("MessageBox.Show(\"hi\"); return 1;", "MessageBox")]
    [InlineData("using Autodesk.AutoCAD.Interop; return 1;", "Autodesk.AutoCAD.Interop")]
    // and the base list
    [InlineData("System.IO.File.WriteAllText(\"x\", \"y\"); return 1;", "System.IO")]
    [InlineData("var e = Expression.Call(Expression.Constant(civil), \"GetAlignmentIds\", null); return 1;", "Expression")]
    public void Civil3d_guard_profile_denies_rebuilds_outside_state_file_members_dialogs_interop_and_the_autocad_list(string code, string expected)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Civil3d);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expected, StringComparison.Ordinal));
        Assert.All(violations, v => Assert.Contains("Civil 3D", v.Message));
    }

    [Fact]
    public void Civil3d_guard_profile_keeps_the_autocad_rule_that_tr_belongs_to_the_bridge()
    {
        // The identifier-scoped denial carries its own explanation instead of the host name, same as in AutoCAD.
        var violations = ScriptGuard.Check("tr.Commit(); return 1;", GuardProfile.Civil3d);

        Assert.Single(violations);
        Assert.Contains("tr.Commit", violations[0].Message);
        Assert.Equal(ScriptGuard.Check("tr.Commit(); return 1;", GuardProfile.Autocad)[0].Message, violations[0].Message);
    }

    [Fact]
    public void Civil3d_guard_profile_is_a_strict_superset_of_the_autocad_profile()
    {
        Assert.All(GuardProfile.Autocad.DeniedIdentifiers, name => Assert.Contains(name, GuardProfile.Civil3d.DeniedIdentifiers));
        Assert.All(GuardProfile.Autocad.DeniedMembers, name => Assert.Contains(name, GuardProfile.Civil3d.DeniedMembers));
        Assert.All(GuardProfile.Autocad.DeniedNamespaces, ns => Assert.Contains(ns, GuardProfile.Civil3d.DeniedNamespaces));
        Assert.Equal(GuardProfile.Autocad.DeniedMembersOnIdentifier, GuardProfile.Civil3d.DeniedMembersOnIdentifier);
        Assert.True(GuardProfile.Civil3d.DeniedMembers.Count > GuardProfile.Autocad.DeniedMembers.Count);
        Assert.Equal("Civil 3D", GuardProfile.Civil3d.HostName);
        Assert.Equal("AutoCAD", GuardProfile.Autocad.HostName);
    }

    [Fact]
    public void Civil3d_guard_profile_lets_reads_the_two_seed_writes_and_rebuild_settings_through()
    {
        const string reads = """
            var ids = civil.GetAlignmentIds();
            var count = 0;
            foreach (ObjectId id in ids)
            {
                var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
                double e = 0, n = 0; a.PointLocation(a.StartingStation, 0, ref e, ref n);
                log(a.Name + " " + a.GetStationStringWithEquations(a.StartingStation) + " " + units.ToMm(a.Length));
                count += a.Entities.Count;
            }
            var s = (TinSurface)tr.GetObject(civil.GetSurfaceIds()[0], OpenMode.ForRead);
            var z = s.FindElevationAtXY(1.0, 2.0);
            var props = s.GetGeneralProperties();
            var c = (Corridor)tr.GetObject(civil.CorridorCollection[0], OpenMode.ForRead);
            var stale = c.IsOutOfDate || c.RebuildAutomatic || s.AutoRebuild;
            var unit = civil.Settings.DrawingSettings.UnitZoneSettings.DrawingUnits;
            var styles = civil.Styles.AlignmentStyles.Count;
            return new { count, z, stale, unit = unit.ToString(), styles, min = props.MinimumElevation };
            """;
        const string writes = """
            var pid = civil.CogoPoints.Add(new Point3d(units.ToDrawing(args.Double("x")), units.ToDrawing(args.Double("y")), 10.0), "MCP", true);
            civil.CogoPoints.SetElevation(pid, 11.0);
            var opts = new PolylineOptions { PlineId = plineId, AddCurvesBetweenTangents = false, EraseExistingEntities = false };
            var aid = Alignment.Create(civil, opts, args.Str("name"), "", "0", "Standard", "");
            var a = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
            a.ImportLabelSet("Major Minor and Geometry Points");
            return a.Length;
            """;

        Assert.Empty(ScriptGuard.Check(reads, GuardProfile.Civil3d));
        Assert.Empty(ScriptGuard.Check(writes, GuardProfile.Civil3d));
    }

    [Fact]
    public void Civil3d_analyzer_profile_applies_the_autocad_transaction_rule()
    {
        Assert.True(ScriptAnalyzer.Analyze("var t = db.TransactionManager.StartTransaction(); return 1;", AnalyzerProfile.Civil3d).UsesTransaction);
        Assert.True(ScriptAnalyzer.Analyze("using var t = db.TransactionManager.StartOpenCloseTransaction(); return 1;", AnalyzerProfile.Civil3d).UsesTransaction);
        Assert.False(ScriptAnalyzer.Analyze("var a = (Alignment)tr.GetObject(id, OpenMode.ForRead); return a.Length;", AnalyzerProfile.Civil3d).UsesTransaction);
        Assert.Equal(AnalyzerProfile.Autocad.TransactionMethodNames, AnalyzerProfile.Civil3d.TransactionMethodNames);
        Assert.Empty(AnalyzerProfile.Civil3d.TransactionTypeNames);
    }

    [Fact]
    public void Civil3d_info_round_trips_in_camel_case_and_is_omitted_when_null()
    {
        var context = new ContextResult
        {
            Host = "civil3d", HostVersion = "2026",
            Civil3d = new Civil3dInfo("Civil3D", true, "Meters", "UTM84-48N", false, 3, 2, 1, 1, 0, 250),
        };

        var json = BridgeJson.Serialize(context);
        using var doc = JsonDocument.Parse(json);
        var civil = doc.RootElement.GetProperty("civil3d");
        Assert.Equal("Civil3D", civil.GetProperty("product").GetString());
        Assert.True(civil.GetProperty("isCivilDocument").GetBoolean());
        Assert.Equal("Meters", civil.GetProperty("drawingUnit").GetString());
        Assert.Equal(3, civil.GetProperty("alignmentCount").GetInt32());
        Assert.Equal(250, civil.GetProperty("cogoPointCount").GetInt32());
        Assert.Equal(11, civil.EnumerateObject().Count());

        var back = BridgeJson.Deserialize<ContextResult>(json)!;
        Assert.Equal(context.Civil3d, back.Civil3d);

        var withoutCivil = BridgeJson.Serialize(new ContextResult { Host = "autocad" });
        Assert.DoesNotContain("civil3d", withoutCivil);
    }

    [Fact]
    public async Task Context_shape_for_civil3d_drops_revit_fields_and_keeps_both_the_autocad_and_civil_blocks()
    {
        var pipe = "hpcivil3d-mcp-test-" + Guid.NewGuid().ToString("N");
        var executor = new FakeRevitExecutor
        {
            ContextHandler = _ => new ContextResult
            {
                RevitVersion = "2026", Host = "civil3d", HostVersion = "2026", DocTitle = "Align-1.dwg", DocPath = "<path>",
                Autocad = new AutocadInfo("Feet", "Imperial", "Model", "0", true, true, true),
                Civil3d = new Civil3dInfo("Civil3D", true, "Feet", null, true, 4, 1, 0, 0, 0, 0),
            },
        };
        using var listener = new PipeListener(pipe, new RequestDispatcher(executor, new BridgeSettings(), "2026", "Civil 3D"));
        listener.Start();
        await using var client = new RevitBridgeClient(
            Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 }),
            NullLogger<RevitBridgeClient>.Instance, Civil3d());

        var text = await new ContextService(client, new ResultFormatter()).ReadAsync(false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.Equal("civil3d", root.GetProperty("host").GetString());
        Assert.Equal("Feet", root.GetProperty("autocad").GetProperty("insunits").GetString());
        Assert.Equal("Feet", root.GetProperty("civil3d").GetProperty("drawingUnit").GetString());
        Assert.True(root.GetProperty("civil3d").GetProperty("insunitsMismatch").GetBoolean());
        Assert.False(root.GetProperty("civil3d").TryGetProperty("coordinateSystemCode", out _));
        Assert.False(root.TryGetProperty("navis", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        await listener.StopAsync();
    }

    /// <summary>The Civil 3D host profile the engine tests use; the real one lives in the HPCivil3d server exe.</summary>
    private static HostProfile Civil3d() => new HostProfile
    {
        HostId = PipeNaming.Civil3dHost, DisplayName = "Civil 3D", ServerName = "test", ProductFolder = "HPCivil3dTest", EnvPrefix = "X_",
        DefaultVersion = 2026, ValidVersions = new[] { 2026 }, MethodPrefix = JsonRpcMethods.Civil3dPrefix,
        ExecuteToolName = "execute_civil3d_code", ContextToolName = "get_civil3d_context", ResourceScheme = "civil3d",
        Categories = new[] { "Document", "Alignment", "Profile", "Surface", "Corridor", "Pipe", "Parcel", "Point", "Data", "Generic" },
        CoreToolNames = new[] { "execute_civil3d_code", "get_civil3d_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.Civil3dImports, ScriptContractSummary = "test", HostAssembly = typeof(Civil3dProfileTests).Assembly,
    };
}
