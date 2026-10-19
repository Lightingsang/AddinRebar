using System;
using System.IO;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace SurveyKata;

class InspectSlabMethods
{
    public static void Run()
    {
        string outDir = @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_slab\decompiled";
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        string cad2013 = @"C:\kata_pro\Kata_pro64_Cad2013.dll";
        string classLib = @"C:\kata_pro\Kata_Class_Lib.dll";
        string revitLib = @"C:\kata_pro\Kata_Class_Lib_Revit.dll";

        var decompiler2013 = new CSharpDecompiler(cad2013, new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false
        });

        // 1. Decompile full slab forms & classes from Cad2013
        string[] typesToDecompile = new[] {
            "Kata_pro64_Cad2013.view_san",
            "Kata_pro64_Cad2013.Form_Rai_Thep_Ke_San",
            "Kata_pro64_Cad2013.Form_Chia_San",
            "Kata_pro64_Cad2013.Form_Phan_Loai_San",
            "Kata_pro64_Cad2013.TK_san",
            "Kata_pro64_Cad2013.Info_slab",
            "Kata_pro64_Cad2013.Input_slab",
            "Kata_pro64_Cad2013.InfoThepCho",
            "Kata_pro64_Cad2013.PhanLoaiSan",
            "Kata_pro64_Cad2013.slab_Kata",
            "Kata_pro64_Cad2013.info_ck_san"
        };

        foreach (var tName in typesToDecompile)
        {
            try
            {
                string shortName = tName.Substring(tName.LastIndexOf('.') + 1);
                string code = decompiler2013.DecompileTypeAsString(new FullTypeName(tName));
                File.WriteAllText(Path.Combine(outDir, $"cad2013_{shortName}.cs"), code, Encoding.UTF8);
                Console.WriteLine($"Decompiled {shortName} from Cad2013");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decompiling {tName}: {ex.Message}");
            }
        }

        // 2. Decompile Lenh_thep slab methods
        string[] methods = new[] {
            "Ve_san", "Ve_san_polyline", "Ve_san_4d", "Chia_san1",
            "Trien_khai_day_san", "Khai_thong_tin_san", "Thong_ke_san",
            "Phan_loai_san1", "Chuyen_block_thep_san_sang_theplk1",
            "show_thep_rai1", "Drop_panel1", "Ve_thep_openning1"
        };

        var lenhThepType = decompiler2013.TypeSystem.FindType(new FullTypeName("Kata_pro64_Cad2013.Lenh_thep"));
        if (lenhThepType != null)
        {
            foreach (var mName in methods)
            {
                try
                {
                    using var sw = new StreamWriter(Path.Combine(outDir, $"cad2013_Lenh_thep_{mName}.cs"), false, Encoding.UTF8);
                    bool found = false;
                    foreach (var method in lenhThepType.GetMethods())
                    {
                        if (method.Name.Equals(mName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            sw.WriteLine($"// Decompiled Kata_pro64_Cad2013.Lenh_thep::{method.Name}");
                            sw.WriteLine(decompiler2013.DecompileAsString(method.MetadataToken));
                        }
                    }
                    if (found)
                        Console.WriteLine($"Decompiled method {mName}");
                    else
                        Console.WriteLine($"Method {mName} not found");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decompiling {mName}: {ex.Message}");
                }
            }
        }

        // 3. Decompile ClassLib types
        try
        {
            var decompilerClassLib = new CSharpDecompiler(classLib, new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false
            });
            string codeSlab3d = decompilerClassLib.DecompileTypeAsString(new FullTypeName("Kata_Class_Lib.Info_Slab3D"));
            File.WriteAllText(Path.Combine(outDir, "classlib_Info_Slab3D.cs"), codeSlab3d, Encoding.UTF8);
            Console.WriteLine("Decompiled Info_Slab3D from Kata_Class_Lib");

            string codePoly = decompilerClassLib.DecompileTypeAsString(new FullTypeName("Kata_Class_Lib.PolygonModule"));
            File.WriteAllText(Path.Combine(outDir, "classlib_PolygonModule.cs"), codePoly, Encoding.UTF8);
            Console.WriteLine("Decompiled PolygonModule from Kata_Class_Lib");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decompiling ClassLib: {ex.Message}");
        }

        // 4. Decompile RevitLib types
        try
        {
            var decompilerRevit = new CSharpDecompiler(revitLib, new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false
            });
            string codeRevitSlab = decompilerRevit.DecompileTypeAsString(new FullTypeName("Kata_Class_Lib_Revit.Bien_chung"));
            File.WriteAllText(Path.Combine(outDir, "revitlib_Bien_chung.cs"), codeRevitSlab, Encoding.UTF8);
            Console.WriteLine("Decompiled Bien_chung from Kata_Class_Lib_Revit");

            string codeRevitRebar = decompilerRevit.DecompileTypeAsString(new FullTypeName("Kata_Class_Lib_Revit.Revit_rebar"));
            File.WriteAllText(Path.Combine(outDir, "revitlib_Revit_rebar.cs"), codeRevitRebar, Encoding.UTF8);
            Console.WriteLine("Decompiled Revit_rebar from Kata_Class_Lib_Revit");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decompiling RevitLib: {ex.Message}");
        }

        Console.WriteLine("Done all slab decompilations!");
    }
}
