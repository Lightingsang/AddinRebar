using System.Collections.Generic;
using HPKata.AutoCad.Abstractions;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Interop;
using HPKata.AutoCad.Tests.Mocks;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class RebarMarkAuditTests
    {
        [Fact]
        public void CheckDuplicateMarks_WithEmptyOrCleanRecords_ReturnsZeroConflicts()
        {
            var cleanRecords = new List<KataXDataRecord>
            {
                new() { STT = 1, Phi = 20, Dang = "01", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 4000.0 } },
                new() { STT = 2, Phi = 16, Dang = "02", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 3500.0 } },
                new() { STT = 3, Phi = 10, Dang = "22", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 400.0, 500.0 } }
            };

            int count = HandleLinkController.CheckDuplicateMarks(cleanRecords, out List<string> reports);

            Assert.Equal(0, count);
            Assert.Empty(reports);
        }

        [Fact]
        public void CheckDuplicateMarks_WithConflictingSpecifications_DetectsAndReportsConflicts()
        {
            var conflictingRecords = new List<KataXDataRecord>
            {
                new() { STT = 1, Phi = 20, Dang = "01", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 4000.0 } },
                new() { STT = 1, Phi = 25, Dang = "01", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 4000.0 } }, // Diameter mismatch!
                new() { STT = 2, Phi = 16, Dang = "02", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 3500.0 } },
                new() { STT = 2, Phi = 16, Dang = "05", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 3500.0 } }  // Shape mismatch!
            };

            int count = HandleLinkController.CheckDuplicateMarks(conflictingRecords, out List<string> reports);

            Assert.Equal(2, count);
            Assert.Equal(2, reports.Count);
            Assert.Contains(reports, r => r.Contains("Conflicting mark 1") && r.Contains("Ø20") && r.Contains("Ø25"));
            Assert.Contains(reports, r => r.Contains("Conflicting mark 2") && r.Contains("shape '02'") && r.Contains("shape '05'"));
        }

        [Fact]
        public void CheckDuplicateMarks_WithIdenticalDuplicateEntities_ReportsDuplicateCount()
        {
            var duplicateRecords = new List<KataXDataRecord>
            {
                new() { STT = 5, Phi = 18, Dang = "01", TenCauKien = "C1", ChieuDaiCacDoanMm = new[] { 3000.0 } },
                new() { STT = 5, Phi = 18, Dang = "01", TenCauKien = "C1", ChieuDaiCacDoanMm = new[] { 3000.0 } },
                new() { STT = 5, Phi = 18, Dang = "01", TenCauKien = "C1", ChieuDaiCacDoanMm = new[] { 3000.0 } }
            };

            int count = HandleLinkController.CheckDuplicateMarks(duplicateRecords, out List<string> reports);

            Assert.Equal(1, count);
            Assert.Single(reports);
            Assert.Contains("Duplicate mark 5", reports[0]);
            Assert.Contains("3 rebar entities", reports[0]);
        }

        [Fact]
        public void CheckDuplicateMarks_WithMockCadDocumentContext_WritesFormattedMessages()
        {
            var context = new MockCadDocumentContext();
            context.Entities.Add(new CadEntityDto
            {
                Handle = "1A01",
                XDataRecord = new KataXDataRecord { STT = 1, Phi = 20, Dang = "01", ChieuDaiCacDoanMm = new[] { 2500.0 } }
            });
            context.Entities.Add(new CadEntityDto
            {
                Handle = "1A02",
                XDataRecord = new KataXDataRecord { STT = 1, Phi = 16, Dang = "01", ChieuDaiCacDoanMm = new[] { 2500.0 } }
            });

            int count = HandleLinkController.CheckDuplicateMarks(context, out List<string> reports);

            Assert.Equal(1, count);
            Assert.Single(reports);
            Assert.Contains(context.Messages, m => m.Contains("1 duplicate marks detected"));
        }

        [Fact]
        public void AssignMarkNotation_SwitchesActiveModeBetweenSequentialAndSuffix()
        {
            HandleLinkController.AssignMarkNotation(isSecondary: true);
            Assert.Equal(MarkNotationMode.Suffix, HandleLinkController.ActiveMarkNotationMode);

            HandleLinkController.AssignMarkNotation(isSecondary: false);
            Assert.Equal(MarkNotationMode.Sequential, HandleLinkController.ActiveMarkNotationMode);
        }
    }
}
