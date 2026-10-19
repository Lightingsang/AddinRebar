using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDcePro.Revit.Commands;
using Xunit;

namespace HPDcePro.Adapters.Tests.Revit
{
    public sealed class RevitCommandExecutionContextTests
    {
        [Fact]
        public void ModelingCommands_NullCommandData_ReturnsCancelledAndDiagnosticMessage()
        {
            IExternalCommand[] commands =
            {
                new CommandAddBeam(),
                new CommandAddColumn(),
                new CommandAddGrid(),
                new CommandAddSlab(),
                new CommandAddWall(),
                new CommandDatTenDam(),
                new CommandCreateAnalytical()
            };

            foreach (var cmd in commands)
            {
                string message = string.Empty;
                var res = cmd.Execute(null!, ref message, null!);
                Assert.Equal(Result.Cancelled, res);
                Assert.False(string.IsNullOrWhiteSpace(message), $"{cmd.GetType().Name} must provide a diagnostic message.");
            }
        }

        [Fact]
        public void JoinCutCommands_NullCommandData_ReturnsCancelledAndDiagnosticMessage()
        {
            IExternalCommand[] commands =
            {
                new CommandAutoJoin(),
                new CommandAutoCut(),
                new CommandBatchUnjoin(),
                new CommandBeamDisallowJoin(),
                new CommandCreateCope(),
                new CommandJoinByOrder(),
                new DceBeamBreaks()
            };

            foreach (var cmd in commands)
            {
                string message = string.Empty;
                var res = cmd.Execute(null!, ref message, null!);
                Assert.Equal(Result.Cancelled, res);
                Assert.False(string.IsNullOrWhiteSpace(message), $"{cmd.GetType().Name} must provide a diagnostic message.");
            }
        }

        [Fact]
        public void RebarAndTakeoffCommands_NullCommandData_ReturnsCancelledAndDiagnosticMessage()
        {
            IExternalCommand[] commands =
            {
                new CommandAddRebarBeam(),
                new CommandAddRebarColumn(),
                new CommandAddRebarWall(),
                new CommandAddRebarSlab(),
                new CommandAddRebarFooting(),
                new CommandXuatKhoiLuong()
            };

            foreach (var cmd in commands)
            {
                string message = string.Empty;
                var res = cmd.Execute(null!, ref message, null!);
                Assert.Equal(Result.Cancelled, res);
                Assert.False(string.IsNullOrWhiteSpace(message), $"{cmd.GetType().Name} must provide a diagnostic message.");
            }
        }

        [Fact]
        public void SettingsAndExchangeCommands_NullCommandData_ReturnsCancelledAndDiagnosticMessage()
        {
            IExternalCommand[] commands =
            {
                new CommandLogin(),
                new CommandLogout(),
                new CommandDceSetup(),
                new CommandExportBeamToExcel(),
                new CommandExportToAutocad(),
                new CommandEtabsToRevit(),
                new CommandExportToEtabs()
            };

            foreach (var cmd in commands)
            {
                string message = string.Empty;
                var res = cmd.Execute(null!, ref message, null!);
                Assert.Equal(Result.Cancelled, res);
                Assert.False(string.IsNullOrWhiteSpace(message), $"{cmd.GetType().Name} must provide a diagnostic message.");
            }
        }
    }
}
