using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using HPDcePro.Core.Common;
using HPDcePro.Revit;
using HPDcePro.Revit.Commands;
using Xunit;

namespace HPDcePro.Adapters.Tests.Revit
{
    public sealed class RevitCommandsReflectionTests
    {
        private static readonly Type[] s_structuralCommands =
        {
            typeof(CommandLogin),
            typeof(CommandLogout),
            typeof(CommandAddBeam),
            typeof(CommandAddColumn),
            typeof(CommandAddGrid),
            typeof(CommandAddSlab),
            typeof(CommandAddWall),
            typeof(CommandAutoCut),
            typeof(CommandAutoJoin),
            typeof(CommandBatchUnjoin),
            typeof(CommandBeamDisallowJoin),
            typeof(CommandCreateCope),
            typeof(CommandJoinByOrder),
            typeof(DceBeamBreaks),
            typeof(CommandDatTenDam),
            typeof(CommandDceSetup),
            typeof(CommandExportBeamToExcel),
            typeof(CommandExportToAutocad),
            typeof(CommandCreateAnalytical),
            typeof(CommandEtabsToRevit),
            typeof(CommandExportToEtabs),
            typeof(CommandXuatKhoiLuong),
            typeof(CommandAddRebarBeam)
        };

        [Fact]
        public void RevitCommands_All23Commands_ImplementIExternalCommand()
        {
            Assert.Equal(23, s_structuralCommands.Length);
            foreach (var type in s_structuralCommands)
            {
                Assert.True(typeof(IExternalCommand).IsAssignableFrom(type), $"{type.Name} must implement IExternalCommand.");
            }
        }

        [Fact]
        public void RevitCommands_All23Commands_HaveTransactionAndRegenerationAttributes()
        {
            foreach (var type in s_structuralCommands)
            {
                var transAttr = type.GetCustomAttribute<TransactionAttribute>();
                Assert.NotNull(transAttr);
                Assert.Equal(TransactionMode.Manual, transAttr.Mode);

                var regenAttr = type.GetCustomAttribute<RegenerationAttribute>();
                Assert.NotNull(regenAttr);
                Assert.Equal(RegenerationOption.Manual, regenAttr.Option);
            }
        }

        [Fact]
        public void RevitCommands_All23Commands_HavePublicParameterlessConstructor()
        {
            foreach (var type in s_structuralCommands)
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                Assert.NotNull(ctor);
                Assert.True(ctor.IsPublic, $"{type.Name} must have a public parameterless constructor.");
            }
        }

        [Fact]
        public void RevitApplication_DefinesCorrectRibbonTabName()
        {
            Assert.Equal("DCE PRO", DceProRevitApplication.TabName);
        }

        [Fact]
        public void RevitApplication_HasStartupAndShutdownMethods()
        {
            var appType = typeof(DceProRevitApplication);
            Assert.True(typeof(IExternalApplication).IsAssignableFrom(appType));
            Assert.NotNull(appType.GetMethod("OnStartup"));
            Assert.NotNull(appType.GetMethod("OnShutdown"));
        }

        [Fact]
        public void RevitRibbonIcons_AllMappedAssetsExistOnDisk()
        {
            string[] iconAssets =
            {
                "IconRevit/Gird.png",
                "IconRevit/Column.png",
                "IconRevit/Beam.png",
                "IconRevit/Floor.png",
                "IconRevit/Wall.png",
                "IconRevit/Name.png",
                "IconCad/Calculator.png",
                "IconRevit/Join.png",
                "IconCad/Cut.png",
                "IconRevit/DeleteLink.png",
                "IconCad/Repair1.png",
                "IconCad/Repair2.png",
                "IconCad/Repair3.png",
                "IconRevit/Rebar.png",
                "IconCad/Rebarlayout.png",
                "IconCad/Spread.png",
                "IconCad/Drawing.png",
                "IconCad/HouSe.png",
                "IconRevit/ThongKe.png",
                "IconCad/AutoStr_32.png",
                "IconRevit/ExportToExcel.png",
                "IconRevit/Autocad.png",
                "IconRevit/Etabs.png",
                "IconCad/ETABS.png",
                "IconCad/Setup.png",
                "IconCad/Login.png",
                "IconCad/Logout.png"
            };

            foreach (var icon in iconAssets)
            {
                string fullPath = AssetPathResolver.ResolveIcon(icon);
                Assert.True(File.Exists(fullPath), $"Icon asset '{icon}' must exist at '{fullPath}'.");
            }
        }

        [Fact]
        public void CompanionRebarCommands_ImplementIExternalCommand()
        {
            Type[] companionRebar =
            {
                typeof(CommandAddRebarColumn),
                typeof(CommandAddRebarWall),
                typeof(CommandAddRebarSlab),
                typeof(CommandAddRebarFooting)
            };

            foreach (var type in companionRebar)
            {
                Assert.True(typeof(IExternalCommand).IsAssignableFrom(type));
                Assert.NotNull(type.GetCustomAttribute<TransactionAttribute>());
                Assert.NotNull(type.GetCustomAttribute<RegenerationAttribute>());
            }
        }
    }
}
