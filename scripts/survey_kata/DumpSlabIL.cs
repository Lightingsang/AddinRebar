using System;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace SurveyKata;

class DumpSlabIL
{
    public static void Run()
    {
        string cad2025 = @"C:\kata_pro\Kata_pro64_Cad2025.dll";
        string outDir = @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_slab\decompiled";

        var readerParams = new ReaderParameters { ReadWrite = false, InMemory = true };
        using var asm = AssemblyDefinition.ReadAssembly(cad2025, readerParams);

        var lenhThep = asm.MainModule.Types.FirstOrDefault(t => t.FullName == "Kata_pro64_Cad2025.Lenh_thep");
        if (lenhThep == null) return;

        string[] targets = new[] {
            "Ve_san", "Ve_san_polyline", "Ve_san_4d", "Chia_san1",
            "Trien_khai_day_san", "Khai_thong_tin_san", "Thong_ke_san",
            "Phan_loai_san1", "Chuyen_block_thep_san_sang_theplk1",
            "show_thep_rai1", "Drop_panel1", "Ve_thep_openning1"
        };

        using var sw = new StreamWriter(Path.Combine(outDir, "slab_methods_il_2025.txt"), false, Encoding.UTF8);

        foreach (var mName in targets)
        {
            var m = lenhThep.Methods.FirstOrDefault(x => x.Name.Equals(mName, StringComparison.OrdinalIgnoreCase));
            if (m != null)
            {
                sw.WriteLine($"=== Method: {m.FullName} ===");
                if (m.HasBody)
                {
                    foreach (var ins in m.Body.Instructions)
                    {
                        sw.WriteLine($"  {ins.Offset:X4}: {ins.OpCode} {ins.Operand}");
                    }
                }
                else
                {
                    sw.WriteLine("  [No Body]");
                }
                sw.WriteLine();
            }
        }

        Console.WriteLine("Done dumping 2025 slab IL!");
    }
}
