using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// AutoCAD command methods for Quantity Surveying (QS) workflow,
    /// framing plan takeoff, deduction, Excel export, element search, and schedule table.
    /// </summary>
    public static class QsCommands
    {
        [CommandMethod("QS")]
        public static void QsWorkflow()
        {
            QsTakeoffController.ExecuteMasterWorkflow();
        }

        [CommandMethod("QS_mbkc")]
        public static void QsFramingPlan()
        {
            QsTakeoffController.ExecuteMasterWorkflow();
        }

        [CommandMethod("QS_tru")]
        public static void QsDeduction()
        {
            QsTakeoffController.ExecuteMasterWorkflow();
        }

        [CommandMethod("xuatQS")]
        public static void ExportQsExcel()
        {
            QsTakeoffController.ExportExcel();
        }

        [CommandMethod("timqs")]
        public static void FindQsElement()
        {
            QsTakeoffController.FindElement();
        }

        [CommandMethod("tkqs")]
        public static void GenerateQsTable()
        {
            QsTakeoffController.GenerateSummaryTable();
        }

        [CommandMethod("chiatam")]
        [CommandMethod("chia_tam")]
        [CommandMethod("CHIA_TAM_LIGHT")]
        public static void OptimizePanelLayout()
        {
            PanelTileLayoutController.OptimizePanelLayoutInteractive();
        }

        [CommandMethod("opgach")]
        [CommandMethod("op_gach")]
        [CommandMethod("OP_GACH_LIGHT")]
        public static void OptimizeTileLayout()
        {
            PanelTileLayoutController.OptimizeTileLayoutInteractive();
        }
    }
}
