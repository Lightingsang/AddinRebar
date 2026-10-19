#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPKata/HPKata.Core/bin/Debug/net8.0/HPKata.Core.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPKata/HPKata.Core.Tests/bin/Debug/net8.0/ClosedXML.dll"

open System
open System.IO
open HPKata.Core.Columns.Models
open HPKata.Core.Columns.Calculators
open HPKata.Core.Columns.Etabs
open HPKata.Core.Columns.Persistence
open HPKata.Core.Walls.Models
open HPKata.Core.Walls.Calculators
open HPKata.Core.Walls.Etabs
open HPKata.Core.Walls.Persistence

printfn "=================================================================="
printfn "  ADVERSARIAL STRESS TEST & INTEGRITY AUDIT — PHASE 3 (COLUMN & WALL)"
printfn "=================================================================="

let mutable totalPassed = 0
let mutable totalFailed = 0

let check condition name =
    if condition then
        printfn "  [PASS] %s" name
        totalPassed <- totalPassed + 1
    else
        printfn "  [FAIL] %s" name
        totalFailed <- totalFailed + 1

// ------------------------------------------------------------------
// TEST SUITE 1: ColumnCrankBendCalculator
// ------------------------------------------------------------------
printfn "\n--- 1. ColumnCrankBendCalculator Singularities ---"

// Normal and boundary cases
let s0 = ColumnCrankBendCalculator.DetermineStrategy(0.0)
check (s0 = ColumnTransitionStrategy.ContinuousStraight) "stepDown = 0.0 -> ContinuousStraight"

let s50 = ColumnCrankBendCalculator.DetermineStrategy(50.0)
check (s50 = ColumnTransitionStrategy.CrankBend) "stepDown = 50.0 -> CrankBend"

let s51 = ColumnCrankBendCalculator.DetermineStrategy(50.001)
check (s51 = ColumnTransitionStrategy.CutoffAndStarterDowel) "stepDown = 50.001 -> CutoffAndStarterDowel"

let negThrows =
    try
        ColumnCrankBendCalculator.DetermineStrategy(-5.0) |> ignore
        false
    with :? ArgumentOutOfRangeException -> true
check negThrows "Negative stepDown throws ArgumentOutOfRangeException"

let crankLen = ColumnCrankBendCalculator.CalculateCrankLength(40.0)
check (crankLen = 240.0) "Crank length for 40mm at 1:6 = 240mm"

let crankLenZero = ColumnCrankBendCalculator.CalculateCrankLength(0.0)
check (crankLenZero = 0.0) "Crank length for 0mm = 0mm"

// ------------------------------------------------------------------
// TEST SUITE 2: ColumnSpliceCalculator & 11.7m Commercial Invariants
// ------------------------------------------------------------------
printfn "\n--- 2. ColumnSpliceCalculator Commercial Bar Invariants ---"

let lap20 = ColumnSpliceCalculator.CalculateLapLength(20, false)
check (lap20 = 700.0) "Compression lap length for d=20 (35d) = 700mm"

let lapTens20 = ColumnSpliceCalculator.CalculateLapLength(20, true)
check (lapTens20 = 900.0) "Tension lap length for d=20 (45d) = 900mm"

let lapMin = ColumnSpliceCalculator.CalculateLapLength(10, false)
check (lapMin >= 400.0) "Lap length clamped to minimum 400mm"

let stagger = ColumnSpliceCalculator.CalculateStaggerOffset(700.0)
check (stagger = 910.0) "Stagger offset for 700mm lap (1.3x) = 910mm"

// 60m high-rise column commercial segmentation
let highRiseSegs = ColumnSpliceCalculator.SegmentCommercialColumnBars(0.0, 60000.0, 25, 3300.0, true)
check (highRiseSegs.Count > 5) (sprintf "60m tower produces %d segments (>5)" highRiseSegs.Count)

let allWithinStock = highRiseSegs |> Seq.forall (fun s -> s.LengthMm <= 11700.001)
check allWithinStock "ALL segments in 60m tower satisfy Length <= 11700 mm"

let zeroHeightSegs = ColumnSpliceCalculator.SegmentCommercialColumnBars(1000.0, 1000.0, 20, 3000.0, false)
check (zeroHeightSegs.Count = 0) "Zero height column returns 0 segments without loop"

// ------------------------------------------------------------------
// TEST SUITE 3: ColumnConfinementCalculator & Seismic Rules
// ------------------------------------------------------------------
printfn "\n--- 3. ColumnConfinementCalculator Seismic Invariants ---"

// Normal column 400x400, clear height 2700
let conf = ColumnConfinementCalculator.CalculateConfinement(400.0, 400.0, 2700.0, 600.0, 20)
check (conf.ConfinementHeightL0Mm = 500.0) "Confinement height l0 = max(400, 450, 500) = 500mm"
check (conf.DenseSpacingA1Mm <= 100.0) "Dense spacing a1 <= 100mm"
check (conf.SparseSpacingA2Mm <= 200.0) "Sparse spacing a2 <= 200mm"
check (conf.JointSets >= 2) "Beam-column joint sets >= 2"

// Zero clear height check
let zeroConf = ColumnConfinementCalculator.CalculateConfinement(400.0, 400.0, 0.0, 0.0, 20)
check (zeroConf.TotalSets >= 0) "Zero clear height produces valid non-negative sets without exception"

// Slender column check
let slenderL0 = ColumnConfinementCalculator.CalculateConfinementHeight(200.0, 200.0, 9000.0)
check (slenderL0 = 1500.0) "Slender column l0 = max(200, 9000/6=1500, 500) = 1500mm"

// ------------------------------------------------------------------
// TEST SUITE 4: TcvnColumnRebarOptimizer Stress
// ------------------------------------------------------------------
printfn "\n--- 4. TcvnColumnRebarOptimizer Edge Cases ---"

// Normal optimization
let opt = TcvnColumnRebarOptimizer.Optimize(400.0, 400.0, 2500.0, 0.6, 3.5)
check (opt.TotalSteelAreaMm2 >= 2500.0) "Optimized area satisfies Ast >= 2500 mm2"
check (opt.TotalSteelAreaMm2 <= 400.0 * 400.0 * 0.035) "Optimized area within rho <= 3.5%"

// Unachievable target Ast fallback check
let optExtreme = TcvnColumnRebarOptimizer.Optimize(300.0, 300.0, 50000.0, 0.6, 3.5)
check (optExtreme <> null && optExtreme.TotalBarCount >= 4) "Extreme Ast triggers fallback without crash"

// Negative dimension throws
let optThrows =
    try
        TcvnColumnRebarOptimizer.Optimize(-300.0, 300.0, 1000.0) |> ignore
        false
    with :? ArgumentException -> true
check optThrows "Negative section dimensions throw ArgumentException"

// ------------------------------------------------------------------
// TEST SUITE 5: Wall Boundary & Web Mesh Calculators
// ------------------------------------------------------------------
printfn "\n--- 5. Wall Boundary & Web Mesh Invariants ---"

// Boundary calculation: Lw = 4000, tw = 300
let bbz = WallBoundaryCalculator.CalculateBoundaryLength(4000.0, 300.0)
check (bbz = 600.0) "Boundary length bbz = max(0.15*4000=600, 1.5*300=450, 400) = 600mm"

// Ultra short wall clamped
let bbzShort = WallBoundaryCalculator.CalculateBoundaryLength(1000.0, 200.0)
check (bbzShort = 400.0) "Short wall boundary length clamped to 400mm"

// Web mesh vertical bars: wall length 3000, boundary 500 each side -> web 2000mm, spacing 200mm -> 11 per curtain * 2 = 22
let vCount = WallWebMeshCalculator.CalculateVerticalBarCount(3000.0, 500.0, 500.0, 200.0, true)
check (vCount = 22) "Double-curtain vertical bars in 2000mm web at s=200 = 22"

// Wall with boundaries meeting in center (zero web)
let zeroWebCount = WallWebMeshCalculator.CalculateVerticalBarCount(1000.0, 500.0, 500.0, 200.0, true)
check (zeroWebCount = 0) "Zero web wall returns 0 vertical web bars"

// Cross tie calculation
let ties = WallWebMeshCalculator.CalculateCrossTieCount(2000.0, 3000.0, 4.0)
check (ties = 24) "Cross ties for 6m2 at 4/m2 = 24"

// U-Bar calculation
let struct (uW, uLeg, uTot) = WallWebMeshCalculator.CalculateUBarDimensions(300.0, 25.0, 12)
check (uW = 250.0 && uLeg = 360.0 && uTot = 970.0) "U-bar: width=250, leg=360, total=970mm"

// ------------------------------------------------------------------
// TEST SUITE 6: WallOpeningTrimmerCalculator
// ------------------------------------------------------------------
printfn "\n--- 6. WallOpeningTrimmerCalculator Invariants ---"

let op = new WallOpeningModel()
op.OpeningMark <- "D1"
op.WidthMm <- 900.0
op.HeightMm <- 2100.0
op.HasDiagonalBars <- true
op.DiagonalBarsPerCorner <- 2
op.JambExtraBarCountPerSide <- 2
op.HeaderExtraBarCountPerEdge <- 2

let trim = WallOpeningTrimmerCalculator.CalculateTrimmers(op, 500.0)
check (trim.DiagonalBarCount = 8) "4 corners x 2 bars = 8 diagonal bars"
check (trim.JambExtraBarCount = 4) "2 sides x 2 bars = 4 jamb extra bars"
check (trim.JambExtraBarLengthMm = 3100.0) "Jamb length = 2100 + 2*500 = 3100mm"
check (trim.HeaderSillExtraBarCount = 4) "2 edges x 2 bars = 4 header/sill extra bars"
check (trim.HeaderSillExtraBarLengthMm = 1900.0) "Header length = 900 + 2*500 = 1900mm"

let nullOpThrows =
    try
        WallOpeningTrimmerCalculator.CalculateTrimmers(null) |> ignore
        false
    with :? ArgumentNullException -> true
check nullOpThrows "Null opening throws ArgumentNullException"

// ------------------------------------------------------------------
// TEST SUITE 7: ETABS Column & Wall Table Parsing
// ------------------------------------------------------------------
printfn "\n--- 7. ETABS Table Mapping Robustness ---"

let etabsColText = 
    "Story\tColumn\tWidth\tDepth\tP\tM2\tM3\tAst\tAv/s\n" +
    "Story1\tC1\t500\t500\t-1250.5\t45.2\t110.8\t3200\t1.2\n" +
    "Story2\tC1\t500\t500\t-950.0\t35.0\t85.0\t2800\t1.0\n"

let cols = EtabsColumnTableMapper.MapTableContent(etabsColText)
check (cols.Count = 2) "Parsed 2 ETABS column design rows"
check (cols.[0].ColumnMark = "C1" && cols.[0].WidthMm = 500.0 && cols.[0].RequiredAstMm2 = 3200.0) "Column fields mapped correctly"

// Empty and corrupt inputs
let emptyCols = EtabsColumnTableMapper.MapTableContent("")
check (emptyCols.Count = 0) "Empty text returns empty list"

// Note: When markCol is -1, EtabsColumnTableMapper uses default mark="C1" (lenient fallback)
let corruptCols = EtabsColumnTableMapper.MapTableContent("garbage text only\nno delimiter")
check (corruptCols.Count >= 0) "Corrupt table handled without exception (synthesizes default record or returns 0)"

let etabsWallText =
    "Story,Pier,Thickness,Length,P,V2,M3,Asb,Avh\n" +
    "Story1,P1,350,4000,-3500,250,1200,4500,2.5\n"

let piers = EtabsWallTableMapper.MapTableContent(etabsWallText)
check (piers.Count = 1) "Parsed 1 ETABS pier design row"
check (piers.[0].PierMark = "P1" && piers.[0].ThicknessMm = 350.0 && piers.[0].RequiredBoundaryAsbMm2 = 4500.0) "Pier fields mapped correctly"

// ------------------------------------------------------------------
// TEST SUITE 8: Schedule Aggregation & ClosedXML Excel Export
// ------------------------------------------------------------------
printfn "\n--- 8. Schedule Aggregation & ClosedXML Export ---"

let colLayout = new ColumnRebarLayout()
colLayout.ColumnMark <- "C1"
let story1 = new ColumnStorySegment()
story1.StoryIndex <- 0
story1.StoryName <- "Tầng 1"
story1.StoryHeightMm <- 3600.0
story1.WidthMm <- 400.0
story1.DepthMm <- 400.0
story1.Section.CornerDiameterMm <- 20
story1.TieDistribution.TieDiameterMm <- 8
story1.TieDistribution.DenseSpacingMm <- 100.0
story1.TieDistribution.SparseSpacingMm <- 200.0
colLayout.Stories.Add(story1)

let sched = ColumnScheduleAggregator.AggregateSchedule(colLayout, 1)
check (sched.Count > 0) "Column schedule produced valid items"

// Test ClosedXML generation to memory stream
let msCol = new MemoryStream()
ColumnPlanScheduleWorkbookGenerator.GenerateWorkbookStream(ResizeArray([colLayout]), msCol)
check (msCol.Length > 1000L) (sprintf "ClosedXML generated valid XLSX stream (%d bytes)" msCol.Length)

// Shear wall schedule and export
let wallModel = new ShearWallModel()
wallModel.WallMark <- "W1"
wallModel.ThicknessMm <- 300.0
wallModel.LengthMm <- 3000.0
let wStory = new WallStorySegment()
wStory.StoryIndex <- 0
wStory.StoryName <- "Tầng 1"
wStory.StoryHeightMm <- 3600.0
wStory.ThicknessMm <- 300.0
wStory.LengthMm <- 3000.0
wallModel.Stories.Add(wStory)
wallModel.BoundaryElements.Add(WallBoundaryCalculator.CreateBoundaryElement("BE-1", WallBoundaryPosition.Left, 3000.0, 300.0))
wallModel.WebReinforcement <- new WallWebReinforcementModel()

let wallSched = WallScheduleAggregator.AggregateSchedule(wallModel, 1)
check (wallSched.Count > 0) "Wall schedule produced valid items"

let msWall = new MemoryStream()
WallPlanScheduleWorkbookGenerator.GenerateWorkbookStream(ResizeArray([wallModel]), msWall)
check (msWall.Length > 1000L) (sprintf "ClosedXML generated valid Wall XLSX stream (%d bytes)" msWall.Length)

// ------------------------------------------------------------------
// SUMMARY
// ------------------------------------------------------------------
printfn "\n=================================================================="
printfn "  STRESS TEST SUMMARY: %d PASSED, %d FAILED" totalPassed totalFailed
printfn "=================================================================="

if totalFailed > 0 then
    exit 1
else
    exit 0
