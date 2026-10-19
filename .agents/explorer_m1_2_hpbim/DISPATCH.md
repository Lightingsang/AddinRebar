## 2026-10-05T23:40:37Z
[Message] timestamp=2026-10-05T23:40:37Z sender=f21aade0-effc-4aa5-a071-ae17d49dabe3 priority=MESSAGE_PRIORITY_HIGH content=You are teamwork_preview_explorer assigned to Milestone 1 (M1: Domain BIM Schema & Spatial Algorithms) of the HPBim deliverable.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_2_hpbim

Read the authoritative user request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)

Read the project specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md

Consult prior survey findings in:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_vncad\handoff.md

Your task:
Analyze and formulate the exact implementation blueprint for:
1. Domain BIM schema models under `src/HPBim.Core/Models/`:
   - `AxisGrid.cs`: Name, CoordinateMm, Direction (X/Y), StartPoint, EndPoint.
   - `LevelDef.cs`: Name, ElevationM, ElevationMm, HeightMm, FloorNumber.
   - `ColumnDef.cs`: Id, CenterLocation, WidthMm (220), DepthMm (220), BaseLevel, TopLevel.
   - `WallDef.cs`: Id, StartPoint, EndPoint, ThicknessMm (110/220), BaseLevel, TopLevel, Openings.
   - `OpeningDef.cs`: Id, HostWallId, DistanceAlongWallMm, WidthMm, HeightMm, SillHeightMm, Kind (Door, Door2, Window, Opening), Swing.
   - `BeamDef.cs`: Id, StartPoint, EndPoint, WidthMm, HeightMm, Level, BeamType (D1, D2, D3, GM).
   - `SlabDef.cs`: Id, Boundary, ThicknessMm (100), Level, ElevationOffsetMm, Voids.
   - `RoomDef.cs`: Name, TagLocation, TheoreticalAreaM2, Level, BoundaryPolygon.
   - `MepFixtureDef.cs`: Id, Category, FamilyName, Location, RotationDeg, Level.
   - `PipeRunDef.cs`: Id, SystemType (WaterSupply, SoilDrain, WasteDrain), DiameterMm, Points, Slope.
   - `RebarDef.cs`: BarMark, DiameterMm, SteelGrade, ShapeType, LengthMm, Quantity, SpacingMm, CoverMm.
   - `BimModel.cs`: Aggregates all levels, grids, columns, walls, beams, slabs, rooms, fixtures, pipes.
2. Spatial algorithms under `src/HPBim.Core/Algorithms/`:
   - `WallSegmenter.cs`: Splits wall centerline into solid segments between door/window cutouts.
   - `RoomAreaCalculator.cs`: Shoelace formula area computation, checks enclosed status.
   - `OpeningPlacementEngine.cs`: Computes exact opening center point in 2D and 3D relative to host wall.
3. Clean code compliance: Methods <= 50 lines, Classes <= 300 lines, 0 empty catches.

Deliverables:
- Write your implementation plan to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_2_hpbim\handoff.md.
- Update g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_2_hpbim\progress.md.
- Send a message to parent with your summary and handoff path.
