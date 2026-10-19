using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for saving/loading beam models to DWG XRecords and 2D UI preview
    /// (std, stda, atd, shtd).
    /// </summary>
    public static class BeamPersistenceCommands
    {
        [CommandMethod("Save_thep_dam")]
        [CommandMethod("std")]
        public static void SaveThepDam() => BeamPersistenceController.SaveBeamRebarToDwg();

        [CommandMethod("Save_thep_dam_all")]
        [CommandMethod("stda")]
        public static void SaveThepDamAll() => BeamPersistenceController.SaveAllBeamsToDwg();

        [CommandMethod("Add_thep_dam")]
        [CommandMethod("atd")]
        public static void AddThepDam() => BeamPersistenceController.AddRebarLayerInteractive();

        [CommandMethod("show_thep_dam")]
        [CommandMethod("shtd")]
        public static void ShowThepDam() => BeamPersistenceController.PreviewBeamRebar2D();
    }
}
