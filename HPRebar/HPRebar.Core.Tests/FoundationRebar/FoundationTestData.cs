using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.Core.Tests.FoundationRebar;

/// <summary>
/// Reusable test fixtures and factory methods for FoundationRebar unit test suites.
/// </summary>
internal static class FoundationTestData
{
    public static FoundationGeometrySnapshot StandardSnapshot(
        double length = 3000.0,
        double width = 2000.0,
        double thickness = 500.0,
        Point3? origin = null)
    {
        return FoundationGeometrySnapshot.CreateAxisAligned(
            origin ?? new Point3(0, 0, 0),
            length,
            width,
            thickness);
    }

    public static FoundationGeometrySnapshot OrientedSnapshot(
        double angleDegrees,
        double length = 3000.0,
        double width = 2000.0,
        double thickness = 500.0,
        Point3? origin = null)
    {
        return FoundationGeometrySnapshot.CreateOriented(
            origin ?? new Point3(100.0, 200.0, 50.0),
            angleDegrees,
            length,
            width,
            thickness);
    }

    public static FoundationRebarSpec StandardSpec(
        double spacingBottomX = 150.0,
        double spacingBottomY = 150.0,
        double spacingTopX = 200.0,
        double spacingTopY = 200.0,
        double diameterBottomX = 16.0,
        double diameterBottomY = 16.0,
        double diameterTopX = 12.0,
        double diameterTopY = 12.0,
        double coverTop = 50.0,
        double coverBottom = 50.0,
        double coverSide = 50.0,
        bool isTopMatEnabled = true,
        FoundationHookType hookType = FoundationHookType.None,
        double hookLength = 0.0)
    {
        return new FoundationRebarSpec
        {
            SpacingBottomX = spacingBottomX,
            SpacingBottomY = spacingBottomY,
            SpacingTopX = spacingTopX,
            SpacingTopY = spacingTopY,
            DiameterBottomX = diameterBottomX,
            DiameterBottomY = diameterBottomY,
            DiameterTopX = diameterTopX,
            DiameterTopY = diameterTopY,
            CoverTop = coverTop,
            CoverBottom = coverBottom,
            CoverSide = coverSide,
            IsTopMatEnabled = isTopMatEnabled,
            HookType = hookType,
            HookLength = hookLength
        };
    }
}
