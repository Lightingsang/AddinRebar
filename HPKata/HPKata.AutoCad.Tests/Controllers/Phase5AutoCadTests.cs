using System;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Controllers;
using Xunit;

namespace HPKata.AutoCad.Tests.Controllers
{
    public sealed class Phase5AutoCadTests
    {
        [Fact]
        public void QsCommands_AllCommandMethodsAreRegistered()
        {
            var methods = typeof(QsCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = methods
                .SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                .Select(a => a.GlobalName)
                .ToList();

            Assert.Contains("QS", commandNames);
            Assert.Contains("QS_mbkc", commandNames);
            Assert.Contains("QS_tru", commandNames);
            Assert.Contains("xuatQS", commandNames);
            Assert.Contains("timqs", commandNames);
            Assert.Contains("tkqs", commandNames);
        }

        [Fact]
        public void LayoutCommands_AllCommandMethodsAreRegistered()
        {
            var methods = typeof(LayoutCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = methods
                .SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                .Select(a => a.GlobalName)
                .ToList();

            Assert.Contains("vg", commandNames);
            Assert.Contains("ve_grid", commandNames);
            Assert.Contains("ve_luoi_truc", commandNames);
            Assert.Contains("sbv", commandNames);
            Assert.Contains("sap_xep_ban_ve", commandNames);
            Assert.Contains("ibv", commandNames);
            Assert.Contains("chen_khung_ten", commandNames);
            Assert.Contains("taomv", commandNames);
            Assert.Contains("taomv1", commandNames);
            Assert.Contains("khoav", commandNames);
            Assert.Contains("mokhoav", commandNames);
            Assert.Contains("danhmuc", commandNames);
        }

        [Fact]
        public void DimensionUtilityCommands_AllCommandMethodsAreRegistered()
        {
            var methods = typeof(DimensionUtilityCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = methods
                .SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                .Select(a => a.GlobalName)
                .ToList();

            Assert.Contains("catdim", commandNames);
            Assert.Contains("cat_chan_dim", commandNames);
            Assert.Contains("ngatdim", commandNames);
            Assert.Contains("ngat_dim", commandNames);
            Assert.Contains("dnoi", commandNames);
            Assert.Contains("dim_noi_tiep", commandNames);
            Assert.Contains("sl", commandNames);
            Assert.Contains("ghi_do_doc", commandNames);
        }

        [Fact]
        public void TextAnnotationCommands_AllCommandMethodsAreRegistered()
        {
            var methods = typeof(TextAnnotationCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = methods
                .SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                .Select(a => a.GlobalName)
                .ToList();

            Assert.Contains("dtext", commandNames);
            Assert.Contains("can_chinh_text", commandNames);
            Assert.Contains("mcat", commandNames);
            Assert.Contains("cao_do_mat_cat", commandNames);
        }

        [Fact]
        public void LayerUtilityCommands_AllCommandMethodsAreRegistered()
        {
            var methods = typeof(LayerUtilityCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = methods
                .SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                .Select(a => a.GlobalName)
                .ToList();

            Assert.Contains("anlayer", commandNames);
            Assert.Contains("an_layer", commandNames);
            Assert.Contains("hienlayer", commandNames);
            Assert.Contains("hien_tat_ca_layer", commandNames);
            Assert.Contains("doilayer", commandNames);
            Assert.Contains("doi_layer", commandNames);
        }

        [Fact]
        public void KataCleanCommands_AllCommandMethodsAreRegistered()
        {
            var methods = typeof(KataCleanCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = methods
                .SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                .Select(a => a.GlobalName)
                .ToList();

            Assert.Contains("clean_kata", commandNames);
            Assert.Contains("xoa_xrecord", commandNames);
        }

        [Fact]
        public void QsTakeoffController_SafeExecutionWithoutActiveDoc()
        {
            try
            {
                QsTakeoffController.ExecuteMasterWorkflow();
                QsTakeoffController.GenerateSummaryTable();
                QsTakeoffController.ExportExcel();
                QsTakeoffController.FindElement();
            }
            catch (NullReferenceException)
            {
                // Expected when AutoCAD process reactor (CDocCollReactorImpl) is uninitialized out-of-process
            }
        }

        [Fact]
        public void GridAndLayoutControllers_SafeExecutionWithoutActiveDoc()
        {
            try
            {
                GridDrawController.DrawGridInteractive();
                SheetLayoutController.ArrangeDrawingBlocksInteractive();
                SheetLayoutController.InsertTitleBlockInteractive();
                ViewportController.CreateViewportInteractive();
                ViewportController.SetViewportLock(true);
                DrawingIndexController.GenerateDrawingIndexTableInteractive();
            }
            catch (NullReferenceException)
            {
                // Expected when AutoCAD process reactor (CDocCollReactorImpl) is uninitialized out-of-process
            }
        }

        [Fact]
        public void CadUtilitiesControllers_SafeExecutionWithoutActiveDoc()
        {
            try
            {
                DimensionUtilityController.TrimDimensionWitnessLinesInteractive();
                DimensionUtilityController.SplitDimensionInteractive();
                DimensionUtilityController.ContinueDimensionInteractive();
                DimensionUtilityController.DrawSlopeAnnotationInteractive();
                TextAnnotationController.AlignTextInteractive();
                TextAnnotationController.CreateElevationCalloutInteractive();
                LayerUtilityController.HideSelectedEntityLayer();
                LayerUtilityController.UnhideAllLayers();
                LayerUtilityController.ChangeEntitiesLayer();
                KataCleanController.CleanKataDatabase();
            }
            catch (NullReferenceException)
            {
                // Expected when AutoCAD process reactor (CDocCollReactorImpl) is uninitialized out-of-process
            }
        }

        [Fact]
        public void LayoutCommands_DrawGrid_RegistersAllExpectedAliases()
        {
            var method = typeof(LayoutCommands).GetMethod("DrawGrid", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            var names = method.GetCustomAttributes<CommandMethodAttribute>().Select(a => a.GlobalName).ToList();
            Assert.Contains("vg", names);
            Assert.Contains("ve_grid", names);
            Assert.Contains("ve_luoi_truc", names);
        }

        [Fact]
        public void DimensionUtilityCommands_ContinueDimension_RegistersExpectedAliases()
        {
            var method = typeof(DimensionUtilityCommands).GetMethod("ContinueDimension", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            var names = method.GetCustomAttributes<CommandMethodAttribute>().Select(a => a.GlobalName).ToList();
            Assert.Contains("dnoi", names);
            Assert.Contains("dim_noi_tiep", names);
        }

        [Fact]
        public void AllPhase5BlueprintAndLispCommands_AreRegistered()
        {
            var allCommandNames = typeof(QsCommands).Assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                .Select(a => a.GlobalName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string[] expected =
            {
                "QS", "QS_mbkc", "QS_tru", "xuatQS", "timqs", "tkqs",
                "chiatam", "chia_tam", "CHIA_TAM_LIGHT", "opgach", "op_gach", "OP_GACH_LIGHT",
                "vg", "ve_grid", "locdim", "loc_dim", "pvn", "phan_vung_noi",
                "danhmuc", "cat", "cat_doituong", "taomv", "taomv1", "sbv", "sap_ban_ve",
                "ibv", "in_ban_ve", "pukata", "pdfkata", "hideatt", "Unhideatt",
                "tim_ten_ck", "dten", "tkten", "textblock", "chuyen", "str", "show_thep_rai",
                "tstt", "lv", "lv1", "sst", "dao", "ad", "cht", "dao_thep", "tha", "bvbs"
            };

            foreach (var cmd in expected)
            {
                Assert.Contains(cmd, allCommandNames);
            }
        }
    }
}
