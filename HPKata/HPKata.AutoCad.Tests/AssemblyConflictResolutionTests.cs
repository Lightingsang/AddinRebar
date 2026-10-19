using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using HPKata.AutoCad.Interop;
using HPKata.UI.Common;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public sealed class AssemblyConflictResolutionTests
    {
        [Fact]
        public void KataCadAssemblyResolver_WhenDefaultAlcHasOlderVersion820_LoadsVersion840IntoIsolatedAlcWithout0x80131621()
        {
            KataCadAssemblyResolver.Initialize();
            KataUiAssemblyResolver.Initialize();

            string tempRoot = Path.Combine(Path.GetTempPath(), "HPKata_AlcConflict_" + Guid.NewGuid().ToString("N"));
            string vidbeDir = Path.Combine(tempRoot, "AutocadVidbeCoding");
            string hpKataDir = Path.Combine(tempRoot, "HPKata");
            Directory.CreateDirectory(vidbeDir);
            Directory.CreateDirectory(hpKataDir);

            string baseDirV84Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SyntheticMvvmConflictLib.dll");
            try
            {
                const string sharedAsmName = "SyntheticMvvmConflictLib";
                string v82Path = Path.Combine(vidbeDir, sharedAsmName + ".dll");
                string v84Path = Path.Combine(hpKataDir, sharedAsmName + ".dll");

                EmitMinimalDll(v82Path, sharedAsmName, new Version(8, 2, 0, 0));
                EmitMinimalDll(v84Path, sharedAsmName, new Version(8, 4, 0, 0));
                File.Copy(v84Path, baseDirV84Path, true);

                // 1. Simulate AutocadVidbeCoding loading v8.2.0.0 into AssemblyLoadContext.Default first via Assembly.LoadFrom
                Assembly loaded82 = Assembly.LoadFrom(v82Path);
                Assert.Equal(new Version(8, 2, 0, 0), loaded82.GetName().Version);
                Assert.Same(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(loaded82));

                // 2. Confirm that naive Default ALC load of v8.4.0.0 throws FileLoadException (0x80131621)
                Assert.Throws<FileLoadException>(() =>
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(v84Path));

                // 3. Exercise the REAL CoreCLR native Assembly.Load binder for Version=8.4.0.0 while v8.2.0.0 is in Default ALC!
                var requested84 = new AssemblyName($"{sharedAsmName}, Version=8.4.0.0, Culture=neutral, PublicKeyToken=null");
                Assembly boundViaClr = Assembly.Load(requested84);

                Assert.NotNull(boundViaClr);
                Assert.Equal(new Version(8, 4, 0, 0), boundViaClr.GetName().Version);
                Assert.NotSame(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(boundViaClr));
            }
            finally
            {
                try
                {
                    if (File.Exists(baseDirV84Path)) File.Delete(baseDirV84Path);
                    if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
                }
                catch (Exception)
                {
                    // Annotated catch per Rule Q-B2: ignore locked temp DLL cleanup
                }
            }
        }

        [Fact]
        public void KataCadAssemblyResolver_ResolvesCommunityToolkitMvvm840AndBeamDetailingViewModel()
        {
            KataCadAssemblyResolver.Initialize();
            KataUiAssemblyResolver.Initialize();

            var mvvmName = new AssemblyName("CommunityToolkit.Mvvm, Version=8.4.0.0, Culture=neutral, PublicKeyToken=4aff67a105548ee2");
            Assembly? mvvmAsm = KataCadAssemblyResolver.ResolveAssembly(mvvmName);

            Assert.NotNull(mvvmAsm);
            Assert.True(mvvmAsm!.GetName().Version >= new Version(8, 4, 0, 0));

            var vm = new BeamDetailingViewModel { IsShopMode = false };
            Assert.True(vm.Spans.Count > 0);
            vm.ApplyCommand.Execute(null);
            Assert.True(vm.IsConfirmed);
            Assert.NotNull(vm.ResultLayout);
        }

        private static void EmitMinimalDll(string outputPath, string simpleName, Version version)
        {
            // Copy a tiny valid .NET assembly and rewrite its AssemblyDef version + name in metadata,
            // or use PersistedAssemblyBuilder / template assembly bytes.
            // Since HPKata.Core.dll is a valid .NET 8 assembly, we can create a minimal PE using Roslyn or metadata patching.
            byte[] sourceBytes = File.ReadAllBytes(typeof(KataUiAssemblyResolver).Assembly.Location);
            // Patch the ASCII string "HPKata.UI\0" -> simpleName is not same length; instead compile a 1-line C# DLL or patch version.
            // Wait: we have Microsoft.CodeAnalysis.CSharp or we can patch a fixed-length name!
            WriteMinimalManagedPe(outputPath, simpleName, version);
        }

        private static void WriteMinimalManagedPe(string outputPath, string simpleName, Version version)
        {
            // Use System.Reflection.Metadata.Ecma335.MetadataBuilder + ManagedPEBuilder (built into .NET 8 BCL!)
            var metadataBuilder = new System.Reflection.Metadata.Ecma335.MetadataBuilder();
            metadataBuilder.AddModule(
                0,
                metadataBuilder.GetOrAddString(simpleName + ".dll"),
                metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
                default,
                default);

            metadataBuilder.AddAssembly(
                metadataBuilder.GetOrAddString(simpleName),
                version,
                default,
                default,
                0,
                System.Reflection.AssemblyHashAlgorithm.None);

            metadataBuilder.AddTypeDefinition(
                default,
                default,
                metadataBuilder.GetOrAddString("<Module>"),
                default,
                System.Reflection.Metadata.Ecma335.MetadataTokens.FieldDefinitionHandle(1),
                System.Reflection.Metadata.Ecma335.MetadataTokens.MethodDefinitionHandle(1));

            var peHeaderBuilder = new System.Reflection.PortableExecutable.PEHeaderBuilder(
                imageCharacteristics: System.Reflection.PortableExecutable.Characteristics.ExecutableImage |
                                      System.Reflection.PortableExecutable.Characteristics.Dll);

            var peBuilder = new System.Reflection.PortableExecutable.ManagedPEBuilder(
                peHeaderBuilder,
                new System.Reflection.Metadata.Ecma335.MetadataRootBuilder(metadataBuilder),
                new System.Reflection.Metadata.BlobBuilder());

            var peBlob = new System.Reflection.Metadata.BlobBuilder();
            peBuilder.Serialize(peBlob);

            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            peBlob.WriteContentTo(fs);
        }
    }
}
