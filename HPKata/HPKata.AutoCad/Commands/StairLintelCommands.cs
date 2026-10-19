using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for staircase detailing, lintel beams, and universal cross-sections
    /// (ve_thang/vethang, velanhto, vemc).
    /// </summary>
    public static class StairLintelCommands
    {
        [CommandMethod("ve_thang")]
        [CommandMethod("vethang")]
        public static void VeThang() => StairDrawController.DrawStairInteractive();

        [CommandMethod("velanhto")]
        public static void VeLanhto() => LintelDrawController.DrawLintelInteractive();

        [CommandMethod("vemc")]
        public static void VeMc() => SectionCutController.DrawGenericSectionInteractive();
    }
}
