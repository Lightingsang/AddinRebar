using System;
using System.IO;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace SurveyKata;

class DecompileHelper
{
    public static void DecompileMethod(string assemblyPath, string typeFullName, string methodName, string outputFile)
    {
        var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false
        });

        var type = decompiler.TypeSystem.FindType(new FullTypeName(typeFullName));
        if (type == null)
        {
            Console.WriteLine($"Type {typeFullName} not found!");
            return;
        }

        using var sw = new StreamWriter(outputFile, false, Encoding.UTF8);
        sw.WriteLine($"// Decompiled {typeFullName}::{methodName}");
        
        bool found = false;
        foreach (var method in type.GetMethods())
        {
            if (method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                sw.WriteLine($"\n// Method: {method.Name}");
                var code = decompiler.DecompileAsString(method.MetadataToken);
                sw.WriteLine(code);
            }
        }

        if (!found)
        {
            sw.WriteLine($"// Method {methodName} not found, dumping all methods in {typeFullName}:");
            foreach (var m in type.GetMethods())
            {
                sw.WriteLine($"//   {m.Name}");
            }
        }

        Console.WriteLine($"Decompiled to {outputFile}");
    }

    public static void DecompileType(string assemblyPath, string typeFullName, string outputFile)
    {
        var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false
        });

        var type = decompiler.TypeSystem.FindType(new FullTypeName(typeFullName));
        if (type == null)
        {
            Console.WriteLine($"Type {typeFullName} not found!");
            return;
        }

        using var sw = new StreamWriter(outputFile, false, Encoding.UTF8);
        sw.WriteLine($"// Decompiled Type: {typeFullName}");
        var code = decompiler.DecompileTypeAsString(new FullTypeName(typeFullName));
        sw.WriteLine(code);
        Console.WriteLine($"Decompiled full type to {outputFile}");
    }
}
