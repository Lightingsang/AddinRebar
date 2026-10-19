using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for inclined beams and curved beams (vdx, vdc).
    /// </summary>
    public static class InclinedCurvedBeamCommands
    {
        [CommandMethod("ve_dam_xien")]
        [CommandMethod("vdx")]
        public static void VeDamXien() => CurvedInclinedBeamController.DrawInclinedBeam();

        [CommandMethod("ve_dam_cong")]
        [CommandMethod("vdc")]
        public static void VeDamCong() => CurvedInclinedBeamController.DrawCurvedBeam();
    }
}
