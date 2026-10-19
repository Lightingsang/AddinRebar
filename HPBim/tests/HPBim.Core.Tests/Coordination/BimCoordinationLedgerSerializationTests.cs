namespace HPBim.Core.Tests.Coordination;

using System;
using System.IO;
using System.Text.Json;
using HPBim.Core.Coordination;
using Xunit;

public sealed class BimCoordinationLedgerSerializationTests
{
    [Fact]
    public void SerializeAndDeserialize_CompleteLedger_RoundTripsAccurately()
    {
        var original = new BimCoordinationLedger
        {
            Project = "Test Project KC-90",
            UpdatedAt = new DateTime(2026, 10, 7, 3, 0, 0, DateTimeKind.Utc),
            PrimeDirective = "Khi chua du co so de ket luan, hay dung lai.",
            ActivePhase = "PHAN_MONG",
            PhaseStatus = PhaseStatus.InProgress,
            QualityGate = "GATE_1_MODEL_CLEANUP",
            Tolerances = new CoordinationTolerances
            {
                SectionDimensionMm = 0.0,
                ElevationMm = 1.0,
                PlanPositionMm = 1.0,
                PileCenterMm = 2.0
            },
            Summary = new CoordinationProgressSummary
            {
                TotalElements = 1,
                Step1_ReadCad = 1,
                Group1_ReadyToModel = 1
            },
            Elements = new[]
            {
                new CoordinationElementRecord
                {
                    Id = "GB-01",
                    Category = CoordinationCategory.GradeBeam,
                    Code = "GM-1",
                    Level = "Tầng Móng",
                    Step = CoordinationLifecycleStep.Step1_ReadCad,
                    TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel,
                    CadSourceSpec = new CadSourceSpec
                    {
                        Handle = "3F9A",
                        Layer = "DK",
                        TopElevation = -1.600,
                        BottomElevation = -2.000,
                        Width = 200,
                        Height = 400,
                        Supports = new[] { "FT-01", "FT-02" },
                        CrossCheckPassed = true
                    },
                    RevitTargetSpec = new RevitTargetSpec
                    {
                        ElementId = 123456,
                        UniqueId = "abc-123",
                        FamilyName = "Concrete-Rectangular Beam",
                        TypeName = "B_200x400",
                        MeasuredWidth = 200,
                        MeasuredHeight = 400,
                        TwoPhaseSelfCheckPassed = true
                    },
                    AuditRecord = new AuditRecord
                    {
                        Status = AuditStatus.Dung,
                        AuditStep = 4,
                        DeltaDimensionMm = 0.0,
                        DeltaElevationMm = 0.5,
                        DefectRoute = DefectRoute.None
                    },
                    History = new[]
                    {
                        new StepTransition
                        {
                            FromStep = null,
                            ToStep = CoordinationLifecycleStep.Step1_ReadCad,
                            Actor = "CAD_Agent",
                            Comment = "Extracted from DWG"
                        }
                    }
                }
            },
            Rfis = new[]
            {
                new RfiRecord
                {
                    RfiId = "RFI-001",
                    Title = "Xung đột cao độ",
                    ConflictDescription = "Mặt bằng -1.6m vs mặt cắt -1.0m",
                    ElementIds = new[] { "GB-01" },
                    Status = RfiStatus.PendingChiefEngineer
                }
            }
        };

        string json = BimCoordinationLedgerSerializer.Serialize(original, indented: true);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var deserialized = BimCoordinationLedgerSerializer.Deserialize(json);

        Assert.Equal(original.Project, deserialized.Project);
        Assert.Equal(original.ActivePhase, deserialized.ActivePhase);
        Assert.Equal(original.PhaseStatus, deserialized.PhaseStatus);
        Assert.Equal(original.QualityGate, deserialized.QualityGate);
        Assert.Equal(original.Tolerances.SectionDimensionMm, deserialized.Tolerances.SectionDimensionMm);
        Assert.Single(deserialized.Elements);

        var elem = deserialized.Elements[0];
        Assert.Equal("GB-01", elem.Id);
        Assert.Equal("GM-1", elem.Code);
        Assert.Equal(CoordinationCategory.GradeBeam, elem.Category);
        Assert.Equal(200, elem.CadSourceSpec.Width);
        Assert.NotNull(elem.RevitTargetSpec);
        Assert.Equal(123456, elem.RevitTargetSpec.ElementId);
        Assert.NotNull(elem.AuditRecord);
        Assert.Equal(AuditStatus.Dung, elem.AuditRecord.Status);
        Assert.Single(elem.History);

        Assert.Single(deserialized.Rfis);
        Assert.Equal("RFI-001", deserialized.Rfis[0].RfiId);
    }

    [Fact]
    public void Deserialize_ActualLedgerJsonFile_LoadsSuccessfully()
    {
        string path = Path.Combine("..", "..", "..", "..", "..", "state", "bim_coordination_ledger.json");
        if (!File.Exists(path))
        {
            path = Path.Combine("state", "bim_coordination_ledger.json");
        }

        if (File.Exists(path))
        {
            var ledger = BimCoordinationLedgerSerializer.DeserializeFromFile(path);
            Assert.NotNull(ledger);
            Assert.Contains("KC-HT-90", ledger.Project);
            Assert.Equal("PHAN_MONG", ledger.ActivePhase);
        }
    }

    [Fact]
    public void Deserialize_NullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BimCoordinationLedgerSerializer.Deserialize(""));
        Assert.Throws<ArgumentException>(() => BimCoordinationLedgerSerializer.Deserialize("   "));
    }

    [Fact]
    public void SerializeToFile_AtomicReplace_PreservesIntegrityAndCleansUpTempFiles()
    {
        var ledger = new BimCoordinationLedger { Project = "Atomic Test", ActivePhase = "PHAN_THAN" };
        string tempPath = Path.Combine(Path.GetTempPath(), $"atomic_ledger_{Guid.NewGuid():N}.json");
        try
        {
            BimCoordinationLedgerSerializer.SerializeToFile(ledger, tempPath);
            Assert.True(File.Exists(tempPath));

            var updated = ledger with { ActivePhase = "HOAN_THIEN" };
            BimCoordinationLedgerSerializer.SerializeToFile(updated, tempPath);

            var loaded = BimCoordinationLedgerSerializer.DeserializeFromFile(tempPath);
            Assert.Equal("HOAN_THIEN", loaded.ActivePhase);

            string dir = Path.GetDirectoryName(tempPath)!;
            string prefix = Path.GetFileName(tempPath) + ".tmp_";
            var strayTemps = Directory.GetFiles(dir, prefix + "*");
            Assert.Empty(strayTemps);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            string lockPath = $"{tempPath}.lock";
            if (File.Exists(lockPath)) File.Delete(lockPath);
        }
    }
}
