using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// AutoCAD command methods for dimension trimming (catdim), splitting (ngatdim),
    /// continuous chaining (dnoi), and slope annotation (sl).
    /// </summary>
    public static class DimensionUtilityCommands
    {
        [CommandMethod("catdim")]
        [CommandMethod("cat_chan_dim")]
        [CommandMethod("locdim")]
        [CommandMethod("loc_dim")]
        [CommandMethod("cat")]
        [CommandMethod("cat_doituong")]
        public static void TrimWitnessLines()
        {
            DimensionUtilityController.TrimDimensionWitnessLinesInteractive();
        }

        [CommandMethod("ngatdim")]
        [CommandMethod("ngat_dim")]
        [CommandMethod("pvn")]
        [CommandMethod("phan_vung_noi")]
        public static void SplitDimension()
        {
            DimensionUtilityController.SplitDimensionInteractive();
        }

        [CommandMethod("dnoi")]
        [CommandMethod("dim_noi_tiep")]
        public static void ContinueDimension()
        {
            DimensionUtilityController.ContinueDimensionInteractive();
        }

        [CommandMethod("sl")]
        [CommandMethod("ghi_do_doc")]
        public static void AnnotateSlope()
        {
            DimensionUtilityController.DrawSlopeAnnotationInteractive();
        }
    }
}
