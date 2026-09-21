string nodeSelText = args.Require("nodeNumbers");
string suppType = args.Require("supportType");

var sel = structure.Selections.Create(IRobotObjectType.I_OT_NODE);
sel.FromText(nodeSelText);
if (sel.Count == 0) throw new ArgumentException($"No valid nodes found matching selection '{nodeSelText}'.");

structure.Nodes.SetLabel(sel, IRobotLabelType.I_LT_SUPPORT, suppType);

log($"Assigned support '{suppType}' to {sel.Count} nodes: {sel.ToText()}");

return new
{
    success = true,
    support = suppType,
    nodeCount = sel.Count,
    assignedNodes = sel.ToText()
};
