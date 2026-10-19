namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using Xunit;

public sealed class CoordinationLifecycleStepTests
{
    [Fact]
    public void TransitionStep_SequentialHappyPath_TransitionsThroughAllStepsSuccessfully()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Test Project");

        var element = CreateValidElement("GB-01", CoordinationCategory.GradeBeam, "GM-1");
        service.RegisterElement(element);

        // Step 1 -> Step 2
        var s2 = service.TransitionStep("GB-01", CoordinationLifecycleStep.Step2_CheckAndClassify, "CAD_Agent", "Classified OK");
        Assert.Equal(CoordinationLifecycleStep.Step2_CheckAndClassify, s2.Step);
        Assert.Single(s2.History);

        // Classify to Group 1 (Ready)
        service.ClassifyElement("GB-01", ElementTechnicalGroup.Group1_ReadyToModel, "CAD_Agent", "Verified against DWG");

        // Step 2 -> Step 3 (Allowed because Group 1 and valid spec)
        var s3 = service.TransitionStep("GB-01", CoordinationLifecycleStep.Step3_ModelRevit, "Revit_Agent", "Modeled in Revit");
        Assert.Equal(CoordinationLifecycleStep.Step3_ModelRevit, s3.Step);
        Assert.Equal(2, s3.History.Count);

        // Step 3 -> Step 4
        var s4 = service.TransitionStep("GB-01", CoordinationLifecycleStep.Step4_QaqcAudit, "QAQC_Agent", "Auditing 8 steps");
        Assert.Equal(CoordinationLifecycleStep.Step4_QaqcAudit, s4.Step);

        // Step 4 -> Step 7 (Clean pass)
        var s7 = service.TransitionStep("GB-01", CoordinationLifecycleStep.Step7_ConfirmedPass, "QAQC_Agent", "100% Match");
        Assert.Equal(CoordinationLifecycleStep.Step7_ConfirmedPass, s7.Step);
        Assert.Equal(4, s7.History.Count);

        var summary = service.CalculateSummary();
        Assert.Equal(1, summary.TotalElements);
        Assert.Equal(1, summary.Step7_ConfirmedPass);
        Assert.Equal(1, summary.Group1_ReadyToModel);
    }

    [Fact]
    public void TransitionStep_DefectRouteAndReAudit_TransitionsThroughFixAndPasses()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Test Project");

        var element = CreateValidElement("GB-02", CoordinationCategory.GradeBeam, "GM-2");
        service.RegisterElement(element);
        service.TransitionStep("GB-02", CoordinationLifecycleStep.Step2_CheckAndClassify, "CAD_Agent", "Verified");
        service.ClassifyElement("GB-02", ElementTechnicalGroup.Group1_ReadyToModel, "CAD_Agent", "Ready");
        service.TransitionStep("GB-02", CoordinationLifecycleStep.Step3_ModelRevit, "Revit_Agent", "Modeled");

        // Step 3 -> Step 4
        service.TransitionStep("GB-02", CoordinationLifecycleStep.Step4_QaqcAudit, "QAQC_Agent", "Audited");

        // Step 4 -> Step 5 (Defect found)
        var s5 = service.TransitionStep("GB-02", CoordinationLifecycleStep.Step5_RouteFix, "QAQC_Agent", "Defect: Offset 2mm");
        Assert.Equal(CoordinationLifecycleStep.Step5_RouteFix, s5.Step);

        // Step 5 -> Step 6 (Re-audit)
        var s6 = service.TransitionStep("GB-02", CoordinationLifecycleStep.Step6_ReAudit, "QAQC_Agent", "Re-auditing");
        Assert.Equal(CoordinationLifecycleStep.Step6_ReAudit, s6.Step);

        // Step 6 -> Step 7 (Confirmed pass after fix)
        var s7 = service.TransitionStep("GB-02", CoordinationLifecycleStep.Step7_ConfirmedPass, "QAQC_Agent", "Pass");
        Assert.Equal(CoordinationLifecycleStep.Step7_ConfirmedPass, s7.Step);
    }

    [Fact]
    public void TransitionStep_SkipStepAttempt_ThrowsInvalidOperationException()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Test Project");

        var element = CreateValidElement("GB-03", CoordinationCategory.GradeBeam, "GM-3");
        service.RegisterElement(element);

        // Attempting Step 1 -> Step 3 directly (Skipping Step 2)
        var ex1 = Assert.Throws<InvalidOperationException>(() =>
            service.TransitionStep("GB-03", CoordinationLifecycleStep.Step3_ModelRevit, "Actor", "Jump"));
        Assert.Contains("Step skipping is prohibited", ex1.Message);

        // Transition to Step 2
        service.TransitionStep("GB-03", CoordinationLifecycleStep.Step2_CheckAndClassify, "Actor", "Next");

        // Attempting Step 2 -> Step 7 directly (Skipping Steps 3, 4)
        var ex2 = Assert.Throws<InvalidOperationException>(() =>
            service.TransitionStep("GB-03", CoordinationLifecycleStep.Step7_ConfirmedPass, "Actor", "Jump to pass"));
        Assert.Contains("Step skipping is prohibited", ex2.Message);
    }

    [Fact]
    public void TransitionStep_NonGroup1ElementToStep3_ThrowsInvalidOperationException()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Test Project");

        var element = CreateValidElement("GB-04", CoordinationCategory.GradeBeam, "GM-4");
        service.RegisterElement(element);
        service.TransitionStep("GB-04", CoordinationLifecycleStep.Step2_CheckAndClassify, "CAD_Agent", "Check");

        // Element is classified into Group 2 (Conflict)
        service.ClassifyElement("GB-04", ElementTechnicalGroup.Group2_ConflictingInfo, "CAD_Agent", "Conflict found");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.TransitionStep("GB-04", CoordinationLifecycleStep.Step3_ModelRevit, "Revit_Agent", "Attempting model"));
        Assert.Contains("Only Group1 elements can be modeled in Revit", ex.Message);
    }

    [Fact]
    public void TransitionStep_UnknownElementId_ThrowsKeyNotFoundException()
    {
        var service = new BimCoordinationLedgerService();
        service.Initialize("Test Project");

        Assert.Throws<KeyNotFoundException>(() =>
            service.TransitionStep("UNKNOWN-ID", CoordinationLifecycleStep.Step2_CheckAndClassify, "Actor", ""));
    }

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
