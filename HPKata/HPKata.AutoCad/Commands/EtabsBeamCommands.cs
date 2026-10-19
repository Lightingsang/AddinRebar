using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for ETABS live connection, loadbeam import, and slab load distribution
    /// (Etabs, loadbeam, GanTai, HoatTai).
    /// </summary>
    public static class EtabsBeamCommands
    {
        [CommandMethod("Etabs")]
        public static void Etabs() => BeamEtabsController.ConnectEtabsAndImport();

        [CommandMethod("GanTai")]
        public static void GanTai() => BeamEtabsController.AssignDeadLoad();

        [CommandMethod("HoatTai")]
        public static void HoatTai() => BeamEtabsController.AssignLiveLoad();
    }
}
