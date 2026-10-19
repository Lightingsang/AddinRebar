using System;
using System.IO;
using System.Reflection;
using HPDcePro.AutoCad.Commands;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class AutoCadFullCommandsTests
    {
        static AutoCadFullCommandsTests()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string asmName = new AssemblyName(args.Name).Name ?? string.Empty;
                string nugetBase = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".nuget", "packages");

                string[] candidates =
                {
                    Path.Combine(nugetBase, "autocad.net.core", "25.1.0", "lib", "net8.0", $"{asmName}.dll"),
                    Path.Combine(nugetBase, "autocad.net.model", "25.1.0", "lib", "net8.0", $"{asmName}.dll"),
                    Path.Combine(nugetBase, "autocad.net", "25.1.0", "lib", "net8.0", $"{asmName}.dll"),
                    Path.Combine(@"C:\Program Files\Autodesk\AutoCAD 2026", $"{asmName}.dll")
                };

                foreach (var path in candidates)
                {
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
        }

        [Fact]
        public void BeamCommands_ExecuteRealDraftingAndXDataWorkflows_WithoutException()
        {
            var cmds = new BeamCommands();

            var ex = Record.Exception(() =>
            {
                cmds.VeBlockDam2();
                cmds.ImportKichThuoc();
                cmds.XemLienKetTenCauKien();
                cmds.BlockBeam_Size();
                cmds.BlockBeam_Name();
                cmds.BlockBeam_NameSize();
                cmds.BlockBeam_Hide();
                cmds.BlockBeam_Width();
                cmds.DatTenCauKienDam();
                cmds.GanTrucBlockDam();
                cmds.UpDateDLDam();
                cmds.XemLienKetThep();
                cmds.DatTenLineDam2();
                cmds.GanLienKetThep();
                cmds.XoaLienKetThep();
                cmds.GanDLLineDam();
                cmds.XoaDLDataDCE();
                cmds.XemLienKetMBCK();
                cmds.XuatDuLieuDam();
            });

            Assert.Null(ex);
        }

        [Fact]
        public void RebarCommands_ExecuteScheduleAndDistributionWorkflows_WithoutException()
        {
            var cmds = new RebarCommands();

            var ex = Record.Exception(() =>
            {
                cmds.TKTSSOLUONGCAUKIEN();
                cmds.TKTSDUONGKINHTHEP();
                cmds.tkt_dce();
                cmds.TongHopThongKe_dce();
                cmds.MaTTThep();
                cmds.QuetThongKe_dce();
                cmds.ChiaKhoangCach();
            });

            Assert.Null(ex);
        }

        [Fact]
        public void ColumnAndWallCommands_ExecuteDetailingWorkflows_WithoutException()
        {
            var cmds = new ColumnCommands();

            var ex = Record.Exception(() =>
            {
                cmds.VeVachDcePro();
                cmds.ColumnREC1();
                cmds.ColumnREC2();
                cmds.ColumnCi();
                cmds.ImportKichThuocCot();
                cmds.ImportHinhDangCot();
                cmds.UpdateThepTinhToan();
            });

            Assert.Null(ex);
        }

        [Fact]
        public void FootingAndSecondaryCommands_ExecuteFoundationsAndStairs_WithoutException()
        {
            var cmds = new FootingCommands();

            var ex = Record.Exception(() =>
            {
                cmds.VeBlockCoc();
                cmds.DanhTenCoc();
                cmds.XuatToaDoCoc();
                cmds.StripMong();
                cmds.TextDaiMong();
                cmds.VeMongCoc_DCE();
                cmds.VeMongDon_DCE();
                cmds.TMCoc();
                cmds.RaiDaiCoc();
                cmds.VeBlockLanhTo();
                cmds.VeMatCatLanhTo();
                cmds.VeThang_DCE();
            });

            Assert.Null(ex);
        }

        [Fact]
        public void AutoStrCommands_ExecutePipelineAndDraftingWorkflows_WithoutException()
        {
            var cmds = new AutoStrCommands();

            var ex = Record.Exception(() =>
            {
                cmds.XuatDuLieuModelEtabs();
                cmds.UpdateModelEtabs();
                cmds.taomv();
            });

            Assert.Null(ex);
        }
    }
}
