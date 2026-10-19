using System;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Geometric helpers for structural beam spans and discretization.
    /// Replaces KhoangCachDenDiemTraiGoi, KhoangCachDenDiemPhaiGoi, TimSoDoan, TimSoNhip native exports.
    /// </summary>
    public static class BeamSpanGeometry
    {
        public static double DistanceToLeftSupportFace(Point2D pt, Point2D leftColumnCenter, double columnWidth)
        {
            double leftFaceX = leftColumnCenter.X + columnWidth * 0.5;
            return Math.Max(0.0, pt.X - leftFaceX);
        }

        public static double DistanceToRightSupportFace(Point2D pt, Point2D rightColumnCenter, double columnWidth)
        {
            double rightFaceX = rightColumnCenter.X - columnWidth * 0.5;
            return Math.Max(0.0, rightFaceX - pt.X);
        }

        public static int ComputeSegmentCount(double length, double maxSegmentLength)
        {
            if (length <= 0.0 || maxSegmentLength <= 0.0)
            {
                return 1;
            }
            return (int)Math.Ceiling(length / maxSegmentLength);
        }

        public static double ComputeClearSpan(double centerSpan, double leftSupportWidth, double rightSupportWidth)
        {
            double clearSpan = centerSpan - (leftSupportWidth + rightSupportWidth) * 0.5;
            return Math.Max(0.0, clearSpan);
        }
    }
}
