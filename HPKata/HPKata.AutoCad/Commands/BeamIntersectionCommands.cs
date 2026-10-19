using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for beam intersections, hanger stirrups, and beam plan tagging
    /// (damgiao, giaodam, catdam, raidam).
    /// </summary>
    public static class BeamIntersectionCommands
    {
        [CommandMethod("dam_giao")]
        [CommandMethod("damgiao")]
        public static void DamGiao() => BeamIntersectionController.DrawIntersectionInteractive();

        [CommandMethod("giao_dam")]
        [CommandMethod("giaodam")]
        public static void GiaoDam() => BeamIntersectionController.AutoDetectIntersections();

        [CommandMethod("cat_dam")]
        [CommandMethod("catdam")]
        public static void CatDam() => BeamIntersectionController.SplitBeamAtBoundary();

        [CommandMethod("raidam")]
        public static void RaiDam() => BeamIntersectionController.PlaceBeamPlanTags();
    }
}
