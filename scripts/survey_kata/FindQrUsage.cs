using System;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace SurveyKata;

class FindQrUsage
{
    public static void Run()
    {
        string[] dlls = Directory.GetFiles(@"C:\kata_pro", "*.dll");
        using var sw = new StreamWriter(@"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\qrcoder_usage.txt", false, Encoding.UTF8);

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(@"C:\kata_pro");
        resolver.AddSearchDirectory(@"C:\Program Files\Autodesk\AutoCAD 2026");

        var readerParams = new ReaderParameters 
        { 
            ReadWrite = false, 
            InMemory = true,
            AssemblyResolver = resolver
        };

        foreach (var dll in dlls)
        {
            string fileName = Path.GetFileName(dll);
            if (fileName.Equals("Security.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("QRCoder.dll", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var asm = AssemblyDefinition.ReadAssembly(dll, readerParams);
                foreach (var type in asm.MainModule.Types)
                {
                    CheckType(type, sw, fileName);
                }
            }
            catch {}
        }
        Console.WriteLine("Done scanning all DLLs for QRCoder!");
    }

    static void CheckType(TypeDefinition t, StreamWriter sw, string fileName)
    {
        foreach (var m in t.Methods)
        {
            if (!m.HasBody) continue;
            foreach (var instr in m.Body.Instructions)
            {
                if (instr.Operand is MethodReference mr)
                {
                    if (mr.DeclaringType.FullName.Contains("QRCode") || mr.DeclaringType.FullName.Contains("QRCoder"))
                    {
                        sw.WriteLine($"[{fileName}] {t.FullName}::{m.Name} calls {mr.DeclaringType.FullName}::{mr.Name}");
                    }
                }
                else if (instr.Operand is TypeReference tr)
                {
                    if (tr.FullName.Contains("QRCode") || tr.FullName.Contains("QRCoder"))
                    {
                        sw.WriteLine($"[{fileName}] {t.FullName}::{m.Name} references {tr.FullName}");
                    }
                }
            }
        }
        foreach (var nested in t.NestedTypes) CheckType(nested, sw, fileName);
    }
}
