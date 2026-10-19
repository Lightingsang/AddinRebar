using System.Collections.Generic;
using ClosedXML.Excel;
using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Adapters.Tests.Excel
{
    public sealed class ExcelActionExecutionTests
    {
        private readonly DceExcelActionDispatcher _dispatcher = new();

        [Fact]
        public void BeamAction_BeaDesignAndTinhDam_ExecutesCalculations()
        {
            var ctx = new ExcelActionContext();
            ctx.Parameters["b"] = 300.0;
            ctx.Parameters["h"] = 600.0;
            ctx.Parameters["Moment"] = 180.0;
            ctx.Parameters["Shear"] = 90.0;

            var resBea = _dispatcher.ExecuteAction("Bea_Design", ctx);
            Assert.True(resBea.IsSuccess);
            Assert.NotNull(resBea.Data);

            var resAlias = _dispatcher.ExecuteAction("TinhDam", ctx);
            Assert.True(resAlias.IsSuccess);
            Assert.True(resAlias.ExecutionTimeMs >= 0.0);
        }

        [Fact]
        public void ColumnWallAction_ColDesignAndTinhCot_EvaluatesInteractionRatio()
        {
            var ctx = new ExcelActionContext();
            ctx.Parameters["b"] = 400.0;
            ctx.Parameters["h"] = 400.0;
            ctx.Parameters["AxialForce"] = 1500.0;
            ctx.Parameters["MomentX"] = 100.0;
            ctx.Parameters["MomentY"] = 80.0;
            ctx.Parameters["AsTotal"] = 1600.0;

            var resCol = _dispatcher.ExecuteAction("Col_Design", ctx);
            Assert.True(resCol.IsSuccess);

            var resWall = _dispatcher.ExecuteAction("TinhVach", ctx);
            Assert.True(resWall.IsSuccess);
        }

        [Fact]
        public void SlabAction_SlaDesignAndTinhSan_ComputesOrthogonalRebar()
        {
            var ctx = new ExcelActionContext();
            ctx.Parameters["Lx"] = 4.0;
            ctx.Parameters["Ly"] = 5.0;
            ctx.Parameters["DeadLoad"] = 4.0;
            ctx.Parameters["LiveLoad"] = 2.0;
            ctx.Parameters["ThicknessMm"] = 120.0;

            var resSla = _dispatcher.ExecuteAction("Sla_Design", ctx);
            Assert.True(resSla.IsSuccess);

            var resAlias = _dispatcher.ExecuteAction("TinhSan", ctx);
            Assert.True(resAlias.IsSuccess);
        }

        [Fact]
        public void FoundationAction_FouShow_EvaluatesPileCapAndPadFooting()
        {
            var ctxCap = new ExcelActionContext();
            ctxCap.Parameters["FoundationType"] = "PileCap";
            var resCap = _dispatcher.ExecuteAction("Fou_Show", ctxCap);
            Assert.True(resCap.IsSuccess);

            var ctxFoot = new ExcelActionContext();
            ctxFoot.Parameters["FoundationType"] = "PadFooting";
            var resFoot = _dispatcher.ExecuteAction("Fou_Show", ctxFoot);
            Assert.True(resFoot.IsSuccess);

            var resAlias = _dispatcher.ExecuteAction("TinhMongDon", ctxFoot);
            Assert.True(resAlias.IsSuccess);
        }

        [Fact]
        public void RebarCuttingAction_ToHop_OptimizesBarStockDemands()
        {
            var ctx = new ExcelActionContext();
            ctx.Parameters["DiameterMm"] = 20;

            var resToHop = _dispatcher.ExecuteAction("ToHop", ctx);
            Assert.True(resToHop.IsSuccess);

            var resToHop2 = _dispatcher.ExecuteAction("ToHop2", ctx);
            Assert.True(resToHop2.IsSuccess);

            var resGop = _dispatcher.ExecuteAction("GopThepDaiToHop", ctx);
            Assert.True(resGop.IsSuccess);
        }

        [Fact]
        public void MatTienInspection_DetectsWorkbookTypesAndReadsSchedules()
        {
            var wb = new XLWorkbook();
            var wsDam = wb.AddWorksheet("TinhDam");
            wsDam.Cell(4, 2).Value = "D1";
            wsDam.Cell(4, 3).Value = 300;
            wsDam.Cell(4, 4).Value = 600;

            var ctx = new ExcelActionContext { Workbook = wb };

            var resType = _dispatcher.ExecuteAction("MatTienDceExcel.TrangThaiWorkbook", ctx);
            Assert.True(resType.IsSuccess);

            var resDoc = _dispatcher.ExecuteAction("MatTienDceExcel.DocBangDam", ctx);
            Assert.True(resDoc.IsSuccess);
            Assert.Contains("D1", resDoc.Data?.ToString());

            var resCheck = _dispatcher.ExecuteAction("MatTienDceExcel.KiemTraDauVaoDam", ctx);
            Assert.True(resCheck.IsSuccess);
        }

        [Fact]
        public void CadBridgeAndSystemActions_ExecuteSuccessfully()
        {
            var ctx = new ExcelActionContext();

            var resCad = _dispatcher.ExecuteAction("SelectHandle", ctx);
            Assert.True(resCad.IsSuccess);

            var resLine = _dispatcher.ExecuteAction("DrawLine", ctx);
            Assert.True(resLine.IsSuccess);

            var resLogin = _dispatcher.ExecuteAction("DceLogin", ctx);
            Assert.True(resLogin.IsSuccess);

            var resLicense = _dispatcher.ExecuteAction("licenseissuance", ctx);
            Assert.True(resLicense.IsSuccess);
        }
    }
}
