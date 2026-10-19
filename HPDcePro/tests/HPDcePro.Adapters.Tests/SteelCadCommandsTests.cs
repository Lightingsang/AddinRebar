using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using HPDcePro.AutoCad.Commands;
using HPDcePro.UI.Common;
using HPDcePro.UI.Views;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class SteelCadCommandsTests
    {
        static SteelCadCommandsTests()
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

        private static void RunOnStaThread(Action action)
        {
            System.Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (System.Exception ex)
                {
                    // Reason: Capture STA thread exception to propagate back to test runner
                    caught = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught != null)
            {
                throw new InvalidOperationException($"STA thread failed: {caught.Message}", caught);
            }
        }

        [Fact]
        public void SteelCadCommands_RegistersAllTenSteelCommands()
        {
            var commandMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var method in typeof(SteelCadCommands).GetMethods(BindingFlags.Public | BindingFlags.Instance))
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

            string[] expectedCommands =
            {
                "DrawElevationMiddleFrameBT", "DrawElevationMiddleFrame3", "DrawElevationMiddleFrame3BT",
                "DrawElevationMiddleFrame5", "DrawElevationMiddleFrame5a", "DrawElevationVDS1",
                "DrawPlanPulirnRoof", "DrawPlanPurlinMonitor", "DetailAnchorBolt", "SteelConnectionDialog"
            };

            foreach (var cmd in expectedCommands)
            {
                Assert.Contains(cmd, commandMethods);
            }
        }

        [Fact]
        public void AutoStrCommands_RegistersGetformCommand()
        {
            var getform = typeof(AutoStrCommands)
                .GetMethod("GETFORM", BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(getform);
            string? cmdName = null;
            foreach (var cad in getform.CustomAttributes)
            {
                if (cad.AttributeType.Name == "CommandMethodAttribute" &&
                    cad.ConstructorArguments.Count > 0 &&
                    cad.ConstructorArguments[0].Value is string name)
                {
                    cmdName = name;
                    break;
                }
            }
            Assert.Equal("GETFORM", cmdName);
        }

        [Fact]
        public void SteelCadCommands_ElevationCommands_ExecuteWithoutException()
        {
            var cmds = new SteelCadCommands();

            var ex = Record.Exception(() =>
            {
                cmds.DrawElevationMiddleFrameBT();
                cmds.DrawElevationMiddleFrame3();
                cmds.DrawElevationMiddleFrame3BT();
                cmds.DrawElevationMiddleFrame5();
                cmds.DrawElevationMiddleFrame5a();
                cmds.DrawElevationVDS1();
            });

            Assert.Null(ex);
        }

        [Fact]
        public void SteelCadCommands_PurlinAndAnchorBoltCommands_ExecuteWithoutException()
        {
            var cmds = new SteelCadCommands();

            var ex = Record.Exception(() =>
            {
                cmds.DrawPlanPulirnRoof();
                cmds.DrawPlanPurlinMonitor();
                cmds.DetailAnchorBolt();
            });

            Assert.Null(ex);
        }

        [Fact]
        public void SteelConnectionDialog_TargetWindowInstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new SteelConnectionWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }

        [Fact]
        public void AutoStrCommands_GETFORM_TargetWindowInstantiatesOnStaThread()
        {
            RunOnStaThread(() =>
            {
                ThemeManager.EnsureAssembliesLoaded();
                var window = new MainControlCenterWindow();
                Assert.NotNull(window);
                Assert.NotNull(window.ViewModel);

                ThemeManager.Apply(window, dark: true);
                ThemeManager.Apply(window, dark: false);
            });
        }
    }
}
