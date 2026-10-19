using System.Text.Json.Serialization;

namespace HPBim.Core.Coordination;

/// <summary>
/// Lifecycle step tracking for each coordination element (1 to 7).
/// </summary>
public enum CoordinationLifecycleStep
{
    Step1_ReadCad = 1,
    Step2_CheckAndClassify = 2,
    Step3_ModelRevit = 3,
    Step4_QaqcAudit = 4,
    Step5_RouteFix = 5,
    Step6_ReAudit = 6,
    Step7_ConfirmedPass = 7
}

/// <summary>
/// Technical readiness classification group for BIM elements.
/// </summary>
public enum ElementTechnicalGroup
{
    Group1_ReadyToModel = 1,
    Group2_ConflictingInfo = 2,
    Group3_MissingInfo = 3
}

/// <summary>
/// Structural element categories tracked in the coordination ledger.
/// </summary>
[JsonConverter(typeof(CoordinationCategoryJsonConverter))]
public enum CoordinationCategory
{
    Pile = 1,
    Footing = 2,
    GradeBeam = 3,
    Column = 4,
    Wall = 5,
    Slab = 6,
    Beam = 7,
    Level = 8,
    Grid = 9,
    Opening = 10,
    Room = 11,
    MepFixture = 12,
    PipeRun = 13,
    Rebar = 14
}

/// <summary>
/// Phase status in the construction / BIM workflow.
/// </summary>
public enum PhaseStatus
{
    InProgress = 1,
    Frozen = 2,
    Accepted = 3
}

/// <summary>
/// QA/QC audit outcome status.
/// </summary>
public enum AuditStatus
{
    Dung = 1,
    SaiCanSua = 2,
    ChuaDuCoSo = 3
}

/// <summary>
/// Routing destination for defects detected during QA/QC.
/// </summary>
public enum DefectRoute
{
    None = 0,
    CadFix = 1,
    RevitFix = 2
}

/// <summary>
/// Status of Request For Information (RFI).
/// </summary>
[JsonConverter(typeof(RfiStatusJsonConverter))]
public enum RfiStatus
{
    PendingChiefEngineer = 1,
    Approved = 2,
    Rejected = 3
}
