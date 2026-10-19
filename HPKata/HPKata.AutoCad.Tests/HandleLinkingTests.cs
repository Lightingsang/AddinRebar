using System;
using System.Collections.Generic;
using HPKata.AutoCad.Abstractions;
using HPKata.AutoCad.Interop;
using HPKata.AutoCad.Tests.Mocks;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class HandleLinkingTests
    {
        [Fact]
        public void HandleLink_RecordLinking_EstablishesBidirectionalHandles()
        {
            var barRecord = new KataXDataRecord { Dang = "01", Phi = 16, STT = 1 };
            var tagRecord = new KataXDataRecord { Dang = "01", Phi = 16, STT = 1 };

            string barHandle = "4A1F";
            string tagHandle = "4A20";

            HandleLinkService.LinkRecords(barRecord, barHandle, tagRecord, tagHandle);

            Assert.Equal(tagHandle, barRecord.LinkedHandle);
            Assert.Equal("THEP_DON", barRecord.LinkType);
            Assert.Equal(barHandle, tagRecord.LinkedHandle);
            Assert.Equal("THEP_DON", tagRecord.LinkType);
        }

        [Fact]
        public void HandleLink_SeverLink_ClearsBothHandles()
        {
            var barRecord = new KataXDataRecord
            {
                LinkedHandle = "4A20",
                LinkType = "THEP_DON"
            };
            var tagRecord = new KataXDataRecord
            {
                LinkedHandle = "4A1F",
                LinkType = "THEP_DON"
            };

            HandleLinkService.SeverRecordLink(barRecord);
            HandleLinkService.SeverRecordLink(tagRecord);

            Assert.Empty(barRecord.LinkedHandle);
            Assert.Empty(barRecord.LinkType);
            Assert.Empty(tagRecord.LinkedHandle);
            Assert.Empty(tagRecord.LinkType);
        }

        [Fact]
        public void HandleLink_CloneRebinding_RemapsOldHandlesToNewHandles()
        {
            string oldBarHandle = "10A";
            string oldTagHandle = "10B";

            string newBarHandle = "20A";
            string newTagHandle = "20B";

            var oldToNewMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [oldBarHandle] = newBarHandle,
                [oldTagHandle] = newTagHandle
            };

            var clonedBarRecord = new KataXDataRecord { LinkedHandle = oldTagHandle };
            var clonedTagRecord = new KataXDataRecord { LinkedHandle = oldBarHandle };

            HandleLinkService.RebindRecordHandles(clonedBarRecord, oldToNewMap);
            HandleLinkService.RebindRecordHandles(clonedTagRecord, oldToNewMap);

            Assert.Equal(newTagHandle, clonedBarRecord.LinkedHandle);
            Assert.Equal(newBarHandle, clonedTagRecord.LinkedHandle);
        }

        [Fact]
        public void HandleLink_AttributeFormatting_GeneratesStandardCalloutStrings()
        {
            var record = new KataXDataRecord
            {
                SoLuong = "8",
                Phi = 20,
                KhoangCach = "a150",
                STT = 3
            };

            string combined = $"{record.SoLuong}Ø{record.Phi}{record.KhoangCach}".Trim();

            Assert.Equal("8Ø20a150", combined);
            Assert.Equal("3", record.STT.ToString());
            Assert.Equal("20", record.Phi.ToString());
        }

        [Fact]
        public void HandleLink_MockCadContext_UpdatesAttributesAndHighlights()
        {
            var mockContext = new MockCadDocumentContext();
            string barHandle = "4A1F";
            string tagHandle = "4A20";

            mockContext.Entities.Add(new CadEntityDto
            {
                Handle = barHandle,
                EntityType = "Polyline",
                Layer = "kata_thep chu",
                XDataRecord = new KataXDataRecord { LinkedHandle = tagHandle, Phi = 20, SoLuong = "4" }
            });

            mockContext.Entities.Add(new CadEntityDto
            {
                Handle = tagHandle,
                EntityType = "BlockReference",
                Layer = "kata_thep chu",
                XDataRecord = new KataXDataRecord { LinkedHandle = barHandle, Phi = 20, SoLuong = "4" }
            });

            var attrs = new Dictionary<string, string>
            {
                ["SH"] = "1",
                ["DK"] = "20",
                ["DKKC"] = "4Ø20a200"
            };
            mockContext.UpdateBlockAttributes(tagHandle, attrs);
            mockContext.HighlightEntity(barHandle);

            Assert.True(mockContext.UpdatedAttributes.ContainsKey(tagHandle));
            Assert.Equal("4Ø20a200", mockContext.UpdatedAttributes[tagHandle]["DKKC"]);
            Assert.Contains(barHandle, mockContext.HighlightedHandles);
        }
    }
}
