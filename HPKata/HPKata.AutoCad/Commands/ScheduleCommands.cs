using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Interop;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for rebar scheduling, diameter summaries, couplers, cutting optimization, and Excel export.
    /// </summary>
    public static class ScheduleCommands
    {
        [CommandMethod("thong_ke_thep")]
        [CommandMethod("tkt")]
        public static void ThongKeThep()
        {
            ExecuteWithContext(ctx => ScheduleCommandRunner.ExecuteSchedule(ctx, interactive: true));
        }

        [CommandMethod("sua_thong_ke")]
        [CommandMethod("suatk")]
        public static void SuaThongKe()
        {
            ExecuteWithContext(ctx => ScheduleCommandRunner.ExecuteSchedule(ctx, interactive: true));
        }

        [CommandMethod("sstk")]
        public static void SoSanhThongKe()
        {
            ExecuteWithContext(ctx => SummaryCommandRunner.ExecuteCompareSchedule(ctx));
        }

        [CommandMethod("tong_hop_thep")]
        [CommandMethod("ththep")]
        [CommandMethod("tha")]
        public static void TongHopThep()
        {
            ExecuteWithContext(ctx => SummaryCommandRunner.ExecuteSummary(ctx));
        }

        [CommandMethod("thong_ke_coupler")]
        [CommandMethod("tkco")]
        [CommandMethod("bvbs")]
        public static void ThongKeCoupler()
        {
            ExecuteWithContext(ctx => SummaryCommandRunner.ExecuteCouplerSummary(ctx));
        }

        [CommandMethod("to_hop_thep")]
        [CommandMethod("tohopthep")]
        public static void ToHopThep()
        {
            ExecuteWithContext(ctx => CuttingStockCommandRunner.ExecuteCuttingStock(ctx, headless: false));
        }

        [CommandMethod("loadkho")]
        public static void LoadKho()
        {
            ExecuteWithContext(CuttingStockCommandRunner.ExecuteLoadKho);
        }

        [CommandMethod("upkho")]
        public static void UpKho()
        {
            ExecuteWithContext(CuttingStockCommandRunner.ExecuteUpKho);
        }

        [CommandMethod("toexcel")]
        public static void ToExcel()
        {
            ExecuteWithContext(ctx => ExportCommandRunner.ExecuteExcelExport(ctx, isQhPlus: false));
        }

        [CommandMethod("toexcelqh")]
        public static void ToExcelQh()
        {
            ExecuteWithContext(ctx => ExportCommandRunner.ExecuteExcelExport(ctx, isQhPlus: true));
        }

        [CommandMethod("in_tem_thep")]
        [CommandMethod("itt")]
        public static void InTemThep()
        {
            ExecuteWithContext(ctx => ExportCommandRunner.ExecuteTagPrint(ctx));
        }

        private static void ExecuteWithContext(System.Action<CadDocumentContext> action)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (doc.LockDocument())
            {
                var ctx = new CadDocumentContext(doc);
                action(ctx);
            }
        }
    }
}
