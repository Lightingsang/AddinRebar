using System;
using System.IO;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace SurveyKata;

class InspectBeamMethods
{
    public static void Run()
    {
        string outDir = @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_beam_spec";
        string cad2013 = @"C:\kata_pro\Kata_pro64_Cad2013.dll";
        string classLib = @"C:\kata_pro\Kata_Class_Lib.dll";

        var decompiler2013 = new CSharpDecompiler(cad2013, new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false
        });

        // 1. Decompile full Set_Rebar_Beam
        try 
        {
            string code = decompiler2013.DecompileTypeAsString(new FullTypeName("Kata_pro64_Cad2013.Set_Rebar_Beam"));
            File.WriteAllText(Path.Combine(outDir, "decomp_Set_Rebar_Beam.cs"), code, Encoding.UTF8);
            Console.WriteLine("Decompiled Set_Rebar_Beam");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error decompiling Set_Rebar_Beam: " + ex.Message);
        }

        // 2. Decompile Lenh_thep beam methods
        string[] methods = new[] {
            "Vedam", "Vedam_shop", "Load_beam", "Ve_dam_giao1", "Xu_ly_giao_dam",
            "Xu_ly_cat_dam", "Ve_dam_mc21", "Ve_dam_xien1", "Vedamcong",
            "Vedammong", "Vegiangmong", "Vegiangmong_shop", "quet_dam",
            "Save_thep_dam", "Add_thep_dam", "show_thep_dam", "Etabs_Model",
            "Tao_table_tk_cau_kien_block_beam1"
        };

        var lenhThepType = decompiler2013.TypeSystem.FindType(new FullTypeName("Kata_pro64_Cad2013.Lenh_thep"));
        if (lenhThepType != null)
        {
            foreach (var mName in methods)
            {
                try
                {
                    using var sw = new StreamWriter(Path.Combine(outDir, $"decomp_Lenh_thep_{mName}.cs"), false, Encoding.UTF8);
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
    }
}
