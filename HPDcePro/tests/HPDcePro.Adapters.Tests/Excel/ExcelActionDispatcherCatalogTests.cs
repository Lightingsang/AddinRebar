using System;
using System.Collections.Generic;
using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Adapters.Tests.Excel
{
    public sealed class ExcelActionDispatcherCatalogTests
    {
        private static readonly string[] s_legacy73Actions =
        {
            // API (3)
            "APIAUTOCAD", "APICAD", "APICSI",
            // Beam (15)
            "Bea_CalculateTheVolume", "Bea_ChooseColor", "Bea_Design", "Bea_ExportDrawingData",
            "Bea_ExportSize", "Bea_LoadData", "Bea_Print", "Bea_Setup",
            "MatTienDceExcel.DocBangDam", "MatTienDceExcel.KiemTraDauVaoDam", "MatTienDceExcel.DocKetQuaDam",
            "MatTienDceExcel.DocDamDaLuuDuLieuVe", "AutoStr.AutoStr_InThuyetMinhDam",
            "AutoStr.AutoStr_AiTinhToanDam", "AutoStr.AutoStr_AiChuyenDuLieuVeDam",
            // Column / Wall (18)
            "Col_CalculateTheVolume", "Col_ChooseColor", "Col_Design", "Col_Design2",
            "Col_ExportDrawingData", "Col_ExportSize", "Col_LoadData", "Col_LoadDataGenCol",
            "Col_Print", "Col_Setup", "Col_SortData",
            "MatTienDceExcel.DocBangCot", "MatTienDceExcel.DocKetQuaCot", "MatTienDceExcel.DocKetQuaVach",
            "MatTienDceExcel.KiemTraDauVaoCot", "AutoStr.AutoStr_InThuyetMinhCot",
            "AutoStr.AutoStr_AiTinhToanCot", "AutoStr.AutoStr_AiXuatDuLieuVeCot",
            // Slab (10)
            "Sla_ChooseColor", "Sla_Design", "Sla_ExportSize", "Sla_LoadData", "Sla_Print", "Sla_Setup",
            "MatTienDceExcel.DocBangSan", "MatTienDceExcel.KiemTraDauVaoSan",
            "AutoStr.AutoStr_ChonThepSan", "AutoStr.AutoStr_InThuyetMinhSan",
            // Foundation (3)
            "Bas_Show", "Eta_Show", "Fou_Show",
            // Rebar Cutting (3)
            "GopThepDaiToHop", "ToHop", "ToHop2",
            // CAD Bridge (4)
            "DrawLine", "DrawPolyLine", "DrawREC", "SelectHandle",
            // ETABS / SAFE (7)
            "ESStiffnessModifiers", "ESUniformLoadSetsToSafe", "EtaSTableToEta",
            "PointSafeToPointSafe", "SafeTableToSafe", "UniformLoadSets", "DbimPuFile",
            // System & Mat Tien Meta (10)
            "DceAI", "DceAdmin", "DceLanguage", "DceLogin", "DceLogout", "DceUpdate", "licenseissuance",
            "MatTienDceExcel.PhienBan", "MatTienDceExcel.LietKeChucNang", "MatTienDceExcel.TrangThaiWorkbook"
        };

        [Fact]
        public void Dispatcher_RegistersExactly73Actions()
        {
            var dispatcher = new DceExcelActionDispatcher();
            var actions = dispatcher.GetSupportedActions();

            Assert.Equal(73, actions.Count);
            Assert.Equal(73, s_legacy73Actions.Length);
        }

        [Fact]
        public void Dispatcher_EveryLegacyActionFromCatalog_CanExecute()
        {
            var dispatcher = new DceExcelActionDispatcher();
            foreach (var action in s_legacy73Actions)
            {
                Assert.True(dispatcher.CanExecute(action), $"Action '{action}' must be recognized by CanExecute.");
                Assert.True(dispatcher.CanHandle(action), $"Action '{action}' must be recognized by CanHandle.");
            }
        }

        [Fact]
        public void Dispatcher_UnknownAction_ReturnsNotFound()
        {
            var dispatcher = new DceExcelActionDispatcher();
            var ctx = new ExcelActionContext();
            var res = dispatcher.ExecuteAction("ActionNonExistent_FooBar", ctx);

            Assert.False(res.IsSuccess);
            Assert.False(res.Success);
            Assert.Contains("not supported", res.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Dispatcher_NullOrEmptyActionKey_ReturnsFail()
        {
            var dispatcher = new DceExcelActionDispatcher();
            var ctx = new ExcelActionContext();

            var resNull = dispatcher.ExecuteAction(null!, ctx);
            Assert.False(resNull.IsSuccess);

            var resEmpty = dispatcher.ExecuteAction("   ", ctx);
            Assert.False(resEmpty.IsSuccess);
        }

        [Fact]
        public void Dispatcher_Aliases_CanExecuteSuccessfully()
        {
            var dispatcher = new DceExcelActionDispatcher();
            string[] aliases =
            {
                "TinhDam", "TinhCot", "TinhVach", "TinhSan", "TinhMong",
                "TinhDaiCoc", "TinhMongDon", "ToHopThep", "ToiUuCatThep", "XuatBOQ", "InThuyetMinh"
            };

            foreach (var alias in aliases)
            {
                Assert.True(dispatcher.CanExecute(alias), $"Alias '{alias}' must be executable.");
            }
        }

        [Fact]
        public void Dispatcher_NullContext_ThrowsArgumentNullException()
        {
            var dispatcher = new DceExcelActionDispatcher();
            Assert.Throws<ArgumentNullException>(() => dispatcher.ExecuteAction("Bea_Design", null!));
        }
    }
}
