int caseNum = args.Int("caseNumber");
string nodeSelText = args.Require("nodeNumbers");

if (structure.Results.Available == 0)
    throw new InvalidOperationException("No calculation results are available in the model. Run calculations first.");

var sel = structure.Selections.Create(IRobotObjectType.I_OT_NODE);
if (nodeSelText.Equals("all", StringComparison.OrdinalIgnoreCase))
{
    var nCol = structure.Nodes.GetAll();
    for (int i = 1; i <= nCol.Count; i++)
    {
        var n = (IRobotNode)nCol.Get(i);
        if (n.HasLabel(IRobotLabelType.I_LT_SUPPORT) != 0)
            sel.AddOne(n.Number);
    }
}
else
{
    sel.FromText(nodeSelText);
}

if (sel.Count == 0) throw new ArgumentException($"No nodes found for selection '{nodeSelText}'.");

var reactions = new List<object>();
double sumFx = 0, sumFy = 0, sumFz = 0;

for (int i = 1; i <= sel.Count; i++)
{
    int nodeId = sel.Get(i);
    var rData = structure.Results.Nodes.Reactions.Value(nodeId, caseNum);
    sumFx += rData.FX; sumFy += rData.FY; sumFz += rData.FZ;
    reactions.Add(new
    {
        nodeId,
        fx = rData.FX, fy = rData.FY, fz = rData.FZ,
        mx = rData.MX, my = rData.MY, mz = rData.MZ
    });
}

return new
{
    success = true,
    caseNumber = caseNum,
    count = reactions.Count,
    sum = new { fx = sumFx, fy = sumFy, fz = sumFz },
    reactions
};
