using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Commands;
using HPDcePro.UI.Common;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    [Collection("WpfTests")]
    public sealed class Subsystem1AllCommandsParityTests
    {
        [Fact]
        public void All_311_AutoCad_Commands_Are_Registered_With_CommandMethodAttribute()
        {
            var asm = typeof(DceLenhBoTroCommands).Assembly;
            var registeredCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in asm.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var attrs = method.GetCustomAttributes<CommandMethodAttribute>(false);
                    foreach (var attr in attrs)
                    {
                        if (!string.IsNullOrWhiteSpace(attr.GlobalName))
                        {
                            registeredCommands.Add(attr.GlobalName);
                        }
                    }
                }
            }

            Assert.True(registeredCommands.Count >= 311, $"Expected >= 311 registered AutoCAD commands, found {registeredCommands.Count}");
            Assert.Contains("2DXCLIP", registeredCommands);
            Assert.Contains("2MODEL", registeredCommands);
            Assert.Contains("Dai135C_dce", registeredCommands);
            Assert.Contains("ChonThepX_DCE", registeredCommands);
            Assert.Contains("ThongKeThepBlock_DCE", registeredCommands);
            Assert.Contains("ChiaThep_DCE", registeredCommands);
            Assert.Contains("CopyKhoangRaiThep", registeredCommands);
            Assert.Contains("FixLoiMirrorTag_DCE", registeredCommands);
            Assert.Contains("MBKCAUDIT_DCE", registeredCommands);
            Assert.Contains("VEMONGTHANGMAY_DCE", registeredCommands);
            Assert.Contains("DceMayHoc_Train", registeredCommands);
            Assert.Contains("DBIMAIKIEM_DCE", registeredCommands);
        }

        [Fact]
        public void DceLenhBoTroCommands_All_73_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new DceLenhBoTroCommands(), 73);
        }

        [Fact]
        public void TktScheduleCommands_All_19_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new TktScheduleCommands(), 19);
        }

        [Fact]
        public void ShopBeamCommands_All_19_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new ShopBeamCommands(), 19);
        }

        [Fact]
        public void RebarXDataAndLinkCommands_All_22_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new RebarXDataAndLinkCommands(), 22);
        }

        [Fact]
        public void SlabDetailingCommands_All_23_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new SlabDetailingCommands(), 23);
        }

        [Fact]
        public void BeamAndFramingCommands_All_22_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new BeamAndFramingCommands(), 22);
        }

        [Fact]
        public void ColumnWallFoundationCommands_All_19_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new ColumnWallFoundationCommands(), 19);
        }

        [Fact]
        public void LayoutAndDrawingCommands_All_20_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new LayoutAndDrawingCommands(), 20);
        }

        [Fact]
        public void SystemAndIntegrationCommands_All_18_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new SystemAndIntegrationCommands(), 18);
        }

        [Fact]
        public void AiAndDiagCommands_All_12_Endpoints_Execute_Cleanly()
        {
            InvokeAllCommandMethods(() => new AiAndDiagCommands(), 12);
        }

        private static void InvokeAllCommandMethods(Func<object> factory, int expectedCount)
        {
            System.Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    ThemeManager.EnsureAssembliesLoaded();
                    var instance = factory();
                    var methods = instance.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(m => m.GetCustomAttributes<CommandMethodAttribute>(false).Any())
                        .ToList();

                    Assert.Equal(expectedCount, methods.Count);
                    foreach (var method in methods)
                    {
                        var ex = Record.Exception(() => method.Invoke(instance, Array.Empty<object>()));
                        Assert.Null(ex);
                    }
                }
                catch (System.Exception ex)
                {
                    // Reason: Propagate STA thread exception back to xUnit runner.
                    caught = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(caught);
        }
    }
}
