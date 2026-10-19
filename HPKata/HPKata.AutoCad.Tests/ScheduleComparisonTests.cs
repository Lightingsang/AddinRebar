using System.Collections.Generic;
using HPKata.AutoCad.Abstractions;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Tests.Mocks;
using HPKata.Core.Models;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class ScheduleComparisonTests
    {
        [Fact]
        public void CompareRebarItems_WithIdenticalSchedules_ReportsZeroDifferences()
        {
            var baseItems = new List<RebarItem>
            {
                new() { ComponentMark = "D1", BarMark = "1", DiameterMm = 20, CountPerMember = 4, MemberCount = 2, CuttingLengthMm = 3500.0 },
                new() { ComponentMark = "D1", BarMark = "2", DiameterMm = 10, CountPerMember = 20, MemberCount = 2, CuttingLengthMm = 1800.0 }
            };

            var currentItems = new List<RebarItem>
            {
                new() { ComponentMark = "D1", BarMark = "1", DiameterMm = 20, CountPerMember = 4, MemberCount = 2, CuttingLengthMm = 3500.0 },
                new() { ComponentMark = "D1", BarMark = "2", DiameterMm = 10, CountPerMember = 20, MemberCount = 2, CuttingLengthMm = 1800.0 }
            };

            ScheduleComparisonReport report = SummaryCommandRunner.CompareRebarItems(baseItems, currentItems);

            Assert.False(report.HasDifferences);
            Assert.Empty(report.Differences);
            Assert.Equal(0.0, report.WeightDifferenceKg, precision: 2);
            Assert.NotNull(report.ComparisonTable);
            Assert.Equal(3, report.ComparisonTable.Rows.Count); // 2 item rows + 1 total row
        }

        [Fact]
        public void CompareRebarItems_WithModifiedAddedAndRemovedItems_DetectsAllVariances()
        {
            var baseItems = new List<RebarItem>
            {
                new() { ComponentMark = "D1", BarMark = "1", DiameterMm = 20, CountPerMember = 4, MemberCount = 1, CuttingLengthMm = 3000.0 },
                new() { ComponentMark = "D1", BarMark = "2", DiameterMm = 16, CountPerMember = 2, MemberCount = 1, CuttingLengthMm = 2500.0 }
            };

            var currentItems = new List<RebarItem>
            {
                // Mark 1 modified (quantity changed 4 -> 6)
                new() { ComponentMark = "D1", BarMark = "1", DiameterMm = 20, CountPerMember = 6, MemberCount = 1, CuttingLengthMm = 3000.0 },
                // Mark 2 removed
                // Mark 3 added
                new() { ComponentMark = "D1", BarMark = "3", DiameterMm = 25, CountPerMember = 2, MemberCount = 1, CuttingLengthMm = 4000.0 }
            };

            ScheduleComparisonReport report = SummaryCommandRunner.CompareRebarItems(baseItems, currentItems);

            Assert.True(report.HasDifferences);
            Assert.Equal(3, report.Differences.Count);
            Assert.Contains(report.Differences, d => d.Contains("Modified mark 1"));
            Assert.Contains(report.Differences, d => d.Contains("Removed mark 2"));
            Assert.Contains(report.Differences, d => d.Contains("Added mark 3"));
            Assert.True(report.WeightDifferenceKg > 0.0);
        }

        [Fact]
        public void ExecuteCompareSchedule_WithMockCadContext_InsertsTableAndWritesReport()
        {
            var context = new MockCadDocumentContext();

            // All entities (baseline)
            context.Entities.Add(new CadEntityDto
            {
                Handle = "B01",
                XDataRecord = new KataXDataRecord { STT = 1, Phi = 20, SoLuong = "4", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 3000.0 } }
            });
            context.Entities.Add(new CadEntityDto
            {
                Handle = "B02",
                XDataRecord = new KataXDataRecord { STT = 2, Phi = 16, SoLuong = "2", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 2500.0 } }
            });

            // Selected entity (current revision)
            context.SelectedEntities.Add(new CadEntityDto
            {
                Handle = "B01",
                XDataRecord = new KataXDataRecord { STT = 1, Phi = 20, SoLuong = "6", TenCauKien = "D1", ChieuDaiCacDoanMm = new[] { 3000.0 } }
            });

            ScheduleComparisonReport report = SummaryCommandRunner.ExecuteCompareSchedule(context);

            Assert.True(report.HasDifferences);
            Assert.Single(context.InsertedTables);
            Assert.Equal("BẢNG ĐỐI SOÁT SO SÁNH THỐNG KÊ (SSTK)", context.InsertedTables[0].Table.Title);
            Assert.Contains(context.Messages, m => m.Contains("So sánh bảng thống kê (sstk)"));
        }
    }
}
