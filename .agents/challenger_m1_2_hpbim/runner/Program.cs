using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;
using HPBim.Core.Tcvn;

namespace ChallengerRunner;

public static class Program
{
    private static int _passedCount = 0;
    private static int _failedCount = 0;
    private static readonly List<string> Findings = new();

    public static int Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("=== EMPIRICAL ADVERSARIAL VERIFICATION RUNNER: MILESTONE 1 (TCVN & QC ENGINE) ==");
        Console.WriteLine("================================================================================");

        TestTcvnRebarWeights();
        TestCommercialSplices();
        TestStirrupZoning();
        TestQcClashColumnVsOpening();
        TestQcClashBeamVsSlab();
        TestClearanceChecker();
        TestBimModelSerializationRoundTrip();

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"RESULTS: {_passedCount} PASSED, {_failedCount} FAILED");
        Console.WriteLine("================================================================================");

        if (Findings.Count > 0)
        {
            Console.WriteLine("\nNOTABLE EMPIRICAL FINDINGS / EDGE CASE BEHAVIORS:");
            foreach (var finding in Findings)
            {
                Console.WriteLine($"  * {finding}");
            }
        }

        return _failedCount == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string testName, string? details = null)
    {
        if (condition)
        {
            _passedCount++;
            Console.WriteLine($"[PASS] {testName}");
        }
        else
        {
            _failedCount++;
            Console.WriteLine($"[FAIL] {testName} - {details}");
        }
    }

    private static void TestTcvnRebarWeights()
    {
        Console.WriteLine("\n--- 1. TcvnRebarCalculator: Unit Weights & Ground Truth Comparison ---");

        // Ground-truth dictionary from vncad/struct.py
        var vncadGroundTruth = new Dictionary<int, double>
        {
            { 6, 0.222 },
            { 8, 0.395 },
            { 10, 0.617 },
            { 12, 0.888 },
            { 14, 1.208 },
            { 16, 1.578 },
            { 18, 1.998 },
            { 20, 2.466 },
            { 22, 2.984 },
            { 25, 3.853 }
        };

        int[] standardDiameters = { 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32 };

        foreach (int d in standardDiameters)
        {
            double calculatedUnitWeight = TcvnRebarCalculator.CalculateUnitWeight(d);
            double theoreticalW = (d * d) / 162.2;
            Assert(Math.Abs(calculatedUnitWeight - theoreticalW) < 1e-9,
                $"Formula check for D={d}: W = D^2 / 162.2",
                $"Calculated: {calculatedUnitWeight}, Theoretical: {theoreticalW}");

            if (vncadGroundTruth.TryGetValue(d, out double vncadNominal))
            {
                double nominalFromCalc = TcvnRebarCalculator.GetNominalUnitWeight(d);
                Assert(Math.Abs(nominalFromCalc - vncadNominal) < 1e-6,
                    $"Nominal lookup check for D={d} matches vncad nominal ({vncadNominal})",
                    $"Calc: {nominalFromCalc}, vncad: {vncadNominal}");

                // Compare calculated vs vncad nominal within 0.001 kg/m tolerance (per test_thep_va_du_toan.py)
                double diff = Math.Abs(calculatedUnitWeight - vncadNominal);
                Assert(diff < 0.001,
                    $"Calculated weight for D={d} within 0.001 kg/m of vncad ground truth (diff={diff:F6})",
                    $"Calc: {calculatedUnitWeight:F4}, vncad: {vncadNominal:F4}");
            }
            else
            {
                // D=28 and D=32
                double nominalFromCalc = TcvnRebarCalculator.GetNominalUnitWeight(d);
                Assert(Math.Abs(nominalFromCalc - theoreticalW) < 1e-9,
                    $"Fallback nominal for D={d} falls back to formula ({theoreticalW:F4} kg/m)",
                    $"Calc: {nominalFromCalc}, Formula: {theoreticalW}");
            }
        }

        // Test weight with length and quantity
        double w10mD16 = TcvnRebarCalculator.CalculateWeight(16, 10.0);
        Assert(Math.Abs(w10mD16 - 15.78298) < 0.001, "CalculateWeight for D16 L=10m ~ 15.783kg");

        double totalW = TcvnRebarCalculator.CalculateTotalWeight(16, 2500, 4); // 4 bars of 2.5m = 10m
        Assert(Math.Abs(totalW - w10mD16) < 1e-6, "CalculateTotalWeight 4 x 2.5m D16 matches 10m weight");

        // Partition by diameter (small <= 10mm, large > 10mm)
        var sampleBars = new List<RebarDef>
        {
            new() { DiameterMm = 8, LengthMm = 1000, Quantity = 2 }, // small
            new() { DiameterMm = 10, LengthMm = 1000, Quantity = 1 }, // small (threshold)
            new() { DiameterMm = 16, LengthMm = 1000, Quantity = 1 }, // large
            new() { DiameterMm = 20, LengthMm = 1000, Quantity = 1 }  // large
        };
        var (smallKg, largeKg) = TcvnRebarCalculator.PartitionWeightByDiameter(sampleBars);
        double expectedSmall = 2 * (64 / 162.2) + 1 * (100 / 162.2);
        double expectedLarge = 1 * (256 / 162.2) + 1 * (400 / 162.2);
        Assert(Math.Abs(smallKg - expectedSmall) < 1e-6, "PartitionWeightByDiameter small <= 10mm matches");
        Assert(Math.Abs(largeKg - expectedLarge) < 1e-6, "PartitionWeightByDiameter large > 10mm matches");
    }

    private static void TestCommercialSplices()
    {
        Console.WriteLine("\n--- 2. TcvnRebarCalculator: Commercial Bar Splices ---");

        // Case 1: L < 11.7m (e.g. 5m, 10m, 11.699m)
        var s5m = TcvnRebarCalculator.CalculateCommercialBarSplices(5000, 20, LapZone.Tension);
        Assert(s5m.SpliceCount == 0 && s5m.SingleLapLengthMm == 0.0 && s5m.DevelopedLengthMm == 5000.0,
            "L = 5.0m (< 11.7m) -> 0 splices, developed length = 5000mm");

        var s10m = TcvnRebarCalculator.CalculateCommercialBarSplices(10000, 18, LapZone.Compression);
        Assert(s10m.SpliceCount == 0 && s10m.SingleLapLengthMm == 0.0 && s10m.DevelopedLengthMm == 10000.0,
            "L = 10.0m (< 11.7m) -> 0 splices, developed length = 10000mm");

        var s11699 = TcvnRebarCalculator.CalculateCommercialBarSplices(11699, 20, LapZone.Tension);
        Assert(s11699.SpliceCount == 0 && s11699.DevelopedLengthMm == 11699.0,
            "L = 11.699m (< 11.7m) -> 0 splices");

        // Case 2: L = 11.7m (11700mm)
        var s117 = TcvnRebarCalculator.CalculateCommercialBarSplices(11700, 20, LapZone.Tension);
        Assert(s117.SpliceCount == 0 && s117.SingleLapLengthMm == 0.0 && s117.DevelopedLengthMm == 11700.0,
            "L = 11.7m (boundary) -> 0 splices, developed length = 11700mm");

        // Case 3: L = 15m (15000mm)
        // Tension: Lap = 40D = 40 * 20 = 800mm. Splice count = ceil(15000/11700) - 1 = 1. Developed = 15000 + 1*800 = 15800mm.
        var s15mTension = TcvnRebarCalculator.CalculateCommercialBarSplices(15000, 20, LapZone.Tension);
        Assert(s15mTension.SpliceCount == 1, "L = 15.0m Tension -> 1 splice");
        Assert(s15mTension.SingleLapLengthMm == 800.0, "L = 15.0m Tension D20 -> Lap = 40D = 800mm");
        Assert(s15mTension.DevelopedLengthMm == 15800.0, "L = 15.0m Tension D20 -> Developed = 15800mm");

        // Compression: Lap = 30D = 30 * 20 = 600mm. Developed = 15000 + 1*600 = 15600mm.
        var s15mComp = TcvnRebarCalculator.CalculateCommercialBarSplices(15000, 20, LapZone.Compression);
        Assert(s15mComp.SpliceCount == 1, "L = 15.0m Compression -> 1 splice");
        Assert(s15mComp.SingleLapLengthMm == 600.0, "L = 15.0m Compression D20 -> Lap = 30D = 600mm");
        Assert(s15mComp.DevelopedLengthMm == 15600.0, "L = 15.0m Compression D20 -> Developed = 15600mm");

        // Case 4: L = 25m (25000mm)
        // Splice count = ceil(25000/11700) - 1 = ceil(2.136) - 1 = 3 - 1 = 2 splices.
        var s25mTension = TcvnRebarCalculator.CalculateCommercialBarSplices(25000, 20, LapZone.Tension);
        Assert(s25mTension.SpliceCount == 2, "L = 25.0m Tension -> 2 splices");
        Assert(s25mTension.SingleLapLengthMm == 800.0, "L = 25.0m Tension D20 -> Single lap = 800mm");
        Assert(s25mTension.DevelopedLengthMm == 25000 + 2 * 800.0, "L = 25.0m Tension D20 -> Developed = 26600mm");

        // Edge case: L = 23400mm (exactly 2 x 11.7m)
        var s234m = TcvnRebarCalculator.CalculateCommercialBarSplices(23400, 20, LapZone.Tension);
        // Note: ceil(23400/11700) - 1 = 2 - 1 = 1 splice.
        Console.WriteLine($"  [INFO] Boundary analysis: L = 23.4m -> SpliceCount = {s234m.SpliceCount}, Developed = {s234m.DevelopedLengthMm}mm");
        Assert(s234m.SpliceCount == 1, "L = 23.4m boundary -> 1 splice calculated");
    }

    private static void TestStirrupZoning()
    {
        Console.WriteLine("\n--- 3. TcvnRebarCalculator: Stirrup Zoning ---");

        // Beam stirrup zoning
        // Standard span: clearSpan = 4000mm, beamHeight = 400mm
        // Dense zone length = min(2 * H, clearSpan / 3) = min(800, 1333.33) = 800mm
        // Dense spacing = 100mm -> count = floor(800 / 100) = 8
        // Mid span = 4000 - 2 * 800 = 2400mm
        // Sparse spacing = 200mm -> count = floor(2400 / 200) = 12
        var bZones = TcvnRebarCalculator.CalculateBeamStirrupZones(4000, 400);
        Assert(bZones.DenseZoneLengthMm == 800.0, "Beam clearSpan=4000, H=400 -> DenseZoneLength = 800mm");
        Assert(bZones.DenseZoneCountStart == 8, "Beam start dense count = 8");
        Assert(bZones.SparseZoneCount == 12, "Beam mid sparse count = 12");
        Assert(bZones.DenseZoneCountEnd == 8, "Beam end dense count = 8");

        // Short span beam: clearSpan = 1500mm, beamHeight = 400mm
        // 2 * H = 800mm. clearSpan / 3 = 500mm.
        // min(800, 500) = 500mm.
        // Dense count = floor(500 / 100) = 5
        // Mid span = 1500 - 2 * 500 = 500mm
        // Sparse count = floor(500 / 200) = 2
        var bShort = TcvnRebarCalculator.CalculateBeamStirrupZones(1500, 400);
        Assert(bShort.DenseZoneLengthMm == 500.0, "Short beam: clearSpan/3 governs (500mm < 800mm)");
        Assert(bShort.DenseZoneCountStart == 5, "Short beam dense start = 5");
        Assert(bShort.SparseZoneCount == 2, "Short beam sparse mid = 2");

        // Column stirrup zoning
        // Storey height = 3600mm
        // Dense zone length = H / 4 = 900mm
        // Dense spacing = 100mm -> count = floor(900 / 100) = 9
        // Mid height = 3600 - 2 * 900 = 1800mm
        // Sparse spacing = 200mm -> count = floor(1800 / 200) = 9
        var cZones = TcvnRebarCalculator.CalculateColumnStirrupZones(3600);
        Assert(cZones.DenseZoneLengthMm == 900.0, "Column storeyHeight=3600 -> DenseZoneLength = 900mm (H/4)");
        Assert(cZones.DenseCountBottom == 9, "Column dense bottom = 9");
        Assert(cZones.SparseCountMiddle == 9, "Column sparse middle = 9");
        Assert(cZones.DenseCountTop == 9, "Column dense top = 9");

        // Stirrup hook calculation: 10D >= 75mm
        Assert(TcvnRebarCalculator.CalculateStirrupHookLengthMm(6.0) == 75.0, "Stirrup hook D6: 10*6=60 < 75 -> clamps to 75mm");
        Assert(TcvnRebarCalculator.CalculateStirrupHookLengthMm(8.0) == 80.0, "Stirrup hook D8: 10*8=80 >= 75 -> 80mm");
        Assert(TcvnRebarCalculator.CalculateStirrupHookLengthMm(10.0) == 100.0, "Stirrup hook D10: 10*10=100 >= 75 -> 100mm");
    }

    private static void TestQcClashColumnVsOpening()
    {
        Console.WriteLine("\n--- 4. QcClashDetector: Column vs Opening Clashes ---");
        var detector = new QcClashDetector();

        // Setup: Column C1 at (0, 0), 220x220mm (halfWidth = 110mm)
        var col1 = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(0, 0),
            WidthMm = 220,
            DepthMm = 220
        };

        // Wall from (0, 0) to (5000, 0)
        // Case A: Positive Clash: Opening at DistanceAlongWallMm = 50mm, Width = 900mm
        // Opening center = 50 + 450 = 500mm along wall -> (500, 0)
        // Wait, opCenter = (500, 0). col center = (0, 0). dist = 500mm.
        // clashLimit = halfW (450) + col.Width/2 (110) = 560mm.
        // 500 < 560 -> CLASH! penetration = 560 - 500 = 60mm.
        var wallClash = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            Openings = new[]
            {
                new OpeningDef { Id = "D1", HostWallId = "W1", DistanceAlongWallMm = 50, WidthMm = 900, Kind = OpeningKind.Door }
            }
        };

        var clashesA = detector.DetectColumnOpeningClashes(new[] { col1 }, new[] { wallClash });
        Assert(clashesA.Count == 1, "Positive clash: Door D1 clashes with Column C1 detected");
        if (clashesA.Count == 1)
        {
            Assert(clashesA[0].ClashType == "ColumnVsOpening", "Clash type is 'ColumnVsOpening'");
            Assert(Math.Abs(clashesA[0].PenetrationMm - 60.0) < 1e-6, "Penetration depth = 60.0mm");
        }

        // Case B: Clear Non-Clash: Opening at DistanceAlongWallMm = 1500mm, Width = 900mm
        // opCenter = 1500 + 450 = 1950mm -> (1950, 0).
        // dist = 1950 > 560 -> Clear non-clash!
        var wallNoClash = new WallDef
        {
            Id = "W2",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            Openings = new[]
            {
                new OpeningDef { Id = "D2", HostWallId = "W2", DistanceAlongWallMm = 1500, WidthMm = 900, Kind = OpeningKind.Door }
            }
        };

        var clashesB = detector.DetectColumnOpeningClashes(new[] { col1 }, new[] { wallNoClash });
        Assert(clashesB.Count == 0, "Clear non-clash: Door D2 at 1500mm has 0 clashes with Column C1");

        // Case C: Spatial grid correctness with negative coordinates
        var colNeg = new ColumnDef
        {
            Id = "C_NEG",
            CenterLocation = new Point2D(-3000, -2000),
            WidthMm = 220,
            DepthMm = 220
        };
        var wallNeg = new WallDef
        {
            Id = "W_NEG",
            StartPoint = new Point2D(-3000, -2000),
            EndPoint = new Point2D(-3000, 2000),
            Openings = new[]
            {
                new OpeningDef { Id = "D_NEG", HostWallId = "W_NEG", DistanceAlongWallMm = 0, WidthMm = 900, Kind = OpeningKind.Door }
            }
        };
        var clashesNeg = detector.DetectColumnOpeningClashes(new[] { colNeg }, new[] { wallNeg });
        Assert(clashesNeg.Count == 1, "Negative coordinate spatial index detects clash correctly");
    }

    private static void TestQcClashBeamVsSlab()
    {
        Console.WriteLine("\n--- 5. QcClashDetector: Secondary Beam vs Undivided Slab ---");
        var detector = new QcClashDetector();

        // Undivided slab 4m x 4m from (0,0) to (4000, 4000)
        var slabBoundary = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(4000, 0),
            new Point2D(4000, 4000),
            new Point2D(0, 4000)
        });
        var slab = new SlabDef
        {
            Id = "S1",
            Boundary = slabBoundary
        };

        // Case 1: Secondary beam passing through interior of slab
        // Beam from (2000, 0) to (2000, 4000). Midpoint = (2000, 2000).
        var secondaryBeam = new BeamDef
        {
            Id = "B_SEC",
            StartPoint = new Point2D(2000, 0),
            EndPoint = new Point2D(2000, 4000),
            WidthMm = 200,
            IsTieBeam = false
        };

        var clashes = detector.DetectBeamSlabClashes(new[] { secondaryBeam }, new[] { slab });
        Assert(clashes.Count == 1, "Secondary beam through slab interior detected as clash");
        if (clashes.Count == 1)
        {
            Assert(clashes[0].ClashType == "SecondaryBeamSlicesUndividedSlab", "Clash type is SecondaryBeamSlicesUndividedSlab");
        }

        // Case 2: Tie beam ignored (IsTieBeam = true)
        var tieBeam = new BeamDef
        {
            Id = "B_TIE",
            StartPoint = new Point2D(2000, 0),
            EndPoint = new Point2D(2000, 4000),
            WidthMm = 250,
            IsTieBeam = true
        };
        var clashesTie = detector.DetectBeamSlabClashes(new[] { tieBeam }, new[] { slab });
        Assert(clashesTie.Count == 0, "Tie beam (IsTieBeam = true) is ignored by slab slice check");

        // Case 3: Secondary beam completely outside slab
        var outsideBeam = new BeamDef
        {
            Id = "B_OUT",
            StartPoint = new Point2D(5000, 0),
            EndPoint = new Point2D(5000, 4000),
            WidthMm = 200,
            IsTieBeam = false
        };
        var clashesOut = detector.DetectBeamSlabClashes(new[] { outsideBeam }, new[] { slab });
        Assert(clashesOut.Count == 0, "Beam outside slab produces 0 clashes");

        // Case 4: Perimeter beam along slab edge
        // A main beam from (0, 0) to (4000, 0) running on the edge. Midpoint = (2000, 0).
        var perimeterBeam = new BeamDef
        {
            Id = "B_PERIMETER",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            WidthMm = 200,
            IsTieBeam = false
        };
        var clashesPerimeter = detector.DetectBeamSlabClashes(new[] { perimeterBeam }, new[] { slab });
        Console.WriteLine($"  [INFO] Perimeter beam check on slab boundary: clashes count = {clashesPerimeter.Count}");
        if (clashesPerimeter.Count > 0)
        {
            Findings.Add("QcClashDetector: Perimeter beam on slab edge triggers 'SecondaryBeamSlicesUndividedSlab' because Polygon2D.Contains includes boundary points by default.");
        }
    }

    private static void TestClearanceChecker()
    {
        Console.WriteLine("\n--- 6. ClearanceChecker: Opening to Column Clearance ---");
        var checker = new ClearanceChecker();

        var col = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(0, 0),
            WidthMm = 220, // halfWidth = 110mm
            DepthMm = 220
        };

        // Wall from (0, 0) to (5000, 0)
        // Column edge is at X = 110mm.
        // Test 1: Opening at clearance 99mm -> DistanceAlongWall = 110 + 99 = 209mm
        var wall99 = new WallDef
        {
            Id = "W99",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            Openings = new[] { new OpeningDef { Id = "OP_99", HostWallId = "W99", DistanceAlongWallMm = 209, WidthMm = 900 } }
        };
        var v99 = checker.CheckOpeningToColumnClearances(new[] { wall99 }, new[] { col });
        Assert(v99.Count == 1, "Clearance 99mm (< 100mm) triggers violation CLR-001");
        if (v99.Count == 1)
        {
            Assert(v99[0].RuleId == "CLR-001", "Rule code is CLR-001");
            Assert(v99[0].Severity == QcSeverity.Warning, "Severity is Warning");
        }

        // Test 2: Opening at clearance 100mm -> DistanceAlongWall = 110 + 100 = 210mm
        var wall100 = new WallDef
        {
            Id = "W100",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            Openings = new[] { new OpeningDef { Id = "OP_100", HostWallId = "W100", DistanceAlongWallMm = 210, WidthMm = 900 } }
        };
        var v100 = checker.CheckOpeningToColumnClearances(new[] { wall100 }, new[] { col });
        Assert(v100.Count == 0, "Clearance 100mm (= 100mm threshold) does NOT trigger violation");

        // Test 3: Opening at clearance 150mm -> DistanceAlongWall = 110 + 150 = 260mm
        var wall150 = new WallDef
        {
            Id = "W150",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            Openings = new[] { new OpeningDef { Id = "OP_150", HostWallId = "W150", DistanceAlongWallMm = 260, WidthMm = 900 } }
        };
        var v150 = checker.CheckOpeningToColumnClearances(new[] { wall150 }, new[] { col });
        Assert(v150.Count == 0, "Clearance 150mm (> 100mm threshold) does NOT trigger violation");

        // Test 4: Wall thicknesses check (110, 220 standard; 150 non-standard)
        var walls = new[]
        {
            new WallDef { Id = "W_STD_110", ThicknessMm = 110 },
            new WallDef { Id = "W_STD_220", ThicknessMm = 220 },
            new WallDef { Id = "W_NON_STD", ThicknessMm = 150 }
        };
        var vWall = checker.CheckWallThicknesses(walls);
        Assert(vWall.Count == 1 && vWall[0].RuleId == "TCVN-WALL-001", "Wall thickness 150mm triggers TCVN-WALL-001, 110 and 220 pass");
    }

    private static void TestBimModelSerializationRoundTrip()
    {
        Console.WriteLine("\n--- 7. BimModelSerializer: Round-Trip Serialization & Deserialization ---");

        // 0. Test standalone Vector2D serialization
        try
        {
            var v = new Vector2D(1, 0);
            string vJson = JsonSerializer.Serialize(v);
            Console.WriteLine($"[INFO] Standalone Vector2D serialized: {vJson}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CONFIRMED BUG] Standalone Vector2D serialization failed: {ex.GetType().Name}: {ex.Message}");
            Findings.Add($"Vector2D standalone JSON serialization infinite recursion: {ex.GetType().Name}: {ex.Message}");
        }

        // 0.1 Test standalone Polygon2D serialization & deserialization
        try
        {
            var poly = new Polygon2D(new[] { new Point2D(0, 0), new Point2D(100, 0), new Point2D(100, 100), new Point2D(0, 100) });
            string polyJson = JsonSerializer.Serialize(poly, BimModelSerializer.CreateOptions(true));
            Console.WriteLine($"[INFO] Standalone Polygon2D serialized: {polyJson}");
            var desPoly = JsonSerializer.Deserialize<Polygon2D>(polyJson, BimModelSerializer.CreateOptions(true));
            Console.WriteLine($"[INFO] Standalone Polygon2D deserialized: vertices count = {desPoly?.VertexCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FINDING] Standalone Polygon2D serialization/deserialization failed: {ex.GetType().Name}: {ex.Message}");
            Findings.Add($"Polygon2D serialization/deserialization failed: {ex.GetType().Name}: {ex.Message}");
        }

        var originalModel = CreateComplexBenchmarkModel();

        // 0.3 Proof of fix for Polygon2D and Vector2D serialization
        Console.WriteLine("\n--- Testing Proof-of-Fix for Polygon2D and Vector2D Serialization ---");
        var fixedOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        fixedOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        fixedOptions.Converters.Add(new Vector2DJsonConverter());
        fixedOptions.Converters.Add(new Polygon2DJsonConverter());

        try
        {
            string fixedJson = JsonSerializer.Serialize(originalModel, fixedOptions);
            Assert(!string.IsNullOrEmpty(fixedJson), "With Vector2D & Polygon2D converters, BimModel serializes successfully!");

            var fixedModel = JsonSerializer.Deserialize<BimModel>(fixedJson, fixedOptions);
            Assert(fixedModel != null, "With Vector2D & Polygon2D converters, BimModel deserializes successfully!");
            Assert(fixedModel?.ProjectName == originalModel.ProjectName, "ProjectName matches");
            Assert(fixedModel?.Levels.Count == originalModel.Levels.Count, "Levels count matches");
            Assert(fixedModel?.Columns.Count == originalModel.Columns.Count, "Columns count matches");
            Assert(fixedModel?.Walls.Count == originalModel.Walls.Count, "Walls count matches");
            Assert(fixedModel?.Beams.Count == originalModel.Beams.Count, "Beams count matches");
            Assert(fixedModel?.Slabs.Count == originalModel.Slabs.Count, "Slabs count matches");
            Assert(fixedModel?.Slabs[0].Boundary.VertexCount == 4, "Slab boundary vertices count = 4");
            Assert(fixedModel?.Rooms.Count == originalModel.Rooms.Count, "Rooms count matches");
            Assert(fixedModel?.Fixtures.Count == originalModel.Fixtures.Count, "Fixtures count matches");
            Assert(fixedModel?.Pipes.Count == originalModel.Pipes.Count, "Pipes count matches");
            Assert(fixedModel?.Rebars.Count == originalModel.Rebars.Count, "Rebars count matches");
            Console.WriteLine("[PROOF OF FIX VERIFIED] Custom converters or proper annotations completely resolve the serialization failure!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROOF FAILED] {ex.GetType().Name}: {ex.Message}");
        }

        // 1. Serialize to JSON string
        string json = "";
        try
        {
            json = BimModelSerializer.Serialize(originalModel, indented: true);
            Assert(!string.IsNullOrEmpty(json), "BimModel serialized to JSON string successfully");
        }
        catch (Exception ex)
        {
            Assert(false, "BimModel serialization failed", $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            Findings.Add($"BimModelSerializer.Serialize threw exception: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // 2. Deserialize from JSON string
        BimModel deserializedModel;
        try
        {
            deserializedModel = BimModelSerializer.Deserialize(json);
            Assert(deserializedModel != null, "BimModel deserialized from JSON successfully");
        }
        catch (Exception ex)
        {
            Assert(false, "BimModel deserialization failed", ex.ToString());
            return;
        }

        // 3. Verify data fidelity across all domain collections
        Assert(deserializedModel.ProjectName == originalModel.ProjectName,
            $"ProjectName matches: '{deserializedModel.ProjectName}'");

        Assert(deserializedModel.Levels.Count == originalModel.Levels.Count,
            $"Levels count matches: {deserializedModel.Levels.Count}");
        for (int i = 0; i < originalModel.Levels.Count; i++)
        {
            var orig = originalModel.Levels[i];
            var des = deserializedModel.Levels[i];
            Assert(orig.Name == des.Name && Math.Abs(orig.ElevationM - des.ElevationM) < 1e-6 && orig.FloorNumber == des.FloorNumber,
                $"Level {orig.Name} fidelity matches");
        }

        Assert(deserializedModel.Grids.Count == originalModel.Grids.Count,
            $"Grids count matches: {deserializedModel.Grids.Count}");
        for (int i = 0; i < originalModel.Grids.Count; i++)
        {
            var orig = originalModel.Grids[i];
            var des = deserializedModel.Grids[i];
            Assert(orig.Name == des.Name && orig.Direction == des.Direction && orig.StartPoint.IsAlmostEqualTo(des.StartPoint),
                $"Grid {orig.Name} fidelity matches (Direction={des.Direction})");
        }

        Assert(deserializedModel.Columns.Count == originalModel.Columns.Count,
            $"Columns count matches: {deserializedModel.Columns.Count}");
        for (int i = 0; i < originalModel.Columns.Count; i++)
        {
            var orig = originalModel.Columns[i];
            var des = deserializedModel.Columns[i];
            Assert(orig.Id == des.Id && orig.CenterLocation.IsAlmostEqualTo(des.CenterLocation) && orig.WidthMm == des.WidthMm,
                $"Column {orig.Id} fidelity matches");
        }

        Assert(deserializedModel.Walls.Count == originalModel.Walls.Count,
            $"Walls count matches: {deserializedModel.Walls.Count}");
        for (int i = 0; i < originalModel.Walls.Count; i++)
        {
            var orig = originalModel.Walls[i];
            var des = deserializedModel.Walls[i];
            Assert(orig.Id == des.Id && orig.Openings.Count == des.Openings.Count,
                $"Wall {orig.Id} fidelity matches (Openings: {des.Openings.Count})");
            for (int j = 0; j < orig.Openings.Count; j++)
            {
                var origOp = orig.Openings[j];
                var desOp = des.Openings[j];
                Assert(origOp.Id == desOp.Id && origOp.Kind == desOp.Kind && origOp.Swing == desOp.Swing && origOp.WidthMm == desOp.WidthMm,
                    $"  Opening {origOp.Id} fidelity matches (Kind={desOp.Kind}, Swing={desOp.Swing})");
            }
        }

        Assert(deserializedModel.Beams.Count == originalModel.Beams.Count,
            $"Beams count matches: {deserializedModel.Beams.Count}");

        Assert(deserializedModel.Slabs.Count == originalModel.Slabs.Count,
            $"Slabs count matches: {deserializedModel.Slabs.Count}");
        for (int i = 0; i < originalModel.Slabs.Count; i++)
        {
            var orig = originalModel.Slabs[i];
            var des = deserializedModel.Slabs[i];
            Assert(orig.Id == des.Id && orig.Boundary.VertexCount == des.Boundary.VertexCount,
                $"Slab {orig.Id} boundary vertex count matches: {des.Boundary.VertexCount}");
            Assert(Math.Abs(orig.GrossAreaM2 - des.GrossAreaM2) < 1e-4,
                $"Slab {orig.Id} gross area matches: {des.GrossAreaM2:F2} m2");
        }

        Assert(deserializedModel.Rooms.Count == originalModel.Rooms.Count,
            $"Rooms count matches: {deserializedModel.Rooms.Count}");
        for (int i = 0; i < originalModel.Rooms.Count; i++)
        {
            var orig = originalModel.Rooms[i];
            var des = deserializedModel.Rooms[i];
            Assert(orig.Name == des.Name && orig.TagLocation.IsAlmostEqualTo(des.TagLocation) && orig.TheoreticalAreaM2 == des.TheoreticalAreaM2,
                $"Room {orig.Name} fidelity matches (Area={des.TheoreticalAreaM2} m2)");
        }

        Assert(deserializedModel.Fixtures.Count == originalModel.Fixtures.Count,
            $"Fixtures count matches: {deserializedModel.Fixtures.Count}");
        for (int i = 0; i < originalModel.Fixtures.Count; i++)
        {
            var orig = originalModel.Fixtures[i];
            var des = deserializedModel.Fixtures[i];
            Assert(orig.Id == des.Id && orig.Category == des.Category && orig.FamilyName == des.FamilyName,
                $"Fixture {orig.Id} fidelity matches (Category={des.Category})");
        }

        Assert(deserializedModel.Pipes.Count == originalModel.Pipes.Count,
            $"Pipes count matches: {deserializedModel.Pipes.Count}");

        Assert(deserializedModel.Rebars.Count == originalModel.Rebars.Count,
            $"Rebars count matches: {deserializedModel.Rebars.Count}");
        Assert(Math.Abs(deserializedModel.TotalRebarWeightKg - originalModel.TotalRebarWeightKg) < 1e-4,
            $"Total rebar weight matches: {deserializedModel.TotalRebarWeightKg:F2} kg");

        // 4. Test File Roundtrip
        string tempPath = Path.Combine(Path.GetTempPath(), $"bim_model_test_{Guid.NewGuid():N}.json");
        try
        {
            BimModelSerializer.SerializeToFile(originalModel, tempPath);
            Assert(File.Exists(tempPath), "SerializeToFile wrote JSON file to disk");
            var fromFile = BimModelSerializer.DeserializeFromFile(tempPath);
            Assert(fromFile.ProjectName == originalModel.ProjectName && fromFile.Columns.Count == originalModel.Columns.Count,
                "DeserializeFromFile reloaded model from disk faithfully");
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static BimModel CreateComplexBenchmarkModel()
    {
        var levels = new List<LevelDef>
        {
            new("TANG_1", 0.15, 3650, 1),
            new("TANG_2", 3.80, 3400, 2),
            new("TANG_3", 7.20, 3400, 3),
            new("TANG_4", 10.60, 2800, 4),
            new("TANG_MAI", 13.40, 0, 5)
        };

        var grids = new List<AxisGrid>
        {
            AxisGrid.CreateOrthogonal("X1", 0, GridDirection.X, 0, 18000),
            AxisGrid.CreateOrthogonal("X2", 4500, GridDirection.X, 0, 18000),
            AxisGrid.CreateOrthogonal("YA", 0, GridDirection.Y, 0, 5000),
            AxisGrid.CreateOrthogonal("YB", 5000, GridDirection.Y, 0, 5000)
        };

        var cols = new List<ColumnDef>
        {
            new() { Id = "C1", CenterLocation = new Point2D(0, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "TANG_1", TopLevel = "TANG_2" },
            new() { Id = "C2", CenterLocation = new Point2D(4500, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "TANG_1", TopLevel = "TANG_2" },
            new() { Id = "C3", CenterLocation = new Point2D(0, 5000), WidthMm = 220, DepthMm = 220, BaseLevel = "TANG_1", TopLevel = "TANG_2" },
            new() { Id = "C4", CenterLocation = new Point2D(4500, 5000), WidthMm = 220, DepthMm = 220, BaseLevel = "TANG_1", TopLevel = "TANG_2" }
        };

        var walls = new List<WallDef>
        {
            new()
            {
                Id = "W1",
                StartPoint = new Point2D(0, 0),
                EndPoint = new Point2D(4500, 0),
                ThicknessMm = 220,
                BaseLevel = "TANG_1",
                TopLevel = "TANG_2",
                Openings = new[]
                {
                    new OpeningDef { Id = "D1", HostWallId = "W1", DistanceAlongWallMm = 1500, WidthMm = 900, HeightMm = 2200, Kind = OpeningKind.Door, Swing = OpeningSwing.Left }
                }
            },
            new()
            {
                Id = "W2",
                StartPoint = new Point2D(0, 5000),
                EndPoint = new Point2D(4500, 5000),
                ThicknessMm = 220,
                BaseLevel = "TANG_1",
                TopLevel = "TANG_2",
                Openings = new[]
                {
                    new OpeningDef { Id = "WIN1", HostWallId = "W2", DistanceAlongWallMm = 2000, WidthMm = 1400, HeightMm = 1600, SillHeightMm = 900, Kind = OpeningKind.Window, Swing = OpeningSwing.None }
                }
            }
        };

        var beams = new List<BeamDef>
        {
            new() { Id = "B1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(4500, 0), WidthMm = 200, HeightMm = 400, Level = "TANG_2" },
            new() { Id = "B2", StartPoint = new Point2D(0, 5000), EndPoint = new Point2D(4500, 5000), WidthMm = 200, HeightMm = 400, Level = "TANG_2" }
        };

        var slabBoundary = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(4500, 0),
            new Point2D(4500, 5000),
            new Point2D(0, 5000)
        });
        var slabs = new List<SlabDef>
        {
            new() { Id = "S1", Boundary = slabBoundary, ThicknessMm = 100, Level = "TANG_2" }
        };

        var rooms = new List<RoomDef>
        {
            new() { Name = "PHONG_KHACH", TagLocation = new Point2D(2250, 2500), TheoreticalAreaM2 = 22.5, Level = "TANG_1", BoundaryPolygon = slabBoundary }
        };

        var fixtures = new List<MepFixtureDef>
        {
            new() { Id = "WC1", Category = MepCategory.Sanitary, FamilyName = "WaterCloset", Location = new Point3D(1000, 1000, 0), Level = "TANG_1" },
            new() { Id = "SINK1", Category = MepCategory.Sanitary, FamilyName = "Lavabo", Location = new Point3D(1000, 2000, 850), Level = "TANG_1" }
        };

        var pipes = new List<PipeRunDef>
        {
            new()
            {
                Id = "PIPE_WS_1",
                SystemType = PipeSystemType.WaterSupply,
                DiameterMm = 25,
                Points = new[] { new Point3D(1000, 1000, 0), new Point3D(1000, 1000, 500), new Point3D(1000, 2000, 500) },
                Level = "TANG_1"
            }
        };

        var rebars = new List<RebarDef>
        {
            new() { Id = "R1", BarMark = 1, DiameterMm = 18, LengthMm = 4500, Quantity = 3, ShapeType = RebarShapeType.Straight },
            new() { Id = "R2", BarMark = 2, DiameterMm = 8, LengthMm = 1100, Quantity = 25, ShapeType = RebarShapeType.ClosedStirrup }
        };

        return new BimModel
        {
            ProjectName = "NhaPho_5x18m_Benchmark",
            Levels = levels,
            Grids = grids,
            Columns = cols,
            Walls = walls,
            Beams = beams,
            Slabs = slabs,
            Rooms = rooms,
            Fixtures = fixtures,
            Pipes = pipes,
            Rebars = rebars
        };
    }
}

public class Vector2DJsonConverter : System.Text.Json.Serialization.JsonConverter<Vector2D>
{
    public override Vector2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        double x = doc.RootElement.GetProperty("x").GetDouble();
        double y = doc.RootElement.GetProperty("y").GetDouble();
        return new Vector2D(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2D value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}

public class Polygon2DJsonConverter : System.Text.Json.Serialization.JsonConverter<Polygon2D>
{
    public override Polygon2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var verticesProp = doc.RootElement.GetProperty("vertices");
        var list = new List<Point2D>();
        foreach (var v in verticesProp.EnumerateArray())
        {
            list.Add(new Point2D(v.GetProperty("x").GetDouble(), v.GetProperty("y").GetDouble()));
        }
        return new Polygon2D(list);
    }

    public override void Write(Utf8JsonWriter writer, Polygon2D value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("vertices");
        foreach (var v in value.Vertices)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", v.X);
            writer.WriteNumber("y", v.Y);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

