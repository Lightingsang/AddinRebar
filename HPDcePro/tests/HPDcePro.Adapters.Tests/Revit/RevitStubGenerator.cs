using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HPDcePro.Adapters.Tests.Revit
{
    internal static class RevitStubGenerator
    {
        private static Assembly? s_revitApi;
        private static Assembly? s_revitApiUi;
        private static readonly object s_sync = new();

        internal static Assembly? ResolveRevitAssembly(string asmName, AssemblyLoadContext context)
        {
            lock (s_sync)
            {
                if (string.Equals(asmName, "RevitAPI", StringComparison.OrdinalIgnoreCase))
                {
                    return s_revitApi ??= CompileRevitApiStub(context);
                }
                if (string.Equals(asmName, "RevitAPIUI", StringComparison.OrdinalIgnoreCase))
                {
                    return s_revitApiUi ??= CompileRevitApiUiStub(context);
                }
                return null;
            }
        }

        private static Assembly? CompileRevitApiStub(AssemblyLoadContext context)
        {
            string code = @"
namespace Autodesk.Revit.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TransactionAttribute : System.Attribute
    {
        public TransactionAttribute(TransactionMode mode) { Mode = mode; }
        public TransactionMode Mode { get; }
    }
    public enum TransactionMode { Manual, ReadOnly }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class RegenerationAttribute : System.Attribute
    {
        public RegenerationAttribute(RegenerationOption option) { Option = option; }
        public RegenerationOption Option { get; }
    }
    public enum RegenerationOption { Manual }
}
namespace Autodesk.Revit.DB
{
    public enum TransactionStatus
    {
        Uninitialized = 0,
        Started = 1,
        RolledBack = 2,
        Committed = 3,
        Pending = 4,
        Error = 5
    }
    public class Document {}
    public class ElementSet {}
    public class Transaction : System.IDisposable
    {
        public Transaction(Document doc, string name) {}
        public TransactionStatus Start() => TransactionStatus.Started;
        public TransactionStatus Commit() => TransactionStatus.Committed;
        public TransactionStatus RollBack() => TransactionStatus.RolledBack;
        public void Dispose() {}
    }
}";
            string path = Path.Combine(Path.GetTempPath(), $"HPDcePro_RevitAPI_{Guid.NewGuid():N}", "RevitAPI.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var refs = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.IDisposable).Assembly.Location)
            };
            return EmitAndLoad(code, "RevitAPI", path, refs, context);
        }

        private static Assembly? CompileRevitApiUiStub(AssemblyLoadContext context)
        {
            var revitApi = s_revitApi ??= CompileRevitApiStub(context);
            if (revitApi == null) return null;

            string code = @"
namespace Autodesk.Revit.UI
{
    public enum TaskDialogResult
    {
        None = 0,
        Ok = 1,
        Cancel = 2,
        Retry = 4,
        Yes = 6,
        No = 7,
        Close = 8
    }
    public static class TaskDialog
    {
        public static TaskDialogResult Show(string title, string mainInstruction) => TaskDialogResult.Ok;
    }
    public class UIApplication
    {
        public UIDocument ActiveUIDocument { get; set; }
    }
    public class UIDocument
    {
        public Autodesk.Revit.DB.Document Document { get; set; }
    }
    public class ExternalCommandData
    {
        public UIApplication Application { get; set; }
    }
    public interface IExternalCommand
    {
        Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements);
    }
    public interface IExternalApplication
    {
        Result OnStartup(UIControlledApplication application);
        Result OnShutdown(UIControlledApplication application);
    }
    public enum Result { Succeeded, Failed, Cancelled }
    public class UIControlledApplication
    {
        public void CreateRibbonTab(string tabName) {}
        public RibbonPanel CreateRibbonPanel(string tabName, string panelName) => new RibbonPanel();
    }
    public class RibbonPanel
    {
        public RibbonItem AddItem(RibbonItemData itemData) => null;
    }
    public abstract class RibbonItem {}
    public abstract class RibbonItemData {}
    public class PushButtonData : RibbonItemData
    {
        public PushButtonData(string name, string text, string assemblyName, string className) {}
        public System.Windows.Media.ImageSource LargeImage { get; set; }
        public System.Windows.Media.ImageSource Image { get; set; }
        public string ToolTip { get; set; }
    }
}";
            string path = Path.Combine(Path.GetTempPath(), $"HPDcePro_RevitAPIUI_{Guid.NewGuid():N}", "RevitAPIUI.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var refs = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(revitApi.Location),
                MetadataReference.CreateFromFile(typeof(System.Windows.Media.ImageSource).Assembly.Location)
            };
            return EmitAndLoad(code, "RevitAPIUI", path, refs, context);
        }

        private static Assembly? EmitAndLoad(string code, string asmName, string path, MetadataReference[] refs, AssemblyLoadContext context)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var comp = CSharpCompilation.Create(
                asmName,
                new[] { syntaxTree },
                refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var emitRes = comp.Emit(path);
            if (!emitRes.Success) return null;
            return context.LoadFromAssemblyPath(path);
        }
    }
}
