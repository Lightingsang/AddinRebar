using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for foundation grade beams and ground tie beams (vdm, vgm, vgms).
    /// </summary>
    public static class TieBeamCommands
    {
        [CommandMethod("ve_dam_mong")]
        [CommandMethod("vdm")]
        public static void VeDamMong() => FoundationBeamController.DrawFoundationGradeBeam();

        [CommandMethod("ve_giang_mong")]
        [CommandMethod("vgm")]
        public static void VeGiangMong() => FoundationBeamController.DrawFoundationTieBeam();

        [CommandMethod("ve_giang_shop")]
        [CommandMethod("vgms")]
        public static void VeGiangShop() => FoundationBeamController.DrawFoundationTieBeamShop();
    }
}
