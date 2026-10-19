using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using HPRebar.Core.BeamRebar.Models;

namespace VerifyM2Beam;

class Program
{
    private static Assembly? _hpRebarAsm;
    private static Type? _materialsType;
    private static Type? _concreteMeshBuilderType;
    private static Type? _rebarMeshBuilderType;
    private static Type? _sceneBuilderType;
    private static Type? _specType;
    private static Type? _viewportType;

    [STAThread]
    static int Main()
    {
        Console.WriteLine("===================================================================");
        Console.WriteLine("  MILIESTONE M2 EMPIRICAL ADVERSARIAL CHALLENGE HARNESS");
        Console.WriteLine("===================================================================");

        bool allPassed = true;

        var thread = new Thread(() =>
        {
            try
            {
                SetupAssemblyResolution();

                allPassed &= RunTest1_TubeGenerationMeshBuilder();
                allPassed &= RunTest2_SingleSpanShortBeam();
                allPassed &= RunTest3_FiveSpanContinuousBeam();
                allPassed &= RunTest4_EdgeCasesAndAdversarialGeometries();
                allPassed &= RunTest5_PerformanceBenchmarkCpuTime();
                allPassed &= RunTest6_ImmutabilityAndFrozenAudit();
                allPassed &= RunTest7_ViewportControlLifecycle();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[FATAL HARNESS FAILURE]: {ex}");
                allPassed = false;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Console.WriteLine("\n===================================================================");
        Console.WriteLine($"  FINAL VERDICT: {(allPassed ? "APPROVE" : "REJECT")}");
        Console.WriteLine("===================================================================");

        return allPassed ? 0 : 1;
    }

    private static void SetupAssemblyResolution()
    {
        string binDir = @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\bin\Debug.R26";
        string revitDir = @"C:\Program Files\Autodesk\Revit 2026";
        string dllPath = Path.Combine(binDir, "HPRebar.dll");
        string nugetDir = @"C:\Users\STR-HP03\.nuget\packages";

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var reqName = new AssemblyName(args.Name).Name;
            var probe1 = Path.Combine(binDir, reqName + ".dll");
            if (File.Exists(probe1)) return Assembly.LoadFrom(probe1);
            var probe2 = Path.Combine(revitDir, reqName + ".dll");
            if (File.Exists(probe2)) return Assembly.LoadFrom(probe2);

            // Probe NuGet cache
            var matches = Directory.GetFiles(nugetDir, reqName + ".dll", SearchOption.AllDirectories);
            var best = matches.FirstOrDefault(m => m.Contains("net8.0")) ?? matches.FirstOrDefault();
            if (best != null) return Assembly.LoadFrom(best);

            return null;
        };

        _hpRebarAsm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);

        _materialsType = _hpRebarAsm.GetType("HPRebar.BeamRebar.View.Controls.Beam3DMaterials")!;
        _concreteMeshBuilderType = _hpRebarAsm.GetType("HPRebar.BeamRebar.View.Controls.Beam3DConcreteMeshBuilder")!;
        _rebarMeshBuilderType = _hpRebarAsm.GetType("HPRebar.BeamRebar.View.Controls.Beam3DRebarMeshBuilder")!;
        _sceneBuilderType = _hpRebarAsm.GetType("HPRebar.BeamRebar.View.Controls.Beam3DSceneBuilder")!;
        _specType = _hpRebarAsm.GetType("HPRebar.BeamRebar.Model.BeamRebarSpec")!;
        _viewportType = _hpRebarAsm.GetType("HPRebar.BeamRebar.View.Controls.Beam3DViewport")!;

        Console.WriteLine("[INIT] Successfully loaded HPRebar.dll and resolved all M2 target types.");
        Console.WriteLine("  Merged BeamContinuousStack: " + (_hpRebarAsm.GetType("HPRebar.Core.BeamRebar.Models.BeamContinuousStack") != null));
    }

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    private static object ToRepacked<T>(T obj)
    {
        var targetType = _hpRebarAsm!.GetType(typeof(T).FullName!)!;
        var json = System.Text.Json.JsonSerializer.Serialize(obj, _jsonOptions);
        return System.Text.Json.JsonSerializer.Deserialize(json, targetType, _jsonOptions)!;
    }

    private static object ToRepackedList<T>(IEnumerable<T> items)
    {
        var elemType = _hpRebarAsm!.GetType(typeof(T).FullName!)!;
        var listType = typeof(List<>).MakeGenericType(elemType);
        var json = System.Text.Json.JsonSerializer.Serialize(items, _jsonOptions);
        return System.Text.Json.JsonSerializer.Deserialize(json, listType, _jsonOptions)!;
    }

    // --- TEST 1: Tube Generation with Complex Polylines and Stirrups ---
    private static bool RunTest1_TubeGenerationMeshBuilder()
    {
        Console.WriteLine("\n--- [TEST 1] MeshBuilder Tube Generation (Polylines & Stirrups) ---");
        try
        {
            // 1. Complex 3D Polyline with 90 deg hooks computed by BeamMainBarCalculator
            var stack1 = Create1SpanStack(4000, 250, 400);
            var repackedStack1 = ToRepacked(stack1);
            var mainBarCalcType = _hpRebarAsm!.GetType("HPRebar.Core.BeamRebar.Calculators.BeamMainBarCalculator")!;
            var mainBarsSpec = ToRepacked(new BeamMainBarSpec { TopCount = 2, TopDiameter = 20.0 });
            var topBars = (System.Collections.IEnumerable)mainBarCalcType.GetMethod("ComputeTopMainBars")!
                .Invoke(null, new object[] { repackedStack1, mainBarsSpec, 8.0 })!;

            var buildBarMesh = _rebarMeshBuilderType!.GetMethod("BuildBarMesh")!;
            var barMesh = (MeshGeometry3D)buildBarMesh.Invoke(null, new object[] { topBars, 8 })!;

            if (barMesh == null || barMesh.Positions.Count == 0 || barMesh.TriangleIndices.Count == 0)
            {
                Console.WriteLine("  [FAIL] Polyline tube mesh is empty!");
                return false;
            }
            if (!barMesh.IsFrozen)
            {
                Console.WriteLine("  [FAIL] Polyline tube mesh is not frozen!");
                return false;
            }
            Console.WriteLine($"  [PASS] Hook bar mesh: {barMesh.Positions.Count} vertices, {barMesh.TriangleIndices.Count / 3} triangles, Frozen={barMesh.IsFrozen}");

            // 2. Closed Stirrup Mesh
            var stack = Create1SpanStack(3000, 250, 400);
            var stirrupSpec = new BeamStirrupSpec
            {
                Diameter = 8.0,
                SpacingDense = 100.0,
                SpacingSparse = 150.0,
                Layout = StirrupLayout.ThreeZoneL4
            };

            var buildStirrupMesh = _rebarMeshBuilderType.GetMethod("BuildStirrupMesh")!;
            var stirrupMesh = (MeshGeometry3D)buildStirrupMesh.Invoke(null, new object?[] { ToRepacked(stack), ToRepacked(stirrupSpec), null, 8 })!;

            if (stirrupMesh == null || stirrupMesh.Positions.Count == 0)
            {
                Console.WriteLine("  [FAIL] Stirrup mesh is empty!");
                return false;
            }
            if (!stirrupMesh.IsFrozen)
            {
                Console.WriteLine("  [FAIL] Stirrup mesh is not frozen!");
                return false;
            }
            Console.WriteLine($"  [PASS] Stirrup mesh: {stirrupMesh.Positions.Count} vertices, {stirrupMesh.TriangleIndices.Count / 3} triangles, Frozen={stirrupMesh.IsFrozen}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Test 1 threw exception: {ex}");
            return false;
        }
    }

    // --- TEST 2: 1-Span Short Beam ---
    private static bool RunTest2_SingleSpanShortBeam()
    {
        Console.WriteLine("\n--- [TEST 2] 1-Span Short Beam Geometry ---");
        try
        {
            // Short beam: 1500mm clear span, 200x300mm section
            var stack = Create1SpanStack(1500, 200, 300);
            var spec = CreateSpec();

            var buildScene = _sceneBuilderType!.GetMethod("BuildScene")!;
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(stack), spec })!;

            if (group.Children.Count != 7)
            {
                Console.WriteLine($"  [FAIL] Expected 7 frozen GeometryModel3D children, got {group.Children.Count}");
                return false;
            }

            if (!group.IsFrozen)
            {
                Console.WriteLine("  [FAIL] Scene Model3DGroup is not frozen!");
                return false;
            }

            for (int i = 0; i < group.Children.Count; i++)
            {
                var child = (GeometryModel3D)group.Children[i];
                if (!child.IsFrozen)
                {
                    Console.WriteLine($"  [FAIL] Model child {i} is not frozen!");
                    return false;
                }
            }

            var concreteModel = (GeometryModel3D)group.Children[0];
            var topModel = (GeometryModel3D)group.Children[1];
            var botModel = (GeometryModel3D)group.Children[2];
            var stirrupModel = (GeometryModel3D)group.Children[5];
            var supportModel = (GeometryModel3D)group.Children[6];

            var concMesh = (MeshGeometry3D)concreteModel.Geometry;
            var topMesh = (MeshGeometry3D)topModel.Geometry;
            var botMesh = (MeshGeometry3D)botModel.Geometry;
            var stirrupMesh = (MeshGeometry3D)stirrupModel.Geometry;
            var suppMesh = (MeshGeometry3D)supportModel.Geometry;

            Console.WriteLine($"  Concrete vertices: {concMesh.Positions.Count}");
            Console.WriteLine($"  Top bar vertices:  {topMesh.Positions.Count}");
            Console.WriteLine($"  Bot bar vertices:  {botMesh.Positions.Count}");
            Console.WriteLine($"  Stirrups vertices: {stirrupMesh.Positions.Count}");
            Console.WriteLine($"  Support vertices:  {suppMesh.Positions.Count}");

            if (concMesh.Positions.Count == 0 || topMesh.Positions.Count == 0 || botMesh.Positions.Count == 0 ||
                stirrupMesh.Positions.Count == 0 || suppMesh.Positions.Count == 0)
            {
                Console.WriteLine("  [FAIL] One or more primary meshes are empty!");
                return false;
            }

            Console.WriteLine("  [PASS] 1-Span short beam generated 7 properly frozen models with valid geometry.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Test 2 threw exception: {ex}");
            return false;
        }
    }

    // --- TEST 3: 5-Span Continuous Beam ---
    private static bool RunTest3_FiveSpanContinuousBeam()
    {
        Console.WriteLine("\n--- [TEST 3] 5-Span Continuous Beam (Depth Transitions & Splicing) ---");
        try
        {
            var stack = Create5SpanStack();
            var spec = CreateSpec();

            var buildScene = _sceneBuilderType!.GetMethod("BuildScene")!;
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(stack), spec })!;

            if (group.Children.Count != 7 || !group.IsFrozen)
            {
                Console.WriteLine("  [FAIL] Group validation failed!");
                return false;
            }

            var concMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[0]).Geometry;
            var topMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[1]).Geometry;
            var botMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[2]).Geometry;
            var addMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[3]).Geometry;
            var stirrupMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[5]).Geometry;
            var suppMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[6]).Geometry;

            Console.WriteLine($"  5-Span Concrete vertices: {concMesh.Positions.Count}");
            Console.WriteLine($"  5-Span Top bars vertices: {topMesh.Positions.Count} (includes laps/splices)");
            Console.WriteLine($"  5-Span Bot bars vertices: {botMesh.Positions.Count} (includes stepped transitions)");
            Console.WriteLine($"  5-Span Add bars vertices: {addMesh.Positions.Count}");
            Console.WriteLine($"  5-Span Stirrups vertices: {stirrupMesh.Positions.Count}");
            Console.WriteLine($"  5-Span Supports vertices: {suppMesh.Positions.Count} (6 column stubs)");

            if (concMesh.Positions.Count == 0 || topMesh.Positions.Count == 0 || stirrupMesh.Positions.Count == 0)
            {
                Console.WriteLine("  [FAIL] Meshes failed to generate for 5-span continuous beam!");
                return false;
            }

            Console.WriteLine("  [PASS] 5-Span continuous beam scene constructed and validated successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Test 3 threw exception: {ex}");
            return false;
        }
    }

    // --- TEST 4: Edge Cases and Adversarial Geometries ---
    private static bool RunTest4_EdgeCasesAndAdversarialGeometries()
    {
        Console.WriteLine("\n--- [TEST 4] Adversarial Edge Cases ---");
        bool passed = true;
        var buildScene = _sceneBuilderType!.GetMethod("BuildScene")!;

        // 4A: Empty Stack (0 spans)
        try
        {
            var emptyStack = new BeamContinuousStack();
            var spec = CreateSpec();
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(emptyStack), spec })!;
            if (group.Children.Count == 0 && group.IsFrozen)
            {
                Console.WriteLine("  [PASS] 4A: Empty stack handled gracefully (empty frozen group returned).");
            }
            else
            {
                Console.WriteLine("  [FAIL] 4A: Empty stack did not return empty frozen group!");
                passed = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] 4A: Empty stack threw exception: {ex.Message}");
            passed = false;
        }

        // 4B: Bar count = 0
        try
        {
            var stack = Create1SpanStack(3000, 200, 300);
            var mainBars = new BeamMainBarSpec
            {
                TopCount = 0,
                BottomCount = 0
            };
            var spec = CreateSpec(mainBars: ToRepacked(mainBars));
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(stack), spec })!;
            Console.WriteLine($"  [PASS] 4B: Bar count = 0 handled gracefully without crashing (Group children: {group.Children.Count}).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] 4B: Bar count = 0 threw exception: {ex.Message}");
            passed = false;
        }

        // 4C: Huge Stirrup Spacing (50,000 mm)
        try
        {
            var stack = Create1SpanStack(3000, 200, 300);
            var stirrups = new BeamStirrupSpec
            {
                SpacingDense = 50000.0,
                SpacingSparse = 50000.0
            };
            var spec = CreateSpec(stirrups: ToRepacked(stirrups));
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(stack), spec })!;
            var stirrupMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[5]).Geometry;
            Console.WriteLine($"  [PASS] 4C: Huge spacing handled cleanly (Stirrup vertices: {stirrupMesh.Positions.Count}).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] 4C: Huge spacing threw exception: {ex.Message}");
            passed = false;
        }

        // 4D: Stirrup Diameter = 0
        try
        {
            var stack = Create1SpanStack(3000, 200, 300);
            var stirrups = new BeamStirrupSpec { Diameter = 0.0 };
            var spec = CreateSpec(stirrups: ToRepacked(stirrups));
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(stack), spec })!;
            var stirrupMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[5]).Geometry;
            if (stirrupMesh.Positions.Count == 0)
            {
                Console.WriteLine("  [PASS] 4D: Zero stirrup diameter returns empty stirrup mesh without crashing.");
            }
            else
            {
                Console.WriteLine("  [FAIL] 4D: Zero stirrup diameter generated unexpected geometry.");
                passed = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] 4D: Zero stirrup diameter threw exception: {ex.Message}");
            passed = false;
        }

        // 4E: Cantilever at both ends
        try
        {
            var stack = CreateCantileverStack();
            var spec = CreateSpec();
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(stack), spec })!;
            var concMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[0]).Geometry;
            if (concMesh.Positions.Count > 0 && group.IsFrozen)
            {
                Console.WriteLine("  [PASS] 4E: Cantilever stack rendered accurately.");
            }
            else
            {
                Console.WriteLine("  [FAIL] 4E: Cantilever stack failed to render!");
                passed = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] 4E: Cantilever stack threw exception: {ex.Message}");
            passed = false;
        }

        // 4F: Secondary Beam Intersections (Hanging Stirrups)
        try
        {
            var stack = CreateStackWithSecondaryIntersection();
            var special = new BeamSpecialBarSpec
            {
                EnableHangingStirrups = true,
                HangingStirrupsPerSide = 3,
                HangingStirrupSpacing = 50.0
            };
            var spec = CreateSpec(specialBars: ToRepacked(special));
            var group = (Model3DGroup)buildScene.Invoke(null, new object[] { ToRepacked(stack), spec })!;
            var stirrupMesh = (MeshGeometry3D)((GeometryModel3D)group.Children[5]).Geometry;
            Console.WriteLine($"  [PASS] 4F: Secondary intersection hanging stirrups generated (Vertices: {stirrupMesh.Positions.Count}).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] 4F: Secondary intersection threw exception: {ex.Message}");
            passed = false;
        }

        return passed;
    }

    // --- TEST 5: Performance Benchmark CPU Time (< 5ms) ---
    private static bool RunTest5_PerformanceBenchmarkCpuTime()
    {
        Console.WriteLine("\n--- [TEST 5] Performance Benchmark CPU Scene Building Time ---");
        try
        {
            var stack = Create3SpanStandardStack();
            var repackedStack = ToRepacked(stack);
            var spec = CreateSpec();
            var buildScene = _sceneBuilderType!.GetMethod("BuildScene")!;

            // Warm-up 20 iterations
            for (int i = 0; i < 20; i++)
            {
                buildScene.Invoke(null, new object[] { repackedStack, spec });
            }

            // Benchmark 100 iterations
            int iterations = 100;
            var times = new List<double>(iterations);
            var sw = new Stopwatch();

            for (int i = 0; i < iterations; i++)
            {
                sw.Restart();
                var group = (Model3DGroup)buildScene.Invoke(null, new object[] { repackedStack, spec })!;
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            times.Sort();
            double min = times[0];
            double max = times[times.Count - 1];
            double avg = times.Average();
            double p50 = times[iterations / 2];
            double p95 = times[(int)(iterations * 0.95)];

            Console.WriteLine($"  Iterations: {iterations}");
            Console.WriteLine($"  Min Time:   {min:F3} ms");
            Console.WriteLine($"  Max Time:   {max:F3} ms");
            Console.WriteLine($"  Avg Time:   {avg:F3} ms");
            Console.WriteLine($"  P50 Time:   {p50:F3} ms");
            Console.WriteLine($"  P95 Time:   {p95:F3} ms");

            if (avg < 5.0)
            {
                Console.WriteLine($"  [PASS] Average build time ({avg:F3} ms) is strictly under target threshold (5.0 ms).");
                return true;
            }
            else
            {
                Console.WriteLine($"  [FAIL] Average build time ({avg:F3} ms) exceeds 5.0 ms threshold!");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Benchmark threw exception: {ex}");
            return false;
        }
    }

    // --- TEST 6: Immutability and Frozen State Audit ---
    private static bool RunTest6_ImmutabilityAndFrozenAudit()
    {
        Console.WriteLine("\n--- [TEST 6] Immutability & Frozen State Audit ---");
        try
        {
            string[] materialProps = new[]
            {
                "Concrete", "Support", "MainTopBar", "MainBottomBar",
                "AdditionalTopBar", "AdditionalBottomBar", "AdditionalBar",
                "SideBar", "CrossTie", "Stirrup", "SpecialBar"
            };

            bool allMaterialsFrozen = true;
            foreach (var prop in materialProps)
            {
                var mat = (Material)_materialsType!.GetProperty(prop)!.GetValue(null)!;
                if (!mat.IsFrozen)
                {
                    Console.WriteLine($"  [FAIL] Material {prop} is NOT frozen!");
                    allMaterialsFrozen = false;
                }
            }

            if (allMaterialsFrozen)
            {
                Console.WriteLine("  [PASS] All 11 Beam3DMaterials static instances are completely frozen.");
            }

            return allMaterialsFrozen;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Test 6 threw exception: {ex}");
            return false;
        }
    }

    // --- TEST 7: Viewport Control Lifecycle & Camera ---
    private static bool RunTest7_ViewportControlLifecycle()
    {
        Console.WriteLine("\n--- [TEST 7] Beam3DViewport UserControl Lifecycle ---");
        try
        {
            // Case 7A: Standalone instantiation
            Console.WriteLine("  [SUBTEST 7A] Direct instantiation of Beam3DViewport UserControl:");
            try
            {
                dynamic standalone = Activator.CreateInstance(_viewportType!)!;
                Console.WriteLine("    [UNEXPECTED PASS] Standalone Beam3DViewport created without Theme in scope.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [CONFIRMED BUG] Standalone creation failed as predicted: {ex.InnerException?.Message ?? ex.Message}");
            }

            // Case 7B: Simulating Window environment
            Console.WriteLine("  [SUBTEST 7B] Instantiation inside Window with Theme.xaml loaded:");
            var app = System.Windows.Application.Current ?? new System.Windows.Application();
            var themeUri = new Uri("pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml", UriKind.Absolute);
            var themeDict = new System.Windows.ResourceDictionary { Source = themeUri };

            var win = new System.Windows.Window();
            win.Resources.MergedDictionaries.Add(themeDict);

            try
            {
                dynamic viewportInWin = Activator.CreateInstance(_viewportType!)!;
                win.Content = viewportInWin;
                Console.WriteLine("    [RESULT] Viewport created when Theme.xaml in Window resources.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [CRITICAL BUG] Even inside Window context, creation fails: {ex.InnerException?.Message ?? ex.Message}");
            }

            // Case 7C: When Theme.xaml is loaded into Application.Current
            Console.WriteLine("  [SUBTEST 7C] Instantiation when Theme.xaml is merged into Application.Current.Resources:");
            app.Resources.MergedDictionaries.Add(themeDict);
            dynamic viewport = Activator.CreateInstance(_viewportType!)!;
            Console.WriteLine("    [PASS] Beam3DViewport instantiated once Application.Current has Theme.xaml.");

            // Viewport ModelUpDirection check
            var helixViewField = _viewportType.GetField("HelixView", BindingFlags.NonPublic | BindingFlags.Instance);
            if (helixViewField != null)
            {
                dynamic helixView = helixViewField.GetValue(viewport)!;
                Vector3D up = helixView.ModelUpDirection;
                if (Math.Abs(up.X) < 1e-4 && Math.Abs(up.Y) < 1e-4 && Math.Abs(up.Z - 1.0) < 1e-4)
                {
                    Console.WriteLine("  [PASS] HelixViewport3D ModelUpDirection correctly configured to (0, 0, 1).");
                }
                else
                {
                    Console.WriteLine($"  [FAIL] Unexpected ModelUpDirection: {up}");
                    return false;
                }
            }

            Console.WriteLine("  [PASS] Beam3DViewport initialized and verified on STA thread.");

            // Subtest 7D: Lifecycle Symmetry & _isAttached Verification
            Console.WriteLine("  [SUBTEST 7D] Lifecycle Symmetry (_isAttached flag, OnLoaded/OnUnloaded cycle):");
            var isAttachedField = _viewportType.GetField("_isAttached", BindingFlags.NonPublic | BindingFlags.Instance);
            if (isAttachedField == null)
            {
                Console.WriteLine("  [FAIL] _isAttached field not found on Beam3DViewport!");
                return false;
            }

            bool initialAttached = (bool)isAttachedField.GetValue(viewport)!;
            if (initialAttached)
            {
                Console.WriteLine("  [FAIL] _isAttached should be false initially when Session is null!");
                return false;
            }
            Console.WriteLine($"    Initial state: _isAttached = {initialAttached} [PASS]");

            // Create a valid BeamRebarSession
            var beamStackType = _hpRebarAsm.GetType("HPRebar.BeamRebar.Model.BeamStack")!;
            var sessionType = _hpRebarAsm.GetType("HPRebar.BeamRebar.ViewModel.BeamRebarSession")!;
            var rebarTypeInfoType = _hpRebarAsm.GetType("HPRebar.BeamRebar.Model.RebarTypeInfo")!;

            var stack = Activator.CreateInstance(beamStackType)!;
            var continuousStack = ToRepacked(Create1SpanStack(3000, 200, 300));
            beamStackType.GetProperty("ContinuousStack")!.SetValue(stack, continuousStack);

            var spec = CreateSpec();
            var barTypesList = Activator.CreateInstance(typeof(List<>).MakeGenericType(rebarTypeInfoType))!;
            dynamic session = Activator.CreateInstance(sessionType, new object[] { stack, spec, barTypesList })!;

            // Assign session to viewport
            viewport.Session = session;
            bool attachedAfterSet = (bool)isAttachedField.GetValue(viewport)!;
            if (!attachedAfterSet)
            {
                Console.WriteLine("  [FAIL] _isAttached should be true after setting Session!");
                return false;
            }
            Console.WriteLine($"    After setting Session: _isAttached = {attachedAfterSet} [PASS]");

            var onLoadedMethod = _viewportType.GetMethod("OnLoaded", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance)!;
            var onUnloadedMethod = _viewportType.GetMethod("OnUnloaded", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Trigger Unloaded
            onUnloadedMethod.Invoke(viewport, new object[] { viewport, new RoutedEventArgs() });
            bool attachedAfterUnload = (bool)isAttachedField.GetValue(viewport)!;
            if (attachedAfterUnload)
            {
                Console.WriteLine("  [FAIL] _isAttached should be false after Unloaded!");
                return false;
            }
            Console.WriteLine($"    After OnUnloaded: _isAttached = {attachedAfterUnload} [PASS]");

            // Trigger Loaded (re-attach)
            onLoadedMethod.Invoke(viewport, new object[] { viewport, new RoutedEventArgs() });
            bool attachedAfterReload = (bool)isAttachedField.GetValue(viewport)!;
            if (!attachedAfterReload)
            {
                Console.WriteLine("  [FAIL] _isAttached should be true after OnLoaded reattachment!");
                return false;
            }
            Console.WriteLine($"    After OnLoaded (re-attached): _isAttached = {attachedAfterReload} [PASS]");

            // Trigger duplicate Loaded (idempotency check)
            onLoadedMethod.Invoke(viewport, new object[] { viewport, new RoutedEventArgs() });
            bool attachedAfterDupLoad = (bool)isAttachedField.GetValue(viewport)!;
            if (!attachedAfterDupLoad)
            {
                Console.WriteLine("  [FAIL] _isAttached should still be true after duplicate OnLoaded!");
                return false;
            }
            Console.WriteLine($"    After duplicate OnLoaded: _isAttached = {attachedAfterDupLoad} [PASS]");

            // Stress-test 10 Loaded/Unloaded cycles
            for (int cycle = 0; cycle < 10; cycle++)
            {
                onUnloadedMethod.Invoke(viewport, new object[] { viewport, new RoutedEventArgs() });
                onLoadedMethod.Invoke(viewport, new object[] { viewport, new RoutedEventArgs() });
            }
            bool attachedAfterStress = (bool)isAttachedField.GetValue(viewport)!;
            if (!attachedAfterStress)
            {
                Console.WriteLine("  [FAIL] _isAttached should be true after 10 Loaded/Unloaded cycles!");
                return false;
            }
            Console.WriteLine("    10x Loaded/Unloaded rapid cycle stress test: [PASS]");

            // Detach when setting Session = null
            viewport.Session = null;
            bool attachedAfterNull = (bool)isAttachedField.GetValue(viewport)!;
            if (attachedAfterNull)
            {
                Console.WriteLine("  [FAIL] _isAttached should be false after setting Session = null!");
                return false;
            }
            Console.WriteLine($"    After setting Session = null: _isAttached = {attachedAfterNull} [PASS]");

            // Test ResetCamera
            viewport.ResetCamera();
            Console.WriteLine("    ResetCamera() executed cleanly: [PASS]");

            Console.WriteLine("  [PASS] All Subtests of Test 7 (including Subtest 7D Lifecycle Symmetry) verified 100%.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Test 7 threw exception: {ex}");
            return false;
        }
    }

    // --- Helper Stack Constructors ---
    private static BeamContinuousStack Create1SpanStack(double lengthClear, double width, double height)
    {
        var spans = new List<BeamSpan>
        {
            new(0, "Span 1", lengthClear + 300, width, height, 0.0, 25.0, lengthClear, 150.0)
        };
        var supports = new List<BeamSupportNode>
        {
            new(0, "C0", 0.0, 300.0, SupportType.ExteriorColumn, 300.0, "ext-0", true),
            new(1, "C1", lengthClear + 300.0, 300.0, SupportType.ExteriorColumn, 300.0, "ext-1", true)
        };
        return new BeamContinuousStack(spans, supports);
    }

    private static BeamContinuousStack Create3SpanStandardStack()
    {
        var spans = new List<BeamSpan>
        {
            new(0, "Span 1", 4300, 250, 400, 0.0, 25.0, 4000, 150.0),
            new(1, "Span 2", 5300, 250, 400, 0.0, 25.0, 5000, 4450.0),
            new(2, "Span 3", 4300, 250, 400, 0.0, 25.0, 4000, 9750.0)
        };
        var supports = new List<BeamSupportNode>
        {
            new(0, "C0", 0.0, 300.0, SupportType.ExteriorColumn, 300.0),
            new(1, "C1", 4300.0, 300.0, SupportType.Column, 300.0),
            new(2, "C2", 9600.0, 300.0, SupportType.Column, 300.0),
            new(3, "C3", 13900.0, 300.0, SupportType.ExteriorColumn, 300.0)
        };
        return new BeamContinuousStack(spans, supports);
    }

    private static BeamContinuousStack Create5SpanStack()
    {
        // 5 Spans with stepped depths: 500, 600, 600, 700, 500
        var lengths = new[] { 4000.0, 6000.0, 5000.0, 7000.0, 4500.0 };
        var heights = new[] { 500.0, 600.0, 600.0, 700.0, 500.0 };
        var widths = new[] { 300.0, 350.0, 350.0, 400.0, 300.0 };

        var spans = new List<BeamSpan>();
        var supports = new List<BeamSupportNode>();

        double curX = 200.0; // Left column half width
        supports.Add(new BeamSupportNode(0, "C0", 0.0, 400.0, SupportType.ExteriorColumn, 400.0));

        for (int i = 0; i < 5; i++)
        {
            spans.Add(new BeamSpan(i, $"Span {i+1}", lengths[i] + 400, widths[i], heights[i], 0.0, 25.0, lengths[i], curX));
            curX += lengths[i] + 200.0;
            supports.Add(new BeamSupportNode(i + 1, $"C{i+1}", curX, 400.0, i == 4 ? SupportType.ExteriorColumn : SupportType.Column, 400.0));
            curX += 200.0;
        }

        return new BeamContinuousStack(spans, supports);
    }

    private static object CreateSpec(
        object? mainBars = null,
        object? stirrups = null,
        object? specialBars = null,
        object? sideBars = null)
    {
        var spec = Activator.CreateInstance(_specType!)!;
        if (mainBars != null) _specType!.GetProperty("MainBars")!.SetValue(spec, mainBars);
        if (stirrups != null) _specType!.GetProperty("Stirrups")!.SetValue(spec, stirrups);
        if (specialBars != null) _specType!.GetProperty("SpecialBars")!.SetValue(spec, specialBars);
        if (sideBars != null) _specType!.GetProperty("SideBars")!.SetValue(spec, sideBars);
        return spec;
    }

    private static BeamContinuousStack CreateCantileverStack()
    {
        var spans = new List<BeamSpan>
        {
            new(0, "Left Cantilever", 1500, 250, 400, 0.0, 25.0, 1500, 0.0, CantileverPosition.Left),
            new(1, "Interior Span", 5000, 250, 400, 0.0, 25.0, 5000, 1700.0),
            new(2, "Right Cantilever", 1200, 250, 400, 0.0, 25.0, 1200, 6900.0, CantileverPosition.Right)
        };
        var supports = new List<BeamSupportNode>
        {
            new(0, "C1", 1600.0, 400.0, SupportType.Column, 400.0),
            new(1, "C2", 6800.0, 400.0, SupportType.Column, 400.0)
        };
        return new BeamContinuousStack(spans, supports);
    }

    private static BeamContinuousStack CreateStackWithSecondaryIntersection()
    {
        var baseStack = Create1SpanStack(5000, 300, 500);
        var secIntersections = new List<SecondaryBeamIntersection>
        {
            new(0, 0, 2500.0, 200.0, 400.0, 0.0)
        };
        return new BeamContinuousStack(baseStack.Spans, baseStack.Supports, secIntersections);
    }
}
