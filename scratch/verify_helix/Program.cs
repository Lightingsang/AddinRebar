using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Threading;

namespace VerifyHelix;

class Program
{
    [STAThread]
    static int Main()
    {
        Console.WriteLine("=== Milestone M1: HelixToolkit Empirical Verification Harness ===");
        string baseDir = @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\bin";
        string debugDll = Path.Combine(baseDir, "Debug.R26", "HPRebar.dll");
        string releaseDll = Path.Combine(baseDir, "Release.R26", "HPRebar.dll");

        bool allPassed = true;

        // 1. Verify PE Metadata on both Debug.R26 and Release.R26
        allPassed &= VerifyPeManifest(debugDll, "Debug.R26");
        allPassed &= VerifyPeManifest(releaseDll, "Release.R26");

        // 2. Functional WPF & Geometry Test on STA thread
        allPassed &= VerifyFunctionalExecution(debugDll, "Debug.R26");
        allPassed &= VerifyFunctionalExecution(releaseDll, "Release.R26");

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"FINAL VERDICT: {(allPassed ? "APPROVE" : "REJECT")}");
        Console.WriteLine("==================================================");
        return allPassed ? 0 : 1;
    }

    static bool VerifyPeManifest(string dllPath, string label)
    {
        Console.WriteLine($"\n[TEST 1] PE Manifest & Dependency Audit for {label}:");
        if (!File.Exists(dllPath))
        {
            Console.WriteLine($"  [FAIL] Assembly not found: {dllPath}");
            return false;
        }

        using var fs = File.OpenRead(dllPath);
        using var pe = new PEReader(fs);
        var meta = pe.GetMetadataReader();

        // Check references
        bool foundSharpDx = false;
        bool foundExternalHelix = false;
        foreach (var handle in meta.AssemblyReferences)
        {
            var refDef = meta.GetAssemblyReference(handle);
            string name = meta.GetString(refDef.Name);
            if (name.Contains("SharpDX", StringComparison.OrdinalIgnoreCase)) foundSharpDx = true;
            if (name.Contains("HelixToolkit", StringComparison.OrdinalIgnoreCase)) foundExternalHelix = true;
        }

        if (foundSharpDx)
        {
            Console.WriteLine("  [FAIL] Unwanted SharpDX native dependency reference found!");
            return false;
        }
        Console.WriteLine("  [PASS] Clean assembly manifest: Zero SharpDX native references.");

        if (foundExternalHelix)
        {
            Console.WriteLine("  [FAIL] External HelixToolkit assembly reference found!");
            return false;
        }
        Console.WriteLine("  [PASS] Clean assembly manifest: Zero external HelixToolkit references (fully repacked).");

        // Check Type Definitions
        var typeNames = meta.TypeDefinitions.Select(h =>
        {
            var td = meta.GetTypeDefinition(h);
            string ns = meta.GetString(td.Namespace);
            string n = meta.GetString(td.Name);
            return string.IsNullOrEmpty(ns) ? n : $"{ns}.{n}";
        }).ToHashSet();

        string[] requiredTypes = new[]
        {
            "HelixToolkit.Wpf.HelixViewport3D",
            "HelixToolkit.Wpf.DefaultLights",
            "HelixToolkit.Geometry.MeshBuilder",
            "HelixToolkit.Geometry.MeshGeometry3D",
            "HelixToolkit.Wpf.LinesVisual3D",
            "HelixToolkit.Wpf.TubeVisual3D",
            "HelixToolkit.Wpf.MeshGeometryVisual3D"
        };

        foreach (var req in requiredTypes)
        {
            if (typeNames.Contains(req))
            {
                Console.WriteLine($"  [PASS] Confirmed repacked type in PE: {req}");
            }
            else
            {
                Console.WriteLine($"  [FAIL] Missing required type in PE: {req}");
                return false;
            }
        }

        int helixCount = typeNames.Count(t => t.StartsWith("HelixToolkit"));
        Console.WriteLine($"  [PASS] Total repacked HelixToolkit types in PE: {helixCount}");
        return true;
    }

    static bool VerifyFunctionalExecution(string dllPath, string label)
    {
        Console.WriteLine($"\n[TEST 2] Functional STA Instantiation & Geometry Operations for {label}:");
        bool passed = false;
        var thread = new Thread(() =>
        {
            try
            {
                var alc = new AssemblyLoadContext($"ALC_{label}", isCollectible: true);
                var asm = alc.LoadFromAssemblyPath(dllPath);

                // 1. MeshBuilder & 3D Geometry
                var mbType = asm.GetType("HelixToolkit.Geometry.MeshBuilder")!;
                dynamic mb = Activator.CreateInstance(mbType)!;
                mb.AddBox(new Vector3(0, 0, 0), 200.0f, 300.0f, 500.0f);
                mb.AddCylinder(new Vector3(0, 0, 0), new Vector3(0, 0, 500), 16.0f, 16);
                dynamic mesh = mb.ToMesh();
                int posCount = mesh.Positions.Count;
                int triCount = mesh.TriangleIndices.Count;
                Console.WriteLine($"  [PASS] HelixToolkit.Geometry.MeshBuilder created mesh with {posCount} vertices and {triCount} indices.");

                // 2. HelixViewport3D & DefaultLights
                var vpType = asm.GetType("HelixToolkit.Wpf.HelixViewport3D")!;
                var dlType = asm.GetType("HelixToolkit.Wpf.DefaultLights")!;
                dynamic viewport = Activator.CreateInstance(vpType)!;
                dynamic lights = Activator.CreateInstance(dlType)!;
                viewport.Children.Add(lights);
                Console.WriteLine($"  [PASS] HelixToolkit.Wpf.HelixViewport3D instantiated in STA and DefaultLights attached (Children count: {viewport.Children.Count}).");

                // 3. LinesVisual3D & TubeVisual3D
                var linesType = asm.GetType("HelixToolkit.Wpf.LinesVisual3D")!;
                var tubeType = asm.GetType("HelixToolkit.Wpf.TubeVisual3D")!;
                dynamic lines = Activator.CreateInstance(linesType)!;
                dynamic tube = Activator.CreateInstance(tubeType)!;
                viewport.Children.Add(lines);
                viewport.Children.Add(tube);
                Console.WriteLine($"  [PASS] LinesVisual3D and TubeVisual3D successfully instantiated and added to viewport (Children count: {viewport.Children.Count}).");

                alc.Unload();
                passed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL] Functional execution threw exception: {ex}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return passed;
    }
}
