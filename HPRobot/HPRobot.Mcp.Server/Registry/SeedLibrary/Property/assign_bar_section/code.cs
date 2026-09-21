string barSelText = args.Require("barNumbers");
string sectionName = args.Require("sectionName");

var sel = structure.Selections.Create(IRobotObjectType.I_OT_BAR);
sel.FromText(barSelText);
if (sel.Count == 0) throw new ArgumentException($"No valid bars found matching selection '{barSelText}'.");

structure.Bars.SetLabel(sel, IRobotLabelType.I_LT_BAR_SECTION, sectionName);

log($"Assigned section '{sectionName}' to {sel.Count} bars: {sel.ToText()}");

return new
{
    success = true,
    section = sectionName,
    barCount = sel.Count,
    assignedBars = sel.ToText()
};
