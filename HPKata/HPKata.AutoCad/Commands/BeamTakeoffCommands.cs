using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for beam schedule generation and framing plan scanning (tkdam, qd, qda).
    /// </summary>
    public static class BeamTakeoffCommands
    {
        [CommandMethod("tkdam")]
        public static void TkDam() => BeamTakeoffController.GenerateBeamScheduleTable();

        [CommandMethod("quet_dam")]
        [CommandMethod("qd")]
        public static void QuetDam() => BeamTakeoffController.ScanFramingPlanRegion();

        [CommandMethod("quet_dam_all")]
        [CommandMethod("qda")]
        public static void QuetDamAll() => BeamTakeoffController.ScanFramingPlanAll();
    }
}
