## 2026-10-06T00:00:05Z
You are teamwork_preview_challenger assigned to Milestone 1 (M1: Geometry & Spatial Math Adversarial Verification).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_1_hpbim

Read the authoritative request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)

Read the project specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md

Read Worker handoff at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_hpbim\handoff.md

Your task:
1. Empirically verify the correctness, mathematical rigor, and edge-case behavior of `HPBim.Core` geometry primitives:
   - `Point2D`, `Point3D`, `Vector2D`: normal vector generation (left-hand normal (-y, x)), normalization of zero vector, tolerance equality.
   - `BoundingBox2D`: intersects, contains point, degenerate zero-size boxes, empty boxes.
   - `Polygon2D`: Shoelace formula for signed/absolute area, CW and CCW winding orders, collinear points, empty vertices, centroid calculation, ray-casting point-in-polygon.
   - `LiangBarskyClipping`: Line fully inside, line fully outside, line intersecting 1 edge, line intersecting 2 edges, collinear line with box edge.
2. Build and run empirical checks or write a standalone C# test/runner to verify the math against known mathematical oracles.
3. Provide a clear verdict: APPROVE or REQUEST_CHANGES.
4. Write your challenge report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_1_hpbim\handoff.md`, update `progress.md`, and send message to parent.
