namespace HPBim.Core.Tests.Coordination;

using System;
using System.IO;
using System.Threading.Tasks;
using HPBim.Core.Coordination;
using Xunit;

public sealed class BimCoordinationLedgerServiceTests
{
    [Fact]
    public void Initialize_ValidProjectName_SetsInitialLedgerState()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Dự Án Mẫu");

        var ledger = service.Ledger;
        Assert.Equal("Dự Án Mẫu", ledger.Project);
        Assert.Equal("PHAN_MONG", ledger.ActivePhase);
        Assert.Equal(PhaseStatus.InProgress, ledger.PhaseStatus);
        Assert.Empty(ledger.Elements);
        Assert.Empty(ledger.Rfis);
        Assert.Equal(0, ledger.Summary.TotalElements);
    }

    [Fact]
    public void RegisterElement_ValidElement_RegistersAndUpdatesSummary()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Dự Án Mẫu");

        var el = new CoordinationElementRecord
        {
            Id = "PILE-01",
            Category = CoordinationCategory.Pile,
            Code = "D400-01",
            Step = CoordinationLifecycleStep.Step1_ReadCad,
            TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
        };

        service.RegisterElement(el);

        var ledger = service.Ledger;
        Assert.Single(ledger.Elements);
        Assert.Equal(1, ledger.Summary.TotalElements);
        Assert.Equal(1, ledger.Summary.Step1_ReadCad);
        Assert.Equal(1, ledger.Summary.Group1_ReadyToModel);
    }

    [Fact]
    public void ClassifyElement_UpdatesTechnicalGroupCorrectly()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Dự Án Mẫu");

        var el = new CoordinationElementRecord
        {
            Id = "FT-01",
            Category = CoordinationCategory.Footing,
            Code = "ĐC-1",
            Step = CoordinationLifecycleStep.Step1_ReadCad,
            TechnicalGroup = ElementTechnicalGroup.Group3_MissingInfo
        };

        service.RegisterElement(el);
        Assert.Equal(1, service.Ledger.Summary.Group3_MissingInfo);

        service.ClassifyElement("FT-01", ElementTechnicalGroup.Group1_ReadyToModel, "CAD_Agent", "Info completed");
        Assert.Equal(1, service.Ledger.Summary.Group1_ReadyToModel);
        Assert.Equal(0, service.Ledger.Summary.Group3_MissingInfo);
    }

    [Fact]
    public void UpdateRevitTargetAndRecordAudit_UpdatesElementProperties()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Dự Án Mẫu");

        var el = new CoordinationElementRecord
        {
            Id = "COL-01",
            Category = CoordinationCategory.Column,
            Code = "C1",
            Step = CoordinationLifecycleStep.Step1_ReadCad,
            TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
        };

        service.RegisterElement(el);

        var revitTarget = new RevitTargetSpec
        {
            ElementId = 98765,
            TypeName = "C_220x220",
            TwoPhaseSelfCheckPassed = true
        };
        service.UpdateRevitTarget("COL-01", revitTarget);

        var audit = new AuditRecord
        {
            Status = AuditStatus.Dung,
            AuditStep = 4,
            DeltaDimensionMm = 0.0
        };
        service.RecordAudit("COL-01", audit);

        var updated = service.Ledger.Elements[0];
        Assert.NotNull(updated.RevitTargetSpec);
        Assert.Equal(98765, updated.RevitTargetSpec.ElementId);
        Assert.NotNull(updated.AuditRecord);
        Assert.Equal(AuditStatus.Dung, updated.AuditRecord.Status);
    }

    [Fact]
    public void CreateRfi_GeneratesSequentialRfiRecord()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Dự Án Mẫu");

        var rfi1 = service.CreateRfi("Sai cao độ", "Mâu thuẫn cao độ đài", new[] { "FT-01" }, "Kiểm tra lại mặt cắt");
        var rfi2 = service.CreateRfi("Thiếu tiết diện", "Không có b x h", new[] { "GB-05" }, "Làm rõ với thiết kế");

        Assert.Equal("RFI-001", rfi1.RfiId);
        Assert.Equal("RFI-002", rfi2.RfiId);
        Assert.Equal(2, service.Ledger.Rfis.Count);
    }

    [Fact]
    public async Task SaveAndLoadFromFile_RoundTripsLedgerState()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("File IO Test");

        var el = new CoordinationElementRecord
        {
            Id = "EL-TEST",
            Category = CoordinationCategory.Slab,
            Code = "S1",
            Step = CoordinationLifecycleStep.Step1_ReadCad,
            TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
        };
        service.RegisterElement(el);

        string tempPath = Path.Combine(Path.GetTempPath(), $"ledger_test_{Guid.NewGuid():N}.json");
        try
        {
            await service.SaveToFileAsync(tempPath);
            Assert.True(File.Exists(tempPath));

            var service2 = new BimCoordinationLedgerService();
            await service2.LoadFromFileAsync(tempPath);

            Assert.Equal("File IO Test", service2.Ledger.Project);
            Assert.Single(service2.Ledger.Elements);
            Assert.Equal("EL-TEST", service2.Ledger.Elements[0].Id);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task LoadFromFile_HydratesDualKeyMappingsAndElementFallbacks()
    {
        var service1 = new BimCoordinationLedgerService();
        service1.Initialize("Hydration Test");

        var key1 = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1");
        service1.RegisterMapping(LedgerElementMapping.Create(key1, "uid-c1", 1001L, CoordinationCategory.Column, "C1", "hash1"));

        var el2 = new CoordinationElementRecord
        {
            Id = "COL-02",
            Category = CoordinationCategory.Column,
            Code = "C2",
            CadSourceSpec = new CadSourceSpec { DwgSheet = "DocA", Handle = "H2" },
            RevitTargetSpec = new RevitTargetSpec { UniqueId = "uid-c2", ElementId = 1002L }
        };
        service1.RegisterElement(el2);

        string tempPath = Path.Combine(Path.GetTempPath(), $"hydration_test_{Guid.NewGuid():N}.json");
        try
        {
            await service1.SaveToFileAsync(tempPath);

            var service2 = new BimCoordinationLedgerService();
            await service2.LoadFromFileAsync(tempPath);

            Assert.True(service2.TryGetTarget(key1, out var uid1));
            Assert.Equal("uid-c1", uid1);
            Assert.True(service2.Mappings.ContainsKey(key1.ToString()));

            var key2 = new CompositeSourceKey("DocA", CoordinationCategory.Column, "H2");
            Assert.True(service2.TryGetTarget(key2, out var uid2));
            Assert.Equal("uid-c2", uid2);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
