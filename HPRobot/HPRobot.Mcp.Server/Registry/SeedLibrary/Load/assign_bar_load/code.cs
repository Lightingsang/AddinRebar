int caseNum = args.Int("caseNumber");
string barSelText = args.Require("barNumbers");
string loadType = args.Str("loadType", "uniform").ToLowerInvariant();
double pz = args.Double("pz");
double px = args.Double("px", 0.0);
double py = args.Double("py", 0.0);
double relPos = Math.Clamp(args.Double("relativePosition", 0.5), 0.0, 1.0);
bool isLocal = args.Bool("isLocal", false);

if (structure.Cases.Exist(caseNum) == 0)
    throw new ArgumentException($"Load case {caseNum} does not exist in the model.");

var c = structure.Cases.Get(caseNum);
if (c is not IRobotSimpleCase sc)
    throw new InvalidOperationException($"Case {caseNum} is not a simple load case (records cannot be added to combinations directly).");

var barSel = structure.Selections.Create(IRobotObjectType.I_OT_BAR);
barSel.FromText(barSelText);
if (barSel.Count == 0) throw new ArgumentException($"No valid bars found for selection '{barSelText}'.");

if (loadType == "uniform")
{
    int recIdx = sc.Records.New(IRobotLoadRecordType.I_LRT_BAR_UNIFORM);
    var rec = sc.Records.Get(recIdx);
    rec.SetValue((short)IRobotUniformRecordValues.I_URV_PX, px);
    rec.SetValue((short)IRobotUniformRecordValues.I_URV_PY, py);
    rec.SetValue((short)IRobotUniformRecordValues.I_URV_PZ, pz);
    rec.SetValue((short)IRobotUniformRecordValues.I_URV_LOCAL_SYSTEM, isLocal ? 1.0 : 0.0);
    rec.Objects.FromText(barSel.ToText());
}
else
{
    int recIdx = sc.Records.New(IRobotLoadRecordType.I_LRT_BAR_FORCE_CONCENTRATED);
    var rec = sc.Records.Get(recIdx);
    rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_FX, px);
    rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_FY, py);
    rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_FZ, pz);
    rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_X, relPos);
    rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_REL, 1.0);
    rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_LOC, isLocal ? 1.0 : 0.0);
    rec.Objects.FromText(barSel.ToText());
}

log($"Applied {loadType} load (Pz={pz} kN) to case {caseNum} on bars {barSel.ToText()}");

return new
{
    success = true,
    caseNumber = caseNum,
    bars = barSel.ToText(),
    loadType,
    pz, px, py,
    isLocal
};
