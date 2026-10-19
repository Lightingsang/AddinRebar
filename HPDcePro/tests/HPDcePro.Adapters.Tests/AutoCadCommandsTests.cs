using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HPDcePro.AutoCad.Commands;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class AutoCadCommandsTests
    {
        static AutoCadCommandsTests()
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
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }
                return null;
            };
        }

        [Fact]
        public void AutoCadCommands_RegistersAllLegacyCommands()
        {
            var asm = typeof(BeamCommands).Assembly;
            var commandMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in asm.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (var cad in method.CustomAttributes)
                    {
                        if (cad.AttributeType.Name == "CommandMethodAttribute" &&
                            cad.ConstructorArguments.Count > 0 &&
                            cad.ConstructorArguments[0].Value is string cmdName)
                        {
                            commandMethods.Add(cmdName);
                        }
                    }
                }
            }

            string[] requiredLegacyCommands =
            {
                // Beam commands (30)
                "ShopDrawingBeam", "VeDam_DCE", "VeDamAll_DCE", "VeMCD_DCE", "VeBlockDam2",
                "VeBlockCaoDoMB", "VeBlockTrucDam", "VeBlockChieuDaySan", "VeBlockGiatCaoDoSan",
                "VeBlockCaoDoMC", "VeBlockSection", "ImportKichThuoc", "SuaCKThep",
                "XemLienKetTenCauKien", "BlockBeam_Size", "BlockBeam_Name", "BlockBeam_NameSize",
                "BlockBeam_Hide", "BlockBeam_Width", "DatTenCauKienDam", "GanTrucBlockDam",
                "UpDateDLDam", "XemLienKetThep", "DatTenLineDam2", "GanLienKetThep",
                "XoaLienKetThep", "GanDLLineDam", "XoaDLDataDCE", "XemLienKetMBCK", "XuatDuLieuDam",
                // Rebar & Schedule commands (9)
                "ToiUuCatThep", "TKTSSOLUONGCAUKIEN", "TKTSDUONGKINHTHEP", "tkt_dce", "stk_dce",
                "TongHopThongKe_dce", "MaTTThep", "QuetThongKe_dce", "ChiaKhoangCach",
                // Column commands (8)
                "VeCotDcePro", "VeVachDcePro", "ImportKichThuocCot", "ImportHinhDangCot",
                "UpdateThepTinhToan", "ColumnREC1", "ColumnREC2", "ColumnCi",
                // Slab commands (6)
                "VeThepSanTopX", "VeThepSanTopY", "SoHieuThepSan", "VeThepSanBotX", "VeThepSanBotY", "ThongKeThepSan",
                // Footing & Secondary commands (12)
                "VeBlockCoc", "DanhTenCoc", "XuatToaDoCoc", "StripMong", "TextDaiMong",
                "VeMongCoc_DCE", "VeMongDon_DCE", "TMCoc", "RaiDaiCoc", "VeBlockLanhTo",
                "VeMatCatLanhTo", "VeThang_DCE",
                // AutoStr & ETABS commands (5)
                "AutoStr", "GETFORM", "XuatDuLieuModelEtabs", "UpdateModelEtabs", "taomv"
            };

            Assert.True(requiredLegacyCommands.Length >= 69, "Should verify at least 69 legacy commands.");

            foreach (var cmd in requiredLegacyCommands)
            {
                Assert.True(commandMethods.Contains(cmd), $"Missing legacy command: {cmd}");
            }
        }
    }
}
