using System;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Controllers;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Slabs.Models;
using HPKata.Core.Stairs.Models;
using Xunit;

namespace HPKata.AutoCad.Tests.Controllers
{
    public sealed class Phase4AutoCadTests
    {
        [Fact]
        public void FoundationTakeoffRegistry_RegisterAndRetrieve()
        {
            FoundationTakeoffRegistry.Clear();

            var layout = new FoundationRebarLayout
            {
                FoundationMark = "M-TEST",
                Footing = new IsolatedFootingModel { FootingMark = "M-TEST", LengthMm = 1500.0, WidthMm = 1500.0 }
            };

            FoundationTakeoffRegistry.Register(layout);

            Assert.True(FoundationTakeoffRegistry.TryGet("M-TEST", out var found));
            Assert.NotNull(found);
            Assert.Equal("M-TEST", found.FoundationMark);

            var all = FoundationTakeoffRegistry.GetAll();
            Assert.Single(all);

            FoundationTakeoffRegistry.Clear();
            Assert.Empty(FoundationTakeoffRegistry.GetAll());
        }

        [Fact]
        public void SlabTakeoffRegistry_RegisterAndRetrieve()
        {
            SlabTakeoffRegistry.Clear();

            var layout = new SlabRebarLayout
            {
                PanelMark = "S-TEST",
                Panel = new SlabPanelModel { PanelMark = "S-TEST", SpanXMm = 4000.0, SpanYMm = 5000.0 }
            };

            SlabTakeoffRegistry.Register(layout);

            Assert.True(SlabTakeoffRegistry.TryGet("S-TEST", out var found));
            Assert.NotNull(found);
            Assert.Equal("S-TEST", found.PanelMark);

            var all = SlabTakeoffRegistry.GetAll();
            Assert.Single(all);

            SlabTakeoffRegistry.Clear();
            Assert.Empty(SlabTakeoffRegistry.GetAll());
        }

        [Fact]
        public void StairTakeoffRegistry_RegisterAndRetrieve()
        {
            StairTakeoffRegistry.Clear();

            var flight = new StairFlightModel { FlightMark = "ST-TEST", RiserCount = 11 };
            var stairModel = new StairReinforcementModel { Flight = flight };
            StairTakeoffRegistry.RegisterStair(stairModel);

            var allStairs = StairTakeoffRegistry.GetAllStairs();
            Assert.Single(allStairs);

            var lintel = new LintelModel { LintelMark = "LT-TEST" };
            StairTakeoffRegistry.RegisterLintel(lintel);

            var allLintels = StairTakeoffRegistry.GetAllLintels();
            Assert.Single(allLintels);

            StairTakeoffRegistry.Clear();
            Assert.Empty(StairTakeoffRegistry.GetAllStairs());
            Assert.Empty(StairTakeoffRegistry.GetAllLintels());
        }

        [Fact]
        public void FoundationCommands_DeclareExpectedCommandMethods()
        {
            var methods = typeof(FoundationCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in methods)
            {
                var attrs = m.GetCustomAttributes<CommandMethodAttribute>();
                foreach (var attr in attrs)
                {
                    commandNames.Add(attr.GlobalName);
                }
            }

            Assert.Contains("vmbm", commandNames);
            Assert.Contains("rm", commandNames);
            Assert.Contains("vmd", commandNames);
            Assert.Contains("vmc", commandNames);
            Assert.Contains("vmcg", commandNames);
            Assert.Contains("phamong", commandNames);
            Assert.Contains("tkmong", commandNames);
        }

        [Fact]
        public void SlabCommands_DeclareExpectedCommandMethods()
        {
            var methods = typeof(SlabCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in methods)
            {
                var attrs = m.GetCustomAttributes<CommandMethodAttribute>();
                foreach (var attr in attrs)
                {
                    commandNames.Add(attr.GlobalName);
                }
            }

            Assert.Contains("vesan", commandNames);
            Assert.Contains("vesanpl", commandNames);
            Assert.Contains("vesan4d", commandNames);
            Assert.Contains("ssan", commandNames);
            Assert.Contains("dsan", commandNames);
            Assert.Contains("chiasan", commandNames);
            Assert.Contains("tksan", commandNames);
        }

        [Fact]
        public void StairLintelCommands_DeclareExpectedCommandMethods()
        {
            var methods = typeof(StairLintelCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var commandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in methods)
            {
                var attrs = m.GetCustomAttributes<CommandMethodAttribute>();
                foreach (var attr in attrs)
                {
                    commandNames.Add(attr.GlobalName);
                }
            }

            Assert.Contains("vethang", commandNames);
            Assert.Contains("velanhto", commandNames);
            Assert.Contains("vemc", commandNames);
        }
    }
}
