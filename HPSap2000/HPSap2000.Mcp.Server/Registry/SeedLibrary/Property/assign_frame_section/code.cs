var frameNames = args.Strings("frameNames").Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
string section = args.Require("section");
if (frameNames.Count == 0) throw new ArgumentException("frameNames must name at least one frame");

int ns = 0; string[] sections = null;
int rs = sapModel.PropFrame.GetNameList(ref ns, ref sections);
if (rs != 0) throw new InvalidOperationException($"SAP2000 returned {rs} from PropFrame.GetNameList");
section = (sections ?? new string[0]).FirstOrDefault(x => string.Equals(x, section, StringComparison.OrdinalIgnoreCase))
          ?? throw new ArgumentException($"section '{section}' is not defined; see get_materials_and_sections");

var affected = new List<string>();
var errors = new List<object>();
foreach (var name in frameNames)
{
    ct.ThrowIfCancellationRequested();
    int ret = sapModel.FrameObj.SetSection(name, section);
    if (ret == 0) affected.Add(name);
    else errors.Add(new { code = "SAP2000_RET", message = $"SAP2000 returned {ret} from FrameObj.SetSection({name}) — unknown frame?", name });
}

log($"section {section} assigned to {affected.Count} of {frameNames.Count} frames");
return new { success = errors.Count == 0, modifiedCount = affected.Count, affectedNames = affected, section, errors, summary = $"{affected.Count} frame(s) now use {section}; snapshot in the result" };
