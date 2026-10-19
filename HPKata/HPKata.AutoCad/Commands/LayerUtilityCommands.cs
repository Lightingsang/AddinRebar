using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// AutoCAD command methods for layer visibility control (anlayer, hienlayer) and layer conversion (doilayer).
    /// </summary>
    public static class LayerUtilityCommands
    {
        [CommandMethod("anlayer")]
        [CommandMethod("an_layer")]
        public static void HideLayer()
        {
            LayerUtilityController.HideSelectedEntityLayer();
        }

        [CommandMethod("hienlayer")]
        [CommandMethod("hien_tat_ca_layer")]
        public static void UnhideAllLayers()
        {
            LayerUtilityController.UnhideAllLayers();
        }

        [CommandMethod("doilayer")]
        [CommandMethod("doi_layer")]
        public static void ChangeLayer()
        {
            LayerUtilityController.ChangeEntitiesLayer();
        }
    }
}
