using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Algorithms;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Tcvn;

public class Program
{
    private static int _passed = 0;
    private static int _failed = 0;
    private static readonly List<string> _failures = new();

    public static int Main()
    {
        Console.WriteLine("=== EMPIRICAL CHALLENGER ADVERSARIAL TEST SUITE ===");
        Console.WriteLine($"Start time (UTC): {DateTime.UtcNow:O}\n");

        RunPoint2DTests();
        RunPoint3DTests();
        RunVector2DTests();
        RunBoundingBox2DTests();
        RunPolygon2DTests();
        RunLiangBarskyTests();
        RunSpatialAlgorithmsTests();
        RunTcvnMathTests();
        RunSpatialGridIndexTests();

        // Adversarial stress test suites
        RunAdversarialLiangBarskyStressTests();
        RunAdversarialPolygonStressTests();
        RunAdversarialVectorStressTests();
        RunAdversarialBoundingBoxStressTests();

        Console.WriteLine("\n========================================================");
        Console.WriteLine($"RESULTS SUMMARY: Passed: {_passed}, Failed: {_failed}");
        if (_failed > 0)
        {
            Console.WriteLine("\nFAILURES DETECTED:");
            foreach (var f in _failures)
            {
                Console.WriteLine($"  [FAIL] {f}");
            }
            Console.WriteLine("========================================================");
            return 1;
        }

        Console.WriteLine("ALL ADVERSARIAL STRESS TESTS PASSED CLEANLY.");
        Console.WriteLine("========================================================");
        return 0;
    }

    private static void Assert(bool condition, string testName, string message = "")
    {
        if (condition)
        {
            _passed++;
        }
        else
        {
            _failed++;
            string err = $"{testName}: {message}";
            _failures.Add(err);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {err}");
            Console.ResetColor();
        }
    }

    private static void AssertThrows<TException>(Action action, string testName) where TException : Exception
    {
        try
        {
            action();
            _failed++;
            string err = $"{testName}: Expected exception {typeof(TException).Name} was NOT thrown.";
            _failures.Add(err);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {err}");
            Console.ResetColor();
        }
        catch (TException)
        {
            _passed++;
        }
        catch (Exception ex)
        {
            _failed++;
            string err = $"{testName}: Expected exception {typeof(TException).Name}, but got {ex.GetType().Name}: {ex.Message}";
            _failures.Add(err);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {err}");
            Console.ResetColor();
        }
    }

    private static bool Near(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;

    // =========================================================================
    // 1. Point2D & Point3D
    // =========================================================================
    private static void RunPoint2DTests()
    {
        Console.WriteLine("--- Running Point2D Tests ---");
        var p1 = new Point2D(3.0, 4.0);
        var p2 = new Point2D(0.0, 0.0);

        Assert(Near(p1.DistanceTo(p2), 5.0), "Point2D.DistanceTo", "3-4-5 Euclidean distance");
        Assert(Near(p1.DistanceSquaredTo(p2), 25.0), "Point2D.DistanceSquaredTo", "Distance squared");

        var pTrans = p1.Translate(2.0, -1.0);
        Assert(Near(pTrans.X, 5.0) && Near(pTrans.Y, 3.0), "Point2D.Translate(dx, dy)", "Translation");

        var v = new Vector2D(-1.0, 2.0);
        var pTransV = p1.Translate(v);
        Assert(Near(pTransV.X, 2.0) && Near(pTransV.Y, 6.0), "Point2D.Translate(Vector2D)", "Vector translation");

        var p3D = p1.ToPoint3D(10.5);
        Assert(Near(p3D.X, 3.0) && Near(p3D.Y, 4.0) && Near(p3D.Z, 10.5), "Point2D.ToPoint3D", "Projection to 3D");

        Assert(p1.IsAlmostEqualTo(new Point2D(3.0000001, 3.9999999), 1e-5), "Point2D.IsAlmostEqualTo within tolerance");
        Assert(!p1.IsAlmostEqualTo(new Point2D(3.01, 4.0), 1e-3), "Point2D.IsAlmostEqualTo outside tolerance");

        var pAdd = p1 + v;
        Assert(Near(pAdd.X, 2.0) && Near(pAdd.Y, 6.0), "Point2D operator +");
        var pSub = p1 - v;
        Assert(Near(pSub.X, 4.0) && Near(pSub.Y, 2.0), "Point2D operator - Vector");
        var vDiff = p1 - p2;
        Assert(Near(vDiff.X, 3.0) && Near(vDiff.Y, 4.0), "Point2D operator - Point");

        Assert(p1 == new Point2D(3.0, 4.0), "Point2D operator ==");
        Assert(p1 != new Point2D(3.0, 4.0001), "Point2D operator !=");
    }

    private static void RunPoint3DTests()
    {
        Console.WriteLine("--- Running Point3D Tests ---");
        var p1 = new Point3D(1.0, 2.0, 2.0);
        var p0 = Point3D.Zero;

        Assert(Near(p1.DistanceTo(p0), 3.0), "Point3D.DistanceTo", "1-2-2 distance is 3");
        Assert(Near(p1.DistanceSquaredTo(p0), 9.0), "Point3D.DistanceSquaredTo");
        Assert(Near(p1.Distance2DTo(p0), Math.Sqrt(5.0)), "Point3D.Distance2DTo", "XY distance");

        var p2D = p1.ToPoint2D();
        Assert(Near(p2D.X, 1.0) && Near(p2D.Y, 2.0), "Point3D.ToPoint2D");

        var pZ = p1.WithZ(50.0);
        Assert(Near(pZ.Z, 50.0) && Near(pZ.X, 1.0), "Point3D.WithZ");

        Assert(p1.IsAlmostEqualTo(new Point3D(1.0000001, 1.9999999, 2.0000001), 1e-5), "Point3D.IsAlmostEqualTo");
    }

    // =========================================================================
    // 2. Vector2D
    // =========================================================================
    private static void RunVector2DTests()
    {
        Console.WriteLine("--- Running Vector2D Tests ---");

        var vx = Vector2D.UnitX;
        var leftOfX = vx.LeftNormal;
        Assert(Near(leftOfX.X, 0.0) && Near(leftOfX.Y, 1.0), "Vector2D.LeftNormal of UnitX is UnitY");
        Assert(Near(vx.Dot(leftOfX), 0.0), "LeftNormal is perpendicular (Dot == 0)");
        Assert(vx.Cross(leftOfX) > 0.0, "LeftNormal has positive Cross product (CCW chirality)");

        var vy = Vector2D.UnitY;
        var leftOfY = vy.LeftNormal;
        Assert(Near(leftOfY.X, -1.0) && Near(leftOfY.Y, 0.0), "Vector2D.LeftNormal of UnitY is (-1, 0)");

        var rightOfX = vx.RightNormal;
        Assert(Near(rightOfX.X, 0.0) && Near(rightOfX.Y, -1.0), "Vector2D.RightNormal of UnitX is (0, -1)");
        Assert(Near(vx.Dot(rightOfX), 0.0), "RightNormal is perpendicular (Dot == 0)");
        Assert(vx.Cross(rightOfX) < 0.0, "RightNormal has negative Cross product (CW chirality)");

        var diag = new Vector2D(3.0, 4.0);
        var leftDiag = diag.LeftNormal;
        Assert(Near(leftDiag.X, -4.0) && Near(leftDiag.Y, 3.0), "LeftNormal of (3,4) is (-4,3)");
        Assert(Near(diag.Dot(leftDiag), 0.0), "LeftNormal of (3,4) is orthogonal");

        var vZero = Vector2D.Zero;
        Assert(vZero.LeftNormal == Vector2D.Zero, "LeftNormal of Zero is Zero");

        AssertThrows<InvalidOperationException>(() => Vector2D.Zero.Normalize(), "Vector2D.Zero.Normalize throws InvalidOperationException");

        var vNearZero = new Vector2D(1e-10, 1e-10);
        AssertThrows<InvalidOperationException>(() => vNearZero.Normalize(), "Near-zero vector Normalize throws InvalidOperationException");

        bool resZero = Vector2D.Zero.TryNormalize(out var uZero);
        Assert(!resZero && uZero == Vector2D.Zero, "Vector2D.Zero.TryNormalize returns false and outputs Zero");

        bool resNearZero = vNearZero.TryNormalize(out var uNearZero);
        Assert(!resNearZero && uNearZero == Vector2D.Zero, "vNearZero.TryNormalize returns false and outputs Zero");

        var vNorm = diag.Normalize();
        Assert(Near(vNorm.Length, 1.0), "Normalize produces unit length");
        Assert(Near(vNorm.X, 0.6) && Near(vNorm.Y, 0.8), "Normalize values (0.6, 0.8)");

        bool tryNorm = diag.TryNormalize(out var uDiag);
        Assert(tryNorm && Near(uDiag.Length, 1.0), "TryNormalize succeeds for non-zero vector");

        Assert(Near(vx.AngleRad, 0.0), "AngleRad of UnitX is 0");
        Assert(Near(vy.AngleRad, Math.PI / 2.0), "AngleRad of UnitY is PI/2");
        Assert(Near(vy.AngleDeg, 90.0), "AngleDeg of UnitY is 90 deg");
        Assert(Near(vx.AngleBetween(vy), Math.PI / 2.0), "AngleBetween UnitX and UnitY is +PI/2");
        Assert(Near(vy.AngleBetween(vx), -Math.PI / 2.0), "AngleBetween UnitY and UnitX is -PI/2");

        AssertThrows<DivideByZeroException>(() => { var _ = diag / 0.0; }, "Vector2D division by scalar 0 throws DivideByZeroException");

        Assert(diag.IsAlmostEqualTo(new Vector2D(3.0000001, 3.9999999), 1e-5), "Vector2D.IsAlmostEqualTo within tolerance");
        Assert(!diag.IsAlmostEqualTo(new Vector2D(3.01, 4.0), 1e-3), "Vector2D.IsAlmostEqualTo outside tolerance");
    }

    // =========================================================================
    // 3. BoundingBox2D
    // =========================================================================
    private static void RunBoundingBox2DTests()
    {
        Console.WriteLine("--- Running BoundingBox2D Tests ---");

        var box1 = new BoundingBox2D(0.0, 0.0, 10.0, 10.0);
        Assert(box1.Width == 10.0 && box1.Height == 10.0 && box1.Area == 100.0, "BoundingBox2D dimensions and area");
        Assert(box1.Center.IsAlmostEqualTo(new Point2D(5.0, 5.0)), "BoundingBox2D center");

        var boxReversed = new BoundingBox2D(new Point2D(10.0, 10.0), new Point2D(0.0, 0.0));
        Assert(boxReversed.Min.Equals(new Point2D(0.0, 0.0)) && boxReversed.Max.Equals(new Point2D(10.0, 10.0)), "BoundingBox2D canonicalizes inverted coordinates");

        Assert(box1.Contains(new Point2D(5.0, 5.0)), "Box contains interior point");
        Assert(box1.Contains(new Point2D(0.0, 0.0)), "Box contains min corner");
        Assert(box1.Contains(new Point2D(10.0, 10.0)), "Box contains max corner");
        Assert(box1.Contains(new Point2D(5.0, 0.0)), "Box contains edge point");
        Assert(!box1.Contains(new Point2D(10.0001, 5.0)), "Box does not contain point outside X");
        Assert(!box1.Contains(new Point2D(5.0, -0.0001)), "Box does not contain point outside Y");

        var subBox = new BoundingBox2D(2.0, 2.0, 8.0, 8.0);
        Assert(box1.Contains(subBox), "Box contains interior sub-box");
        Assert(box1.Contains(box1), "Box contains itself");
        Assert(!subBox.Contains(box1), "Sub-box does not contain larger box");

        var overlapBox = new BoundingBox2D(5.0, 5.0, 15.0, 15.0);
        Assert(box1.Intersects(overlapBox), "Box intersects overlapping box");
        Assert(overlapBox.Intersects(box1), "Intersects is symmetric");

        var disjointBox = new BoundingBox2D(20.0, 20.0, 30.0, 30.0);
        Assert(!box1.Intersects(disjointBox), "Box does not intersect disjoint box");

        var touchEdgeBox = new BoundingBox2D(10.0, 0.0, 20.0, 10.0);
        Assert(box1.Intersects(touchEdgeBox), "Box intersects touching edge box");

        var touchCornerBox = new BoundingBox2D(10.0, 10.0, 20.0, 20.0);
        Assert(box1.Intersects(touchCornerBox), "Box intersects touching corner box");

        var pointBox = new BoundingBox2D(5.0, 5.0, 5.0, 5.0);
        Assert(pointBox.Width == 0.0 && pointBox.Height == 0.0 && pointBox.Area == 0.0, "Point box has zero area");
        Assert(pointBox.IsValid, "Point box is valid");
        Assert(pointBox.Contains(new Point2D(5.0, 5.0)), "Point box contains its single point");
        Assert(!pointBox.Contains(new Point2D(5.0001, 5.0)), "Point box does not contain outside point");
        Assert(box1.Contains(pointBox), "Normal box contains interior point box");
        Assert(box1.Intersects(pointBox), "Normal box intersects interior point box");

        var lineBox = new BoundingBox2D(0.0, 5.0, 10.0, 5.0);
        Assert(lineBox.Width == 10.0 && lineBox.Height == 0.0 && lineBox.Area == 0.0, "Line box has zero height");
        Assert(box1.Contains(lineBox), "Box contains line box on horizontal axis");

        var pts = new[] { new Point2D(1, 2), new Point2D(5, -1), new Point2D(-3, 4) };
        var fromPts = BoundingBox2D.FromPoints(pts);
        Assert(fromPts.MinX == -3.0 && fromPts.MinY == -1.0 && fromPts.MaxX == 5.0 && fromPts.MaxY == 4.0, "FromPoints bounding box bounds");

        AssertThrows<ArgumentNullException>(() => BoundingBox2D.FromPoints(null!), "FromPoints throws ArgumentNullException for null");
        AssertThrows<ArgumentException>(() => BoundingBox2D.FromPoints(Array.Empty<Point2D>()), "FromPoints throws ArgumentException for empty sequence");

        var singlePtBox = BoundingBox2D.FromPoints(new[] { new Point2D(7.0, 8.0) });
        Assert(singlePtBox.MinX == 7.0 && singlePtBox.MaxX == 7.0 && singlePtBox.MinY == 8.0 && singlePtBox.MaxY == 8.0, "FromPoints single point box");

        var union = box1.Union(overlapBox);
        Assert(union.MinX == 0.0 && union.MinY == 0.0 && union.MaxX == 15.0 && union.MaxY == 15.0, "BoundingBox2D Union");

        var inter = box1.Intersection(overlapBox);
        Assert(inter.HasValue && inter.Value.MinX == 5.0 && inter.Value.MinY == 5.0 && inter.Value.MaxX == 10.0 && inter.Value.MaxY == 10.0, "BoundingBox2D Intersection");

        var nullInter = box1.Intersection(disjointBox);
        Assert(!nullInter.HasValue, "Intersection returns null for disjoint boxes");

        var inflated = box1.Inflate(2.0, 3.0);
        Assert(inflated.MinX == -2.0 && inflated.MinY == -3.0 && inflated.MaxX == 12.0 && inflated.MaxY == 13.0, "BoundingBox2D Inflate");

        var deflated = box1.Deflate(0.2);
        Assert(Near(deflated.MinX, 1.0) && Near(deflated.MaxX, 9.0) && Near(deflated.MinY, 1.0) && Near(deflated.MaxY, 9.0), "BoundingBox2D Deflate");
    }

    // =========================================================================
    // 4. Polygon2D
    // =========================================================================
    private static void RunPolygon2DTests()
    {
        Console.WriteLine("--- Running Polygon2D Tests ---");

        var emptyPoly = Polygon2D.Empty;
        Assert(emptyPoly.VertexCount == 0, "Polygon2D.Empty vertex count is 0");
        Assert(emptyPoly.Area == 0.0, "Polygon2D.Empty Area is 0");
        Assert(emptyPoly.SignedArea == 0.0, "Polygon2D.Empty SignedArea is 0");
        Assert(emptyPoly.Perimeter == 0.0, "Polygon2D.Empty Perimeter is 0");
        Assert(emptyPoly.Centroid == Point2D.Zero, "Polygon2D.Empty Centroid is Point2D.Zero");
        Assert(!emptyPoly.Contains(new Point2D(0, 0)), "Polygon2D.Empty Contains is false");

        AssertThrows<ArgumentNullException>(() => new Polygon2D(null!), "Polygon2D throws ArgumentNullException for null");
        AssertThrows<ArgumentException>(() => new Polygon2D(new[] { new Point2D(0, 0) }), "Polygon2D throws for 1 vertex");
        AssertThrows<ArgumentException>(() => new Polygon2D(new[] { new Point2D(0, 0), new Point2D(1, 1) }), "Polygon2D throws for 2 vertices");

        var closedInput = new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(0, 10), new Point2D(0, 0) };
        var polyStripped = new Polygon2D(closedInput);
        Assert(polyStripped.VertexCount == 3, "Polygon2D strips duplicated closing vertex");

        var invalidClosed = new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(0, 0) };
        AssertThrows<ArgumentException>(() => new Polygon2D(invalidClosed), "Polygon2D throws if stripping closure leaves < 3 vertices");

        var ccwSquare = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(1, 0),
            new Point2D(1, 1),
            new Point2D(0, 1)
        });
        Assert(Near(ccwSquare.Area, 1.0), "CCW Unit Square Area == 1.0");
        Assert(Near(ccwSquare.SignedArea, 1.0), "CCW Unit Square SignedArea == 1.0");
        Assert(ccwSquare.IsCounterClockwise, "CCW Unit Square IsCounterClockwise is true");
        Assert(Near(ccwSquare.Perimeter, 4.0), "CCW Unit Square Perimeter == 4.0");
        Assert(ccwSquare.Centroid.IsAlmostEqualTo(new Point2D(0.5, 0.5)), "CCW Unit Square Centroid == (0.5, 0.5)");

        var cwSquare = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(0, 1),
            new Point2D(1, 1),
            new Point2D(1, 0)
        });
        Assert(Near(cwSquare.Area, 1.0), "CW Unit Square Area == 1.0");
        Assert(Near(cwSquare.SignedArea, -1.0), "CW Unit Square SignedArea == -1.0");
        Assert(!cwSquare.IsCounterClockwise, "CW Unit Square IsCounterClockwise is false");
        Assert(cwSquare.Centroid.IsAlmostEqualTo(new Point2D(0.5, 0.5)), "CW Unit Square Centroid == (0.5, 0.5)");

        var tri = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(6, 0),
            new Point2D(0, 4)
        });
        Assert(Near(tri.Area, 12.0), "Triangle Area == 12.0");
        Assert(tri.Centroid.IsAlmostEqualTo(new Point2D(2.0, 4.0 / 3.0)), "Triangle Centroid == (2, 4/3)");

        var triPermuted = new Polygon2D(new[]
        {
            new Point2D(6, 0),
            new Point2D(0, 4),
            new Point2D(0, 0)
        });
        Assert(Near(triPermuted.Area, tri.Area), "Cyclic permutation Area invariance");
        Assert(triPermuted.Centroid.IsAlmostEqualTo(tri.Centroid), "Cyclic permutation Centroid invariance");

        var collinearPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0),
            new Point2D(10, 0)
        });
        Assert(Near(collinearPoly.Area, 0.0), "Collinear polygon has 0 area");
        Assert(Near(collinearPoly.SignedArea, 0.0), "Collinear polygon has 0 signed area");
        var colCentroid = collinearPoly.Centroid;
        Assert(!double.IsNaN(colCentroid.X) && !double.IsNaN(colCentroid.Y) && !double.IsInfinity(colCentroid.X), "Collinear polygon Centroid does not produce NaN/Infinity");
        Assert(colCentroid.Equals(new Point2D(0, 0)), "Collinear polygon Centroid returns vertices[0]");

        var rect = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(100, 50),
            new Point2D(0, 50)
        });

        Assert(rect.Contains(new Point2D(50, 25)), "Contains interior point");
        Assert(!rect.Contains(new Point2D(150, 25)), "Contains exterior point right");
        Assert(!rect.Contains(new Point2D(50, -10)), "Contains exterior point bottom");
        Assert(!rect.Contains(new Point2D(-10, 25)), "Contains exterior point left");
        Assert(!rect.Contains(new Point2D(50, 60)), "Contains exterior point top");

        Assert(rect.Contains(new Point2D(0, 0), includeBoundary: true), "Contains corner vertex (0,0)");
        Assert(rect.Contains(new Point2D(50, 0), includeBoundary: true), "Contains bottom edge midpoint");
        Assert(rect.Contains(new Point2D(100, 25), includeBoundary: true), "Contains right edge midpoint");
        Assert(rect.Contains(new Point2D(50, 50), includeBoundary: true), "Contains top edge midpoint");
        Assert(rect.Contains(new Point2D(0, 25), includeBoundary: true), "Contains left edge midpoint");

        Assert(!rect.Contains(new Point2D(-10, 0), includeBoundary: false), "Ray-cast horizontal outside does not trigger false positive on vertex");

        var lPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(5, 5),
            new Point2D(5, 10),
            new Point2D(0, 10)
        });
        Assert(Near(lPoly.Area, 75.0), "L-shaped polygon area is 100 - 25 = 75");
        Assert(lPoly.Contains(new Point2D(2, 2)), "L-shape contains bottom-left interior");
        Assert(lPoly.Contains(new Point2D(2, 8)), "L-shape contains top-left interior");
        Assert(lPoly.Contains(new Point2D(8, 2)), "L-shape contains bottom-right interior");
        Assert(!lPoly.Contains(new Point2D(8, 8)), "L-shape excludes cutout exterior (8, 8)");
    }

    // =========================================================================
    // 5. LiangBarskyClipping
    // =========================================================================
    private static void RunLiangBarskyTests()
    {
        Console.WriteLine("--- Running LiangBarskyClipping Tests ---");
        var box = new BoundingBox2D(0.0, 0.0, 10.0, 10.0);

        var pIn0 = new Point2D(2.0, 3.0);
        var pIn1 = new Point2D(8.0, 7.0);
        bool clipIn = LiangBarskyClipping.Clip(pIn0, pIn1, box, out var cIn0, out var cIn1);
        Assert(clipIn, "Line fully inside: Intersects returns true");
        Assert(cIn0.IsAlmostEqualTo(pIn0) && cIn1.IsAlmostEqualTo(pIn1), "Line fully inside: Endpoints unchanged");

        var pOut0 = new Point2D(12.0, 5.0);
        var pOut1 = new Point2D(20.0, 5.0);
        bool clipOut = LiangBarskyClipping.Clip(pOut0, pOut1, box, out _, out _);
        Assert(!clipOut, "Line fully outside right: Returns false");

        var pOutDiag0 = new Point2D(-10.0, 20.0);
        var pOutDiag1 = new Point2D(-5.0, 15.0);
        Assert(!LiangBarskyClipping.Intersects(pOutDiag0, pOutDiag1, box), "Line diagonally outside: Intersects returns false");

        var pStartIn = new Point2D(5.0, 5.0);
        var pEndOut = new Point2D(15.0, 5.0);
        bool clip1Edge = LiangBarskyClipping.Clip(pStartIn, pEndOut, box, out var c1_0, out var c1_1);
        Assert(clip1Edge, "Start inside, end outside: Returns true");
        Assert(c1_0.IsAlmostEqualTo(new Point2D(5.0, 5.0)), "Start point remains (5, 5)");
        Assert(c1_1.IsAlmostEqualTo(new Point2D(10.0, 5.0)), "End point clipped to right edge (10, 5)");

        var pStartOut = new Point2D(5.0, -5.0);
        var pEndIn = new Point2D(5.0, 5.0);
        bool clip1EdgeB = LiangBarskyClipping.Clip(pStartOut, pEndIn, box, out var c1b_0, out var c1b_1);
        Assert(clip1EdgeB, "Start outside, end inside: Returns true");
        Assert(c1b_0.IsAlmostEqualTo(new Point2D(5.0, 0.0)), "Start clipped to bottom edge (5, 0)");
        Assert(c1b_1.IsAlmostEqualTo(new Point2D(5.0, 5.0)), "End remains (5, 5)");

        var pThrough0 = new Point2D(-5.0, 5.0);
        var pThrough1 = new Point2D(15.0, 5.0);
        bool clip2Edge = LiangBarskyClipping.Clip(pThrough0, pThrough1, box, out var c2_0, out var c2_1);
        Assert(clip2Edge, "Line passing through box horizontally: Returns true");
        Assert(c2_0.IsAlmostEqualTo(new Point2D(0.0, 5.0)), "Entry point clipped to left edge (0, 5)");
        Assert(c2_1.IsAlmostEqualTo(new Point2D(10.0, 5.0)), "Exit point clipped to right edge (10, 5)");

        var pDiag0 = new Point2D(-5.0, -5.0);
        var pDiag1 = new Point2D(15.0, 15.0);
        bool clipDiag = LiangBarskyClipping.Clip(pDiag0, pDiag1, box, out var cd_0, out var cd_1);
        Assert(clipDiag, "Diagonal line passing through box: Returns true");
        Assert(cd_0.IsAlmostEqualTo(new Point2D(0.0, 0.0)), "Diagonal entry at bottom-left corner (0, 0)");
        Assert(cd_1.IsAlmostEqualTo(new Point2D(10.0, 10.0)), "Diagonal exit at top-right corner (10, 10)");

        var pEdgeIn0 = new Point2D(2.0, 0.0);
        var pEdgeIn1 = new Point2D(8.0, 0.0);
        bool clipEdgeIn = LiangBarskyClipping.Clip(pEdgeIn0, pEdgeIn1, box, out var ceIn0, out var ceIn1);
        Assert(clipEdgeIn, "Collinear segment on edge: Returns true");
        Assert(ceIn0.IsAlmostEqualTo(pEdgeIn0) && ceIn1.IsAlmostEqualTo(pEdgeIn1), "Collinear segment endpoints unchanged");

        var pEdgeExt0 = new Point2D(-5.0, 0.0);
        var pEdgeExt1 = new Point2D(15.0, 0.0);
        bool clipEdgeExt = LiangBarskyClipping.Clip(pEdgeExt0, pEdgeExt1, box, out var ceExt0, out var ceExt1);
        Assert(clipEdgeExt, "Collinear extended segment on edge: Returns true");
        Assert(ceExt0.IsAlmostEqualTo(new Point2D(0.0, 0.0)), "Collinear entry at corner (0, 0)");
        Assert(ceExt1.IsAlmostEqualTo(new Point2D(10.0, 0.0)), "Collinear exit at corner (10, 0)");

        var pEdgeOut0 = new Point2D(12.0, 0.0);
        var pEdgeOut1 = new Point2D(18.0, 0.0);
        bool clipEdgeOut = LiangBarskyClipping.Clip(pEdgeOut0, pEdgeOut1, box, out _, out _);
        Assert(!clipEdgeOut, "Collinear line outside segment: Returns false");

        var pZeroIn = new Point2D(4.0, 4.0);
        bool clipPtIn = LiangBarskyClipping.Clip(pZeroIn, pZeroIn, box, out var cpt0, out var cpt1);
        Assert(clipPtIn, "Zero-length line inside box: Returns true");
        Assert(cpt0.IsAlmostEqualTo(pZeroIn) && cpt1.IsAlmostEqualTo(pZeroIn), "Zero-length line points unchanged");

        var pZeroOut = new Point2D(15.0, 4.0);
        bool clipPtOut = LiangBarskyClipping.Clip(pZeroOut, pZeroOut, box, out _, out _);
        Assert(!clipPtOut, "Zero-length line outside box: Returns false");

        var pointBox = new BoundingBox2D(5.0, 5.0, 5.0, 5.0);
        bool clipThroughPtBox = LiangBarskyClipping.Clip(new Point2D(0, 5), new Point2D(10, 5), pointBox, out var cpb0, out var cpb1);
        Assert(clipThroughPtBox, "Line through point box: Returns true");
        Assert(cpb0.IsAlmostEqualTo(new Point2D(5, 5)) && cpb1.IsAlmostEqualTo(new Point2D(5, 5)), "Clipped to single point (5, 5)");
    }

    // =========================================================================
    // 6. Spatial Algorithms
    // =========================================================================
    private static void RunSpatialAlgorithmsTests()
    {
        Console.WriteLine("--- Running Spatial Algorithms Tests ---");

        var wall = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(10000, 0),
            ThicknessMm = 220.0,
            Openings = new List<OpeningDef>
            {
                new OpeningDef { Id = "D1", DistanceAlongWallMm = 3000.0, WidthMm = 1000.0, HeightMm = 2200.0, Kind = OpeningKind.Door }
            }
        };

        var segmenter = new WallSegmenter();
        var segments = segmenter.Segment(wall);
        Assert(segments.Count == 2, "WallSegmenter creates 2 segments around 1 opening");
        Assert(Near(segments[0].StartPoint.X, 0.0) && Near(segments[0].EndPoint.X, 3000.0), "Segment 1 span [0, 3000]");
        Assert(Near(segments[1].StartPoint.X, 4000.0) && Near(segments[1].EndPoint.X, 10000.0), "Segment 2 span [4000, 10000]");

        Assert(Near(segments[0].Polygon.Area, 3000.0 * 220.0), "Solid polygon 1 area == length * thickness");
        Assert(Near(segments[1].Polygon.Area, 6000.0 * 220.0), "Solid polygon 2 area == length * thickness");

        var roomPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(5000, 0),
            new Point2D(5000, 4000),
            new Point2D(0, 4000)
        });

        var room = new RoomDef
        {
            Name = "Living Room",
            BoundaryPolygon = roomPoly,
            TagLocation = new Point2D(2500, 2000),
            TheoreticalAreaM2 = 20.0
        };

        var roomCalc = new RoomAreaCalculator();
        double roomAreaSqM = roomCalc.ComputeAreaM2(roomPoly);
        Assert(Near(roomAreaSqM, 20.0), "RoomAreaCalculator computes exact 20 m2");
        Assert(roomCalc.ContainsPoint(roomPoly, room.TagLocation), "Room tag is inside boundary");

        var valResult = roomCalc.ValidateRoom(room);
        Assert(valResult.IsValid && valResult.IsEnclosed && valResult.IsTagInside, "ValidateRoom produces valid result");
    }

    // =========================================================================
    // 7. TCVN Math
    // =========================================================================
    private static void RunTcvnMathTests()
    {
        Console.WriteLine("--- Running TCVN Detailing Math Tests ---");

        double w10 = TcvnRebarCalculator.CalculateUnitWeight(10.0);
        Assert(Near(w10, 100.0 / 162.2, 1e-4), "TCVN unit weight D10 == 100/162.2");

        double w18 = TcvnRebarCalculator.CalculateUnitWeight(18.0);
        Assert(Near(w18, 324.0 / 162.2, 1e-4), "TCVN unit weight D18 == 324/162.2");

        var rebar = new RebarDef
        {
            DiameterMm = 18.0,
            LengthMm = 6000.0,
            Quantity = 10
        };
        double totalKg = rebar.TotalWeightKg;
        double expectedKg = (324.0 / 162.2) * 6.0 * 10;
        Assert(Near(totalKg, expectedKg, 1e-3), "RebarDef.TotalWeightKg computes correctly");

        Assert(Near(TcvnRebarCalculator.CalculateAnchorageLengthMm(20.0), 600.0), "Anchorage 30D for D20 is 600mm");
        Assert(Near(TcvnRebarCalculator.CalculateTensionLapLengthMm(20.0), 800.0), "Tension lap 40D for D20 is 800mm");
        Assert(Near(TcvnRebarCalculator.CalculateHook90LengthMm(20.0), 240.0), "Hook 12D for D20 is 240mm");
    }

    // =========================================================================
    // 8. SpatialGridIndex
    // =========================================================================
    private static void RunSpatialGridIndexTests()
    {
        Console.WriteLine("--- Running SpatialGridIndex Tests ---");
        var grid = new SpatialGridIndex<string>(cellSizeMm: 1000.0);

        grid.Insert("ItemPos", new BoundingBox2D(500, 500, 1500, 1500));
        grid.Insert("ItemNeg", new BoundingBox2D(-1500, -1500, -500, -500));

        var queryPos = grid.Query(new BoundingBox2D(1200, 1200, 1300, 1300)).ToList();
        Assert(queryPos.Contains("ItemPos"), "SpatialGridIndex finds item in positive coords");
        Assert(!queryPos.Contains("ItemNeg"), "SpatialGridIndex excludes negative coords item");

        var queryNeg = grid.Query(new BoundingBox2D(-800, -800, -700, -700)).ToList();
        Assert(queryNeg.Contains("ItemNeg"), "SpatialGridIndex handles negative coordinates correctly via Math.Floor");
        Assert(!queryNeg.Contains("ItemPos"), "SpatialGridIndex excludes positive coords item");
    }

    // =========================================================================
    // 9. ADVERSARIAL STRESS TESTS: Liang-Barsky Corner, Collinear & Direction Traps
    // =========================================================================
    private static void RunAdversarialLiangBarskyStressTests()
    {
        Console.WriteLine("--- Running Adversarial LiangBarsky Stress Tests ---");
        var box = new BoundingBox2D(0.0, 0.0, 10.0, 10.0);

        var pRtoL_0 = new Point2D(15.0, 5.0);
        var pRtoL_1 = new Point2D(-5.0, 5.0);
        bool clipRL = LiangBarskyClipping.Clip(pRtoL_0, pRtoL_1, box, out var cRL0, out var cRL1);
        Assert(clipRL, "Reversed line right-to-left intersects");
        Assert(cRL0.IsAlmostEqualTo(new Point2D(10.0, 5.0)), "Reversed entry is on right edge (10, 5)");
        Assert(cRL1.IsAlmostEqualTo(new Point2D(0.0, 5.0)), "Reversed exit is on left edge (0, 5)");

        var pTopToBot_0 = new Point2D(5.0, 15.0);
        var pTopToBot_1 = new Point2D(5.0, -5.0);
        bool clipTB = LiangBarskyClipping.Clip(pTopToBot_0, pTopToBot_1, box, out var cTB0, out var cTB1);
        Assert(clipTB, "Vertical line top-to-bottom intersects");
        Assert(cTB0.IsAlmostEqualTo(new Point2D(5.0, 10.0)), "Entry is on top edge (5, 10)");
        Assert(cTB1.IsAlmostEqualTo(new Point2D(5.0, 0.0)), "Exit is on bottom edge (5, 0)");

        var pVertEdge0 = new Point2D(0.0, -5.0);
        var pVertEdge1 = new Point2D(0.0, 15.0);
        bool clipVertEdge = LiangBarskyClipping.Clip(pVertEdge0, pVertEdge1, box, out var cVE0, out var cVE1);
        Assert(clipVertEdge, "Collinear vertical edge line intersects");
        Assert(cVE0.IsAlmostEqualTo(new Point2D(0.0, 0.0)), "Entry at bottom-left corner (0, 0)");
        Assert(cVE1.IsAlmostEqualTo(new Point2D(0.0, 10.0)), "Exit at top-left corner (0, 10)");

        var pVertIn0 = new Point2D(10.0, 2.0);
        var pVertIn1 = new Point2D(10.0, 8.0);
        bool clipVertIn = LiangBarskyClipping.Clip(pVertIn0, pVertIn1, box, out var cVI0, out var cVI1);
        Assert(clipVertIn, "Collinear segment on right edge intersects");
        Assert(cVI0.IsAlmostEqualTo(pVertIn0) && cVI1.IsAlmostEqualTo(pVertIn1), "Endpoints unchanged on right edge");

        var pTouchCorner0 = new Point2D(-5.0, -5.0);
        var pTouchCorner1 = new Point2D(0.0, 0.0);
        bool clipTouchCorner = LiangBarskyClipping.Clip(pTouchCorner0, pTouchCorner1, box, out var cTC0, out var cTC1);
        Assert(clipTouchCorner, "Line touching corner (0, 0) from outside intersects");
        Assert(cTC0.IsAlmostEqualTo(new Point2D(0.0, 0.0)) && cTC1.IsAlmostEqualTo(new Point2D(0.0, 0.0)), "Clipped to single point (0, 0)");

        var pLeaveCorner0 = new Point2D(10.0, 10.0);
        var pLeaveCorner1 = new Point2D(15.0, 15.0);
        bool clipLeave = LiangBarskyClipping.Clip(pLeaveCorner0, pLeaveCorner1, box, out var cLC0, out var cLC1);
        Assert(clipLeave, "Line starting at corner (10, 10) and leaving intersects at start");
        Assert(cLC0.IsAlmostEqualTo(new Point2D(10.0, 10.0)) && cLC1.IsAlmostEqualTo(new Point2D(10.0, 10.0)), "Clipped to single point (10, 10)");

        var pNearPar0 = new Point2D(-5.0, 5.0);
        var pNearPar1 = new Point2D(15.0, 5.0 + 1e-11);
        bool clipNearPar = LiangBarskyClipping.Clip(pNearPar0, pNearPar1, box, out var cNP0, out var cNP1);
        Assert(clipNearPar, "Near-parallel line clips successfully");
        Assert(Near(cNP0.X, 0.0) && Near(cNP1.X, 10.0), "Near-parallel line clips at X=0 and X=10");

        var pAbove0 = new Point2D(-5.0, 10.0001);
        var pAbove1 = new Point2D(15.0, 10.0001);
        Assert(!LiangBarskyClipping.Intersects(pAbove0, pAbove1, box), "Line 0.0001 above top edge does NOT intersect");
    }

    // =========================================================================
    // 10. ADVERSARIAL STRESS TESTS: Polygon2D High-precision, Singularities, Stress
    // =========================================================================
    private static void RunAdversarialPolygonStressTests()
    {
        Console.WriteLine("--- Running Adversarial Polygon Stress Tests ---");

        // A. 10,000-vertex circular polygon
        int N = 10000;
        double R = 5000.0;
        var circleVertices = new List<Point2D>(N);
        for (int i = 0; i < N; i++)
        {
            double theta = (2.0 * Math.PI * i) / N;
            circleVertices.Add(new Point2D(R * Math.Cos(theta), R * Math.Sin(theta)));
        }
        var circlePoly = new Polygon2D(circleVertices);
        double expectedCircleArea = Math.PI * R * R;
        double expectedCircumference = 2.0 * Math.PI * R;

        Assert(Near(circlePoly.Area, expectedCircleArea, tol: 100.0), "10,000-gon area converges to pi*R^2 (rel error < 0.001%)");
        Assert(Near(circlePoly.Perimeter, expectedCircumference, tol: 1.0), "10,000-gon perimeter converges to 2*pi*R");
        Assert(circlePoly.Centroid.IsAlmostEqualTo(Point2D.Zero, tolerance: 1e-3), "10,000-gon centroid is (0, 0)");
        Assert(circlePoly.Contains(new Point2D(0, 0)), "Circle contains center");
        Assert(circlePoly.Contains(new Point2D(4999.0, 0)), "Circle contains interior near rim");
        Assert(!circlePoly.Contains(new Point2D(5001.0, 0)), "Circle excludes point outside rim");

        // B. Large coordinates (Revit global coordinate system: X, Y = 100,000,000mm = 100km)
        double offsetX = 100_000_000.0;
        double offsetY = 100_000_000.0;
        var largeSquare = new Polygon2D(new[]
        {
            new Point2D(offsetX, offsetY),
            new Point2D(offsetX + 1000.0, offsetY),
            new Point2D(offsetX + 1000.0, offsetY + 1000.0),
            new Point2D(offsetX, offsetY + 1000.0)
        });
        Assert(Near(largeSquare.Area, 1_000_000.0, tol: 1e-4), "Large coordinate polygon (100km offset) Shoelace area is exact 1e6 mm2");
        Assert(largeSquare.Centroid.IsAlmostEqualTo(new Point2D(offsetX + 500.0, offsetY + 500.0), tolerance: 1e-4), "Large coordinate centroid is exact");
        Assert(largeSquare.Contains(new Point2D(offsetX + 500.0, offsetY + 500.0)), "Large coordinate contains interior point");

        // C. Comb / Multi-cavity polygon (3 teeth at [0..10], [20..30], [40..50]; cavities at [10..20], [30..40])
        var combVertices = new List<Point2D>
        {
            new Point2D(0, 0),
            new Point2D(50, 0),
            new Point2D(50, 20),
            new Point2D(40, 20),
            new Point2D(40, 5),   // cavity 2
            new Point2D(30, 5),
            new Point2D(30, 20),
            new Point2D(20, 20),
            new Point2D(20, 5),   // cavity 1
            new Point2D(10, 5),
            new Point2D(10, 20),
            new Point2D(0, 20)
        };
        var combPoly = new Polygon2D(combVertices);
        // Gross: 50 * 20 = 1000. Cavities: 2 * (10 * 15) = 300. Net Area = 700.
        Assert(Near(combPoly.Area, 700.0), "Comb polygon area == 700.0");
        // Interior in tooth 1: (5, 10)
        Assert(combPoly.Contains(new Point2D(5, 10)), "Comb contains tooth 1");
        // Cavity 1: (15, 10) -> OUTSIDE
        Assert(!combPoly.Contains(new Point2D(15, 10)), "Comb excludes cavity 1 exterior");
        // Middle tooth: (25, 10) -> INSIDE
        Assert(combPoly.Contains(new Point2D(25, 10)), "Comb contains tooth 2");
        // Cavity 2: (35, 10) -> OUTSIDE
        Assert(!combPoly.Contains(new Point2D(35, 10)), "Comb excludes cavity 2 exterior");
        // Tooth 3: (45, 10) -> INSIDE
        Assert(combPoly.Contains(new Point2D(45, 10)), "Comb contains tooth 3");
        // Solid base: (25, 2) -> INSIDE
        Assert(combPoly.Contains(new Point2D(25, 2)), "Comb contains base across cavities");

        // D. Self-intersecting bowtie polygon (0,0) -> (10,10) -> (0,10) -> (10,0)
        var bowtie = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 10),
            new Point2D(0, 10),
            new Point2D(10, 0)
        });
        Assert(Near(bowtie.SignedArea, 0.0), "Bowtie signed area cancels to 0.0");
        Assert(Near(bowtie.Area, 0.0), "Bowtie absolute area is 0.0");
        Assert(!double.IsNaN(bowtie.Centroid.X), "Bowtie centroid does not produce NaN");
    }

    // =========================================================================
    // 11. ADVERSARIAL STRESS TESTS: Vector2D Chirality, Angles & Limits
    // =========================================================================
    private static void RunAdversarialVectorStressTests()
    {
        Console.WriteLine("--- Running Adversarial Vector Stress Tests ---");

        var vx = new Vector2D(1, 0);
        var vNegX = new Vector2D(-1, 0);
        double angleAnti = vx.AngleBetween(vNegX);
        Assert(Near(Math.Abs(angleAnti), Math.PI), "AngleBetween anti-parallel vectors is PI (or -PI)");

        var v45 = new Vector2D(1, 1);
        Assert(Near(vx.AngleBetween(v45), Math.PI / 4.0), "AngleBetween UnitX and (1, 1) is +PI/4");
        Assert(Near(v45.AngleBetween(vx), -Math.PI / 4.0), "AngleBetween (1, 1) and UnitX is -PI/4");

        var vOrig = new Vector2D(7.5, -3.2);
        var vRot1 = vOrig.LeftNormal;
        var vRot2 = vRot1.LeftNormal;
        var vRot3 = vRot2.LeftNormal;
        var vRot4 = vRot3.LeftNormal;
        Assert(vRot4.IsAlmostEqualTo(vOrig), "Four LeftNormal rotations return to original vector");
        Assert(vRot2.IsAlmostEqualTo(-vOrig), "Two LeftNormal rotations negate vector");

        var vRight4 = vOrig.RightNormal.RightNormal.RightNormal.RightNormal;
        Assert(vRight4.IsAlmostEqualTo(vOrig), "Four RightNormal rotations return to original vector");

        Assert(vOrig.LeftNormal.IsAlmostEqualTo(-vOrig.RightNormal), "LeftNormal is negative of RightNormal");

        var vAboveThreshold = new Vector2D(1.01e-9, 0);
        Assert(vAboveThreshold.Normalize().Length > 0.99, "Vector just above 1e-9 normalizes cleanly");

        var vBelowThreshold = new Vector2D(0.99e-9, 0);
        AssertThrows<InvalidOperationException>(() => vBelowThreshold.Normalize(), "Vector just below 1e-9 throws on Normalize");
    }

    // =========================================================================
    // 12. ADVERSARIAL STRESS TESTS: BoundingBox2D Boundary Poking & Deflate Limits
    // =========================================================================
    private static void RunAdversarialBoundingBoxStressTests()
    {
        Console.WriteLine("--- Running Adversarial BoundingBox Stress Tests ---");
        var box = new BoundingBox2D(0, 0, 100, 100);

        var touchTop = new BoundingBox2D(0, 100, 100, 200);
        Assert(box.Intersects(touchTop), "Box intersects box touching its top edge");
        var touchTopInter = box.Intersection(touchTop);
        Assert(touchTopInter.HasValue && touchTopInter.Value.Height == 0.0 && touchTopInter.Value.Width == 100.0, "Intersection with touching top box is zero-height line box");

        var separateTop = new BoundingBox2D(0, 100.000000001, 100, 200);
        Assert(!box.Intersects(separateTop), "Box does not intersect box separated by 1e-9");

        var deflated100 = box.Deflate(1.0);
        Assert(Near(deflated100.Width, 0.0) && Near(deflated100.Height, 0.0), "Deflate 1.0 produces zero-size point box");
        Assert(deflated100.Center.IsAlmostEqualTo(box.Center), "Deflate 1.0 collapses at Center");
    }
}
