using System;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace SurveyKata;

class DumpRevitLib
{
    public static void Run()
    {
        string revitDll = @"C:\kata_pro\Kata_Class_Lib_Revit.dll";
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(@"C:\kata_pro");
        resolver.AddSearchDirectory(@"C:\Program Files\Autodesk\Revit 2026");

        var readerParams = new ReaderParameters 
        { 
            ReadWrite = false, 
            InMemory = true,
            AssemblyResolver = resolver
        };

        using var asm = AssemblyDefinition.ReadAssembly(revitDll, readerParams);
        using var sw = new StreamWriter(@"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\revit_lib_members.txt", false, Encoding.UTF8);

        sw.WriteLine($"Assembly: {asm.FullName}");
        foreach (var type in asm.MainModule.Types)
        {
            DumpType(type, sw, "");
        }
        Console.WriteLine("Done dumping Kata_Class_Lib_Revit.dll!");
    }

    static void DumpType(TypeDefinition t, StreamWriter sw, string indent)
    {
        sw.WriteLine($"{indent}TYPE: {t.FullName} (Base: {t.BaseType?.FullName}, Interface: {t.IsInterface})");
        foreach (var f in t.Fields)
        {
            sw.WriteLine($"{indent}  FIELD: {f.FieldType.FullName} {f.Name} (Static={f.IsStatic}, Public={f.IsPublic})");
        }
        foreach (var p in t.Properties)
        {
            sw.WriteLine($"{indent}  PROP: {p.PropertyType.FullName} {p.Name}");
        }
        foreach (var m in t.Methods)
        {
            string pars = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.FullName} {p.Name}"));
            sw.WriteLine($"{indent}  METHOD: {m.ReturnType.FullName} {m.Name}({pars}) (Static={m.IsStatic}, Public={m.IsPublic})");
        }
        foreach (var nested in t.NestedTypes)
        {
            DumpType(nested, sw, indent + "  ");
        }
    }
}
