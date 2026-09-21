bool incMat = args.Bool("includeMaterials", true);
bool incSec = args.Bool("includeSections", true);

var materials = new List<object>();
if (incMat)
{
    var matNames = structure.Labels.GetAvailableNames(IRobotLabelType.I_LT_MATERIAL);
    for (int i = 1; i <= matNames.Count; i++)
    {
        string name = matNames.Get(i);
        var lbl = structure.Labels.Get(IRobotLabelType.I_LT_MATERIAL, name);
        var data = (IRobotMaterialData)lbl.Data;
        materials.Add(new
        {
            name,
            type = data.Type.ToString(),
            e = data.E,
            nu = data.NU,
            unitWeight = data.RO
        });
    }
}

var sections = new List<object>();
if (incSec)
{
    var secNames = structure.Labels.GetAvailableNames(IRobotLabelType.I_LT_BAR_SECTION);
    for (int i = 1; i <= secNames.Count; i++)
    {
        string name = secNames.Get(i);
        var lbl = structure.Labels.Get(IRobotLabelType.I_LT_BAR_SECTION, name);
        var data = (IRobotBarSectionData)lbl.Data;
        sections.Add(new
        {
            name,
            material = data.MaterialName,
            type = data.Type.ToString(),
            ax = data.GetValue(IRobotBarSectionDataValue.I_BSDV_AX),
            iy = data.GetValue(IRobotBarSectionDataValue.I_BSDV_IY),
            iz = data.GetValue(IRobotBarSectionDataValue.I_BSDV_IZ),
            ix = data.GetValue(IRobotBarSectionDataValue.I_BSDV_IX)
        });
    }
}

return new
{
    success = true,
    materialCount = materials.Count,
    sectionCount = sections.Count,
    materials,
    sections
};
