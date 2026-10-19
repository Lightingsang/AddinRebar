using System;
using System.Collections.Generic;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using Xunit;

namespace HPKata.Core.Tests.DrawingLayout
{
    public class DrawingIndexAggregatorTests
    {
        [Fact]
        public void BuildTable_NaturalSorting_OrdersNumbersCorrectly()
        {
            var items = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-10", "Mặt bằng mái"),
                new DrawingIndexItem(2, "KC-01", "Ghi chú chung"),
                new DrawingIndexItem(3, "KC-02", "Mặt bằng móng"),
                new DrawingIndexItem(4, "KC-09", "Chi tiết thang")
            };

            var table = DrawingIndexAggregator.BuildTable(items);

            Assert.Equal(4, table.TotalSheets);
            Assert.Equal("KC-01", table.Rows[0][1]);
            Assert.Equal("KC-02", table.Rows[1][1]);
            Assert.Equal("KC-09", table.Rows[2][1]);
            Assert.Equal("KC-10", table.Rows[3][1]);
        }

        [Fact]
        public void BuildTable_DuplicateSheetNumbers_ReportsWarning()
        {
            var items = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-01", "Bản vẽ 1"),
                new DrawingIndexItem(2, "KC-01", "Bản vẽ 1 trùng")
            };

            var table = DrawingIndexAggregator.BuildTable(items);
            Assert.Contains(table.Warnings, w => w.Contains("Trùng số hiệu bản vẽ"));
        }

        [Fact]
        public void BuildTable_SequenceGaps_ReportsWarning()
        {
            var items = new List<DrawingIndexItem>
            {
                new DrawingIndexItem(1, "KC-01", "Bản vẽ 1"),
                new DrawingIndexItem(2, "KC-05", "Bản vẽ 5") // Gap: 02, 03, 04 missing
            };

            var table = DrawingIndexAggregator.BuildTable(items);
            Assert.Contains(table.Warnings, w => w.Contains("Gián đoạn"));
        }

        [Fact]
        public void BuildTable_NullItems_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => DrawingIndexAggregator.BuildTable(null!));
        }
    }
}
