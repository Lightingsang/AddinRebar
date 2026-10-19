using System;
using System.Globalization;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Drafting
{
    /// <summary>
    /// Host-independent 2D CAD drafting generator for steel portal frames, roof purlin plans, and anchor bolt details.
    /// </summary>
    public sealed partial class SteelFrameDraftingGenerator
    {
        public CadDraftingBuffer GenerateElevationMiddleFrameBT(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawElevationMiddleFrameBTInternal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GenerateElevationMiddleFrame3(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawElevationMiddleFrame3Internal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GenerateElevationMiddleFrame3BT(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawElevationMiddleFrame3BTInternal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GenerateElevationMiddleFrame5(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawElevationMiddleFrame5Internal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GenerateElevationMiddleFrame5a(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawElevationMiddleFrame5aInternal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GenerateElevationVDS1(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawElevationVDS1Internal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GeneratePlanPurlinRoof(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawPlanPurlinRoofInternal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GeneratePlanPurlinMonitor(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawPlanPurlinMonitorInternal(buffer, opt);
            return buffer;
        }

        public CadDraftingBuffer GenerateDetailAnchorBolt(SteelFrameDraftingOptions? options = null)
        {
            var opt = options ?? new SteelFrameDraftingOptions();
            var buffer = new CadDraftingBuffer();
            DrawDetailAnchorBoltInternal(buffer, opt);
            return buffer;
        }

        private static void DrawAxis(CadDraftingBuffer buffer, Point2D origin, string name, double height, SteelFrameDraftingOptions opt)
        {
            buffer.AddLine(origin, new Point2D(origin.X, origin.Y + height), opt.AxisLayer, CadColor.RedIndex, 0.15);
            buffer.AddCircle(new Point2D(origin.X, origin.Y - 400.0), 300.0, opt.AxisLayer, CadColor.RedIndex);
            buffer.AddText(name, new Point2D(origin.X - 100.0, origin.Y - 500.0), 250.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private static void DrawLevelMark(CadDraftingBuffer buffer, Point2D pos, double elevM, string title, SteelFrameDraftingOptions opt)
        {
            string sign = elevM >= 0 ? "+" : string.Empty;
            string elevStr = $"{title} ({sign}{elevM.ToString("F3", CultureInfo.InvariantCulture)})";
            buffer.AddLine(pos, new Point2D(pos.X + 600.0, pos.Y), opt.DimLayer, CadColor.GreenIndex, 0.18);
            buffer.AddText(elevStr, new Point2D(pos.X + 650.0, pos.Y - 50.0), 200.0, opt.TextLayer, CadColor.WhiteIndex);
        }
    }
}
