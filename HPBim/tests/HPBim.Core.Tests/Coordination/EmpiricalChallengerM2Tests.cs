namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HPBim.Core.Coordination;
using Xunit;

/// <summary>
/// Empirical adversarial verification harness for Milestone M2 Fail-Fast Gate and Coordination Ledger.
/// Enforces all invariants specified in DISPATCH.md and ORIGINAL_REQUEST.md.
/// </summary>
public sealed class EmpiricalChallengerM2Tests
{
    private readonly FailFastGate _gate = new();

    #region 1. Fail-Fast Gate Invariant Tests

    [Theory]
    [InlineData(CoordinationCategory.GradeBeam)]
    [InlineData(CoordinationCategory.Footing)]
    [InlineData(CoordinationCategory.Pile)]
    [InlineData(CoordinationCategory.Column)]
    public void FailFastGate_MissingTopElevation_ThrowsFailFastCoordinationException(CoordinationCategory category)
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_ELEV_TEST",
            TopElevation = null, // Missing elevation
            BottomElevation = category == CoordinationCategory.Footing ? -2.000 : null,
            Width = 300,
            Height = 500,
            Diameter = 400,
            Supports = new[] { "SUP-01", "SUP-02" },
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, category, "ELEM-NO-ELEV"));

        Assert.Equal("ELEM-NO-ELEV", ex.ElementId);
        Assert.Contains(ex.Violations, v => v.Contains("FF-3"));
    }

    [Fact]
    public void FailFastGate_FootingMissingBottomElevation_ThrowsFailFastCoordinationException()
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_FT_01",
            TopElevation = -1.600,
            BottomElevation = null, // Footing MUST have bottom elevation
            Width = 1500,
            Height = 1500,
            Supports = new[] { "GRID-A1" },
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.Footing, "FT-NO-BOTTOM"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-3") && v.Contains("đáy đài"));
    }

    [Theory]
    [InlineData(-1.600, -1.600)] // Bottom == Top
    [InlineData(-1.600, -1.200)] // Bottom > Top
    public void FailFastGate_InvertedElevations_ThrowsFailFastCoordinationException(double top, double bottom)
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_INVERTED",
            TopElevation = top,
            BottomElevation = bottom,
            Width = 1200,
            Height = 1200,
            Supports = new[] { "GRID-B2" },
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.Footing, "FT-INVERTED"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-3") && v.Contains(">="));
    }

    [Theory]
    [InlineData(0, 400)]    // Width == 0
    [InlineData(-200, 400)] // Width < 0
    [InlineData(200, 0)]    // Height == 0
    [InlineData(200, -300)] // Height < 0
    public void FailFastGate_ZeroOrNegativeDimensions_ThrowsFailFastCoordinationException(double width, double height)
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_DIM_TEST",
            TopElevation = -1.600,
            Width = width,
            Height = height,
            Supports = new[] { "FT-01", "FT-02" },
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.GradeBeam, "GB-BAD-DIM"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-4"));
    }

    [Fact]
    public void FailFastGate_NullDimensions_ThrowsFailFastCoordinationException()
    {
        var specNullWidth = new CadSourceSpec
        {
            Handle = "HDL_NULL_W",
            TopElevation = -1.600,
            Width = null,
            Height = 400,
            Supports = new[] { "FT-01", "FT-02" }
        };

        var exW = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(specNullWidth, CoordinationCategory.GradeBeam, "GB-NULL-W"));
        Assert.Contains(exW.Violations, v => v.Contains("FF-4") && v.Contains("Width"));

        var specNullHeight = new CadSourceSpec
        {
            Handle = "HDL_NULL_H",
            TopElevation = -1.600,
            Width = 200,
            Height = null,
            Supports = new[] { "FT-01", "FT-02" }
        };

        var exH = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(specNullHeight, CoordinationCategory.GradeBeam, "GB-NULL-H"));
        Assert.Contains(exH.Violations, v => v.Contains("FF-4") && v.Contains("Height"));
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(0.0, null, null)]
    [InlineData(-400.0, null, null)]
    [InlineData(null, 0.0, 400.0)]
    public void FailFastGate_PileInvalidCrossSection_ThrowsFailFastCoordinationException(
        double? diameter, double? width, double? height)
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_PILE_DIM",
            TopElevation = -2.500,
            Diameter = diameter,
            Width = width,
            Height = height,
            Supports = new[] { "FT-01" },
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.Pile, "PILE-BAD-DIM"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-4") && v.Contains("tiết diện"));
    }

    [Fact]
    public void FailFastGate_GradeBeamFewerThan2Supports_ThrowsFailFastCoordinationException()
    {
        var specZero = new CadSourceSpec
        {
            Handle = "HDL_GB_SUP_0",
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = Array.Empty<string>(),
            CrossCheckPassed = true
        };

        var ex0 = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(specZero, CoordinationCategory.GradeBeam, "GB-SUP-0"));
        Assert.Contains(ex0.Violations, v => v.Contains("FF-5") && v.Contains("2 đầu"));

        var specOne = new CadSourceSpec
        {
            Handle = "HDL_GB_SUP_1",
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = new[] { "FT-ONLY-ONE" },
            CrossCheckPassed = true
        };

        var ex1 = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(specOne, CoordinationCategory.GradeBeam, "GB-SUP-1"));
        Assert.Contains(ex1.Violations, v => v.Contains("FF-5") && v.Contains("2 đầu"));
    }

    [Fact]
    public void FailFastGate_PileMissingFootingSupport_ThrowsFailFastCoordinationException()
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_P_NOSUP",
            TopElevation = -2.500,
            Diameter = 400,
            Supports = Array.Empty<string>(),
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.Pile, "P-NO-SUP"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-5") && v.Contains("đài móng"));
    }

    [Fact]
    public void FailFastGate_ColumnMissingSupport_ThrowsFailFastCoordinationException()
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_C_NOSUP",
            TopElevation = 3.800,
            Width = 220,
            Height = 220,
            Supports = Array.Empty<string>(),
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.Column, "C-NO-SUP"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-5") && v.Contains("điểm tựa móng"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FailFastGate_MissingCadHandle_ThrowsFailFastCoordinationException(string emptyHandle)
    {
        var spec = new CadSourceSpec
        {
            Handle = emptyHandle,
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = new[] { "FT-01", "FT-02" },
            CrossCheckPassed = true
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.GradeBeam, "GB-NO-HDL"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-1") && v.Contains("CAD Handle"));
    }

    [Fact]
    public void FailFastGate_CrossCheckFailed_ThrowsFailFastCoordinationException()
    {
        var spec = new CadSourceSpec
        {
            Handle = "HDL_CROSS_FAIL",
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = new[] { "FT-01", "FT-02" },
            CrossCheckPassed = false // Conflict between sources
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.GradeBeam, "GB-CONFLICT"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-6"));
    }

    [Fact]
    public void FailFastGate_NullCadSourceSpec_ThrowsFailFastCoordinationException()
    {
        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(null!, CoordinationCategory.GradeBeam, "GB-NULL-SPEC"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-0"));
    }

    [Fact]
    public void FailFastGate_MultipleViolationsAccumulated_ReportsAllInException()
    {
        var multiDefectSpec = new CadSourceSpec
        {
            Handle = "",           // Defect 1: FF-1
            TopElevation = null,   // Defect 2: FF-3
            Width = -10,           // Defect 3: FF-4
            Height = 0,            // Defect 4: FF-4
            Supports = null!,      // Defect 5: FF-5
            CrossCheckPassed = false // Defect 6: FF-6
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(multiDefectSpec, CoordinationCategory.GradeBeam, "GB-MULTI-ERR"));

        Assert.True(ex.Violations.Count >= 5, $"Expected at least 5 violations, got {ex.Violations.Count}");
        Assert.Contains(ex.Violations, v => v.Contains("FF-1"));
        Assert.Contains(ex.Violations, v => v.Contains("FF-3"));
        Assert.Contains(ex.Violations, v => v.Contains("FF-4"));
        Assert.Contains(ex.Violations, v => v.Contains("FF-5"));
        Assert.Contains(ex.Violations, v => v.Contains("FF-6"));
    }

    [Fact]
    public void FailFastGate_ServiceTransitionsToStep3_EnforcesFailFastGateStrictly()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Test Project");

        var badElement = new CoordinationElementRecord
        {
            Id = "GB-INVALID-CAD",
            Category = CoordinationCategory.GradeBeam,
            Code = "GM-ERR",
            Step = CoordinationLifecycleStep.Step1_ReadCad,
            TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel,
            CadSourceSpec = new CadSourceSpec
            {
                Handle = "3F99",
                TopElevation = -1.600,
                Width = 0, // Invalid width!
                Height = 400,
                Supports = new[] { "FT-01", "FT-02" }
            }
        };

        service.RegisterElement(badElement);
        service.TransitionStep("GB-INVALID-CAD", CoordinationLifecycleStep.Step2_CheckAndClassify, "CAD_Agent", "Checking");

        // Attempt Step 2 -> Step 3 with invalid CadSourceSpec: must throw FailFastCoordinationException
        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            service.TransitionStep("GB-INVALID-CAD", CoordinationLifecycleStep.Step3_ModelRevit, "Revit_Agent", "Try Model"));

        Assert.Contains(ex.Violations, v => v.Contains("FF-4"));

        // Verify element state was NOT mutated to Step 3
        Assert.Equal(CoordinationLifecycleStep.Step2_CheckAndClassify, service.Ledger.Elements[0].Step);
    }

    #endregion

    #region 2. 7-Step State Machine Transition Invariant Tests

    [Fact]
    public void StateMachine_Full7x7TransitionMatrix_EnforcesOnlyLegalTransitions()
    {
        var steps = Enum.GetValues<CoordinationLifecycleStep>();

        // Predefine strictly legal transitions according to BimCoordinationLedgerService
        var legalTransitions = new HashSet<(CoordinationLifecycleStep From, CoordinationLifecycleStep To)>
        {
            (CoordinationLifecycleStep.Step1_ReadCad, CoordinationLifecycleStep.Step2_CheckAndClassify),
            (CoordinationLifecycleStep.Step2_CheckAndClassify, CoordinationLifecycleStep.Step3_ModelRevit),
            (CoordinationLifecycleStep.Step3_ModelRevit, CoordinationLifecycleStep.Step4_QaqcAudit),
            (CoordinationLifecycleStep.Step4_QaqcAudit, CoordinationLifecycleStep.Step5_RouteFix),
            (CoordinationLifecycleStep.Step4_QaqcAudit, CoordinationLifecycleStep.Step7_ConfirmedPass),
            (CoordinationLifecycleStep.Step5_RouteFix, CoordinationLifecycleStep.Step6_ReAudit),
            (CoordinationLifecycleStep.Step6_ReAudit, CoordinationLifecycleStep.Step5_RouteFix),
            (CoordinationLifecycleStep.Step6_ReAudit, CoordinationLifecycleStep.Step7_ConfirmedPass)
        };

        foreach (var fromStep in steps)
        {
            foreach (var toStep in steps)
            {
                var service = new BimCoordinationLedgerService();
                service.Initialize("Matrix Verification");

                var element = CreateValidElement($"EL_{fromStep}_{toStep}", CoordinationCategory.GradeBeam, "GB-M");
                element = element with { Step = fromStep };
                service.RegisterElement(element);

                bool isLegal = legalTransitions.Contains((fromStep, toStep));

                if (isLegal)
                {
                    var updated = service.TransitionStep(element.Id, toStep, "TestActor", "Testing legal step");
                    Assert.Equal(toStep, updated.Step);
                }
                else
                {
                    var ex = Assert.Throws<InvalidOperationException>(() =>
                        service.TransitionStep(element.Id, toStep, "TestActor", "Testing illegal step"));
                    Assert.Contains("Invalid lifecycle step transition", ex.Message);
                }
            }
        }
    }

    [Theory]
    [InlineData(ElementTechnicalGroup.Group2_ConflictingInfo)]
    [InlineData(ElementTechnicalGroup.Group3_MissingInfo)]
    public void StateMachine_NonGroup1Element_CannotTransitionToStep3(ElementTechnicalGroup invalidGroup)
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Group Check");

        var element = CreateValidElement("GB-BLOCKED", CoordinationCategory.GradeBeam, "GB-B");
        service.RegisterElement(element);
        service.TransitionStep("GB-BLOCKED", CoordinationLifecycleStep.Step2_CheckAndClassify, "CAD_Agent", "Review");
        service.ClassifyElement("GB-BLOCKED", invalidGroup, "CAD_Agent", "Classified as not ready");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.TransitionStep("GB-BLOCKED", CoordinationLifecycleStep.Step3_ModelRevit, "Revit_Agent", "Model"));

        Assert.Contains("Only Group1 elements can be modeled in Revit", ex.Message);
    }

    [Fact]
    public void StateMachine_Step7Terminal_CannotTransitionToAnyState()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Terminal Test");

        var element = CreateValidElement("GB-DONE", CoordinationCategory.GradeBeam, "GB-D");
        element = element with { Step = CoordinationLifecycleStep.Step7_ConfirmedPass };
        service.RegisterElement(element);

        foreach (var step in Enum.GetValues<CoordinationLifecycleStep>())
        {
            Assert.Throws<InvalidOperationException>(() =>
                service.TransitionStep("GB-DONE", step, "Actor", "Try transition from terminal"));
        }
    }

    [Fact]
    public void StateMachine_HistoryTracking_MaintainsImmutableAuditLog()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("History Audit");

        var element = CreateValidElement("GB-HIST", CoordinationCategory.GradeBeam, "GM-H");
        service.RegisterElement(element);

        service.TransitionStep("GB-HIST", CoordinationLifecycleStep.Step2_CheckAndClassify, "Agent_CAD", "CAD Checked");
        service.TransitionStep("GB-HIST", CoordinationLifecycleStep.Step3_ModelRevit, "Agent_Revit", "Revit Modeled");
        service.TransitionStep("GB-HIST", CoordinationLifecycleStep.Step4_QaqcAudit, "Agent_QAQC", "Audit 8-steps");
        service.TransitionStep("GB-HIST", CoordinationLifecycleStep.Step5_RouteFix, "Agent_QAQC", "Defect: Offset 1.5mm");
        service.TransitionStep("GB-HIST", CoordinationLifecycleStep.Step6_ReAudit, "Agent_QAQC", "Re-auditing fixed item");
        var final = service.TransitionStep("GB-HIST", CoordinationLifecycleStep.Step7_ConfirmedPass, "Agent_QAQC", "Final Pass");

        Assert.Equal(6, final.History.Count);
        Assert.Equal(CoordinationLifecycleStep.Step1_ReadCad, final.History[0].FromStep);
        Assert.Equal(CoordinationLifecycleStep.Step2_CheckAndClassify, final.History[0].ToStep);
        Assert.Equal("Agent_CAD", final.History[0].Actor);

        Assert.Equal(CoordinationLifecycleStep.Step6_ReAudit, final.History[5].FromStep);
        Assert.Equal(CoordinationLifecycleStep.Step7_ConfirmedPass, final.History[5].ToStep);
        Assert.Equal("Agent_QAQC", final.History[5].Actor);
    }

    #endregion

    #region 3. Ledger JSON Serialization Invariant Tests

    [Fact]
    public void LedgerJson_FullModelRoundtrip_PreservesAllFieldsAndSpecialCharacters()
    {
        var original = new BimCoordinationLedger
        {
            Project = "Dự Án Hội Trường Tân Đông Hiệp (KC-HT-90)",
            UpdatedAt = DateTime.UtcNow,
            PrimeDirective = "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin.",
            ActivePhase = "PHAN_MONG",
            PhaseStatus = PhaseStatus.InProgress,
            QualityGate = "GATE_2_FOUNDATION_EXECUTION",
            Tolerances = new CoordinationTolerances
            {
                SectionDimensionMm = 0.0,
                ElevationMm = 1.0,
                PlanPositionMm = 1.0,
                PileCenterMm = 2.0
            },
            Summary = new CoordinationProgressSummary
            {
                TotalElements = 2,
                Step1_ReadCad = 1,
                Step7_ConfirmedPass = 1,
                Group1_ReadyToModel = 1,
                Group3_MissingInfo = 1,
                UnresolvedHalt = 1
            },
            Elements = new[]
            {
                new CoordinationElementRecord
                {
                    Id = "FT-DC1-01",
                    Category = CoordinationCategory.Footing,
                    Code = "ĐC-1",
                    Level = "Tầng Móng (-1.600m)",
                    Step = CoordinationLifecycleStep.Step7_ConfirmedPass,
                    TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel,
                    CadSourceSpec = new CadSourceSpec
                    {
                        DwgSheet = "KC-02",
                        Handle = "2A5F",
                        Layer = "DK_MONG",
                        TopElevation = -1.600,
                        BottomElevation = -2.500,
                        Width = 1400,
                        Height = 1400,
                        Supports = new[] { "P-01", "P-02", "P-03" },
                        CrossCheckSources = new[] { "Mặt bằng", "Mặt cắt", "Chi tiết" },
                        CrossCheckPassed = true,
                        RawNotes = "Đài cọc ĐC-1 3 cọc D400"
                    },
                    RevitTargetSpec = new RevitTargetSpec
                    {
                        ElementId = 987654,
                        UniqueId = "guid-ft-001-revit",
                        FamilyName = "M_Structural Foundation",
                        TypeName = "DC1_1400x1400x900",
                        LevelName = "Tầng Móng (-1.600m)",
                        MeasuredWidth = 1400,
                        MeasuredHeight = 1400,
                        MeasuredTopElevation = -1.600,
                        TwoPhaseSelfCheckPassed = true,
                        ModeledAt = DateTime.UtcNow
                    },
                    AuditRecord = new AuditRecord
                    {
                        Status = AuditStatus.Dung,
                        AuditStep = 8,
                        DeltaDimensionMm = 0.0,
                        DeltaElevationMm = 0.0,
                        DeltaPositionMm = 0.5,
                        DefectRoute = DefectRoute.None,
                        Notes = "Đạt nghiệm thu 100%",
                        AuditedAt = DateTime.UtcNow
                    },
                    History = new[]
                    {
                        new StepTransition
                        {
                            FromStep = CoordinationLifecycleStep.Step1_ReadCad,
                            ToStep = CoordinationLifecycleStep.Step2_CheckAndClassify,
                            Actor = "CAD_Assistant",
                            Comment = "Đọc CAD hoàn tất"
                        }
                    }
                },
                new CoordinationElementRecord
                {
                    Id = "GB-GM-05",
                    Category = CoordinationCategory.GradeBeam,
                    Code = "GM-5",
                    Level = "Tầng Móng",
                    Step = CoordinationLifecycleStep.Step1_ReadCad,
                    TechnicalGroup = ElementTechnicalGroup.Group3_MissingInfo,
                    CadSourceSpec = new CadSourceSpec
                    {
                        Handle = "3F88",
                        TopElevation = -1.600,
                        Width = null,
                        Height = null,
                        RawNotes = "Thiếu mặt cắt chi tiết dầm"
                    }
                }
            },
            Rfis = new[]
            {
                new RfiRecord
                {
                    RfiId = "RFI-001",
                    Title = "Làm rõ tiết diện GM-5",
                    ConflictDescription = "Mặt bằng ghi GM-5 nhưng thiếu bảng chi tiết tiết diện",
                    ElementIds = new[] { "GB-GM-5" },
                    ProposedAction = "Đề nghị Thiết kế bổ sung tiết diện b x h",
                    Status = RfiStatus.PendingChiefEngineer
                }
            }
        };

        // Serialize and Deserialize
        string json = BimCoordinationLedgerSerializer.Serialize(original, indented: true);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var roundtripped = BimCoordinationLedgerSerializer.Deserialize(json);

        // Assert all root fields
        Assert.Equal(original.Project, roundtripped.Project);
        Assert.Equal(original.PrimeDirective, roundtripped.PrimeDirective);
        Assert.Equal(original.ActivePhase, roundtripped.ActivePhase);
        Assert.Equal(original.PhaseStatus, roundtripped.PhaseStatus);
        Assert.Equal(original.QualityGate, roundtripped.QualityGate);
        Assert.Equal(original.Tolerances.SectionDimensionMm, roundtripped.Tolerances.SectionDimensionMm);
        Assert.Equal(original.Tolerances.PileCenterMm, roundtripped.Tolerances.PileCenterMm);

        // Assert Summary
        Assert.Equal(2, roundtripped.Summary.TotalElements);
        Assert.Equal(1, roundtripped.Summary.UnresolvedHalt);

        // Assert Elements
        Assert.Equal(2, roundtripped.Elements.Count);

        var el0 = roundtripped.Elements[0];
        Assert.Equal("FT-DC1-01", el0.Id);
        Assert.Equal(CoordinationCategory.Footing, el0.Category);
        Assert.Equal(ElementTechnicalGroup.Group1_ReadyToModel, el0.TechnicalGroup);
        Assert.Equal(CoordinationLifecycleStep.Step7_ConfirmedPass, el0.Step);
        Assert.Equal(1400, el0.CadSourceSpec.Width);
        Assert.Equal(-2.500, el0.CadSourceSpec.BottomElevation);
        Assert.Equal(3, el0.CadSourceSpec.Supports.Count);
        Assert.NotNull(el0.RevitTargetSpec);
        Assert.Equal(987654, el0.RevitTargetSpec.ElementId);
        Assert.NotNull(el0.AuditRecord);
        Assert.Equal(AuditStatus.Dung, el0.AuditRecord.Status);
        Assert.Single(el0.History);

        var el1 = roundtripped.Elements[1];
        Assert.Equal("GB-GM-05", el1.Id);
        Assert.Equal(ElementTechnicalGroup.Group3_MissingInfo, el1.TechnicalGroup);
        Assert.Null(el1.CadSourceSpec.Width);

        // Assert Rfis
        Assert.Single(roundtripped.Rfis);
        Assert.Equal("RFI-001", roundtripped.Rfis[0].RfiId);
        Assert.Equal(RfiStatus.PendingChiefEngineer, roundtripped.Rfis[0].Status);
    }

    [Fact]
    public async Task LedgerJson_FileOperations_WriteAndReadMatchExactly()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"emp_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new BimCoordinationLedgerService();
            service.Initialize("Disk Test Project");
            var el = CreateValidElement("EL-DISK", CoordinationCategory.Column, "C-1");
            service.RegisterElement(el);

            await service.SaveToFileAsync(tempFile);
            Assert.True(File.Exists(tempFile));

            var loadedLedger = await BimCoordinationLedgerSerializer.DeserializeFromFileAsync(tempFile);
            Assert.Equal("Disk Test Project", loadedLedger.Project);
            Assert.Single(loadedLedger.Elements);
            Assert.Equal("EL-DISK", loadedLedger.Elements[0].Id);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LedgerJson_StateJsonFileInRepo_DeserializesCleanly()
    {
        // Check relative paths to state/bim_coordination_ledger.json
        string[] candidatePaths =
        {
            Path.Combine("..", "..", "..", "..", "..", "state", "bim_coordination_ledger.json"),
            Path.Combine("state", "bim_coordination_ledger.json"),
            @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\state\bim_coordination_ledger.json"
        };

        string? foundPath = candidatePaths.FirstOrDefault(File.Exists);
        Assert.NotNull(foundPath);

        var ledger = BimCoordinationLedgerSerializer.DeserializeFromFile(foundPath);
        Assert.NotNull(ledger);
        Assert.Equal("PHAN_MONG", ledger.ActivePhase);
        Assert.Equal(PhaseStatus.InProgress, ledger.PhaseStatus);
        Assert.NotNull(ledger.Tolerances);
        Assert.Equal(0.0, ledger.Tolerances.SectionDimensionMm);
        Assert.Equal(1.0, ledger.Tolerances.ElevationMm);
        Assert.NotNull(ledger.Summary);
    }

    [Fact]
    public void SummaryCalculation_UnresolvedHalt_EqualsNonGroup1Count()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Summary Invariant Test");

        var g1 = CreateValidElement("E1", CoordinationCategory.GradeBeam, "G1") with
        {
            TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
        };
        var g2 = CreateValidElement("E2", CoordinationCategory.GradeBeam, "G2") with
        {
            TechnicalGroup = ElementTechnicalGroup.Group2_ConflictingInfo
        };
        var g3 = CreateValidElement("E3", CoordinationCategory.GradeBeam, "G3") with
        {
            TechnicalGroup = ElementTechnicalGroup.Group3_MissingInfo
        };

        service.RegisterElement(g1);
        service.RegisterElement(g2);
        service.RegisterElement(g3);

        var summary = service.CalculateSummary();

        Assert.Equal(3, summary.TotalElements);
        Assert.Equal(1, summary.Group1_ReadyToModel);
        Assert.Equal(1, summary.Group2_ConflictingInfo);
        Assert.Equal(1, summary.Group3_MissingInfo);
        Assert.Equal(2, summary.UnresolvedHalt);
        Assert.Equal(summary.Group2_ConflictingInfo + summary.Group3_MissingInfo, summary.UnresolvedHalt);
    }

    #endregion

    private static CoordinationElementRecord CreateValidElement(string id, CoordinationCategory cat, string code)
    {
        return new CoordinationElementRecord
        {
            Id = id,
            Category = cat,
            Code = code,
            Level = "Tầng Móng",
            Step = CoordinationLifecycleStep.Step1_ReadCad,
            TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel,
            CadSourceSpec = new CadSourceSpec
            {
                Handle = "3F9A1",
                Layer = "DK",
                TopElevation = -1.600,
                BottomElevation = -2.000,
                Width = 200,
                Height = 400,
                Supports = new[] { "FT-01", "FT-02" },
                CrossCheckPassed = true
            }
        };
    }
}
