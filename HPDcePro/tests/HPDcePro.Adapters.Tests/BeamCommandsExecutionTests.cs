using System;
using System.IO;
using System.Reflection;
using HPDcePro.AutoCad.Commands;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class BeamCommandsExecutionTests
    {
        static BeamCommandsExecutionTests()
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
        public void VeDam_DCE_ExecutesGracefullyOutsideCadSession()
        {
            var cmds = new BeamCommands();
            var ex = Record.Exception(() => cmds.VeDam_DCE());
            Assert.Null(ex);
        }

        [Fact]
        public void VeDamAll_DCE_ExecutesGracefullyOutsideCadSession()
        {
            var cmds = new BeamCommands();
            var ex = Record.Exception(() => cmds.VeDamAll_DCE());
            Assert.Null(ex);
        }

        [Fact]
        public void VeMCD_DCE_ExecutesGracefullyOutsideCadSession()
        {
            var cmds = new BeamCommands();
            var ex = Record.Exception(() => cmds.VeMCD_DCE());
            Assert.Null(ex);
        }

        [Fact]
        public void VeBlockDam2_ExecutesGracefullyOutsideCadSession()
        {
            var cmds = new BeamCommands();
            var ex = Record.Exception(() => cmds.VeBlockDam2());
            Assert.Null(ex);
        }
    }
}
