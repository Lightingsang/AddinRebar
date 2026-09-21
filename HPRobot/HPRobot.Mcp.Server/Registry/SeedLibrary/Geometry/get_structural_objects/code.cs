string objType = args.Str("objectType", "all").ToLowerInvariant();
int limit = Math.Clamp(args.Int("limit", 100), 1, 1000);

object nodeList = null;
if (objType == "all" || objType == "nodes")
{
    var nCol = structure.Nodes.GetAll();
    var nodes = new List<object>();
    int nMax = Math.Min(nCol.Count, limit);
    for (int i = 1; i <= nMax; i++)
    {
        var node = (IRobotNode)nCol.Get(i);
        string support = node.HasLabel(IRobotLabelType.I_LT_SUPPORT) != 0 
            ? node.GetLabelName(IRobotLabelType.I_LT_SUPPORT) 
            : null;
        nodes.Add(new { id = node.Number, x = node.X, y = node.Y, z = node.Z, support });
    }
    nodeList = new { total = nCol.Count, returned = nodes.Count, items = nodes };
}

object barList = null;
if (objType == "all" || objType == "bars")
{
    var bCol = structure.Bars.GetAll();
    var bars = new List<object>();
    int bMax = Math.Min(bCol.Count, limit);
    for (int i = 1; i <= bMax; i++)
    {
        var bar = (IRobotBar)bCol.Get(i);
        string section = bar.HasLabel(IRobotLabelType.I_LT_BAR_SECTION) != 0 
            ? bar.GetLabelName(IRobotLabelType.I_LT_BAR_SECTION) 
            : null;
        string material = bar.HasLabel(IRobotLabelType.I_LT_MATERIAL) != 0 
            ? bar.GetLabelName(IRobotLabelType.I_LT_MATERIAL) 
            : null;
        bars.Add(new { id = bar.Number, startNode = bar.StartNode, endNode = bar.EndNode, length = bar.Length, section, material });
    }
    barList = new { total = bCol.Count, returned = bars.Count, items = bars };
}

object panelList = null;
if (objType == "all" || objType == "panels")
{
    var pCol = structure.Objects.GetAll();
    var panels = new List<object>();
    int pMax = Math.Min(pCol.Count, limit);
    for (int i = 1; i <= pMax; i++)
    {
        var obj = (IRobotObjObject)pCol.Get(i);
        string thickness = obj.HasLabel(IRobotLabelType.I_LT_PANEL_THICKNESS) != 0 
            ? obj.GetLabelName(IRobotLabelType.I_LT_PANEL_THICKNESS) 
            : null;
        panels.Add(new { id = obj.Number, thickness });
    }
    panelList = new { total = pCol.Count, returned = panels.Count, items = panels };
}

return new
{
    success = true,
    nodes = nodeList,
    bars = barList,
    panels = panelList
};
