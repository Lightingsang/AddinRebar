int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
var warnings = new List<string>();

int nm = 0; string[] matNames = null;
int ret = sapModel.PropMaterial.GetNameList(ref nm, ref matNames);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from PropMaterial.GetNameList");
var materials = new List<object>();
foreach (var name in (matNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eMatType type = eMatType.Steel; int color = 0; string notes = "", guid = "";
    int rm = sapModel.PropMaterial.GetMaterial(name, ref type, ref color, ref notes, ref guid);
    if (rm != 0) { warnings.Add($"material {name}: GetMaterial returned {rm}"); continue; }
    double e = 0, u = 0, a = 0, g = 0;
    int ri = sapModel.PropMaterial.GetMPIsotropic(name, ref e, ref u, ref a, ref g);
    materials.Add(new { name, type = type.ToString(), eMPa = ri == 0 ? Math.Round(e * 1000, 1) : (double?)null, poisson = ri == 0 ? u : (double?)null, thermalCoefficient = ri == 0 ? a : (double?)null, gMPa = ri == 0 ? Math.Round(g * 1000, 1) : (double?)null, isotropic = ri == 0 });
}

int ns = 0; string[] secNames = null;
int retS = sapModel.PropFrame.GetNameList(ref ns, ref secNames);
if (retS != 0) throw new InvalidOperationException($"ETABS returned {retS} from PropFrame.GetNameList");
var frameSections = new List<object>();
foreach (var name in (secNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eFramePropType type = eFramePropType.General;
    int rt = sapModel.PropFrame.GetTypeOAPI(name, ref type);
    double area = 0, as2 = 0, as3 = 0, torsion = 0, i22 = 0, i33 = 0, s22 = 0, s33 = 0, z22 = 0, z33 = 0, r22 = 0, r33 = 0;
    int rp = sapModel.PropFrame.GetSectProps(name, ref area, ref as2, ref as3, ref torsion, ref i22, ref i33, ref s22, ref s33, ref z22, ref z33, ref r22, ref r33);
    if (rp != 0) warnings.Add($"section {name}: GetSectProps returned {rp}");
    frameSections.Add(new { name, type = rt == 0 ? type.ToString() : null, areaMm2 = rp == 0 ? area : (double?)null, i22Mm4 = rp == 0 ? i22 : (double?)null, i33Mm4 = rp == 0 ? i33 : (double?)null, s22Mm3 = rp == 0 ? s22 : (double?)null, s33Mm3 = rp == 0 ? s33 : (double?)null, z22Mm3 = rp == 0 ? z22 : (double?)null, z33Mm3 = rp == 0 ? z33 : (double?)null, r22Mm = rp == 0 ? r22 : (double?)null, r33Mm = rp == 0 ? r33 : (double?)null, torsionMm4 = rp == 0 ? torsion : (double?)null });
}

log($"{nm} materials, {ns} frame sections");
return new
{
    success = true,
    materials,
    materialCount = nm,
    frameSections,
    frameSectionCount = ns,
    truncated = nm > limit || ns > limit,
    warnings,
    summary = $"{materials.Count} of {nm} materials, {frameSections.Count} of {ns} frame sections",
};
