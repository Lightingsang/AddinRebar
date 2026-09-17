double x1 = args.RequireDouble("x1"), y1 = args.RequireDouble("y1"), z1 = args.RequireDouble("z1");
double x2 = args.RequireDouble("x2"), y2 = args.RequireDouble("y2"), z2 = args.RequireDouble("z2");
string section = args.Str("section", null);
string userName = args.Str("name", "") ?? "";
double length = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) + (z2 - z1) * (z2 - z1));
if (length < 1) throw new ArgumentException("the two points coincide (length < 1 mm)");

if (section != null)
{
    int ns = 0; string[] sections = null;
    int rs = sapModel.PropFrame.GetNameList(ref ns, ref sections);
    if (rs != 0) throw new InvalidOperationException($"ETABS returned {rs} from PropFrame.GetNameList");
    section = (sections ?? new string[0]).FirstOrDefault(x => string.Equals(x, section, StringComparison.OrdinalIgnoreCase))
              ?? throw new ArgumentException($"section '{section}' is not defined; see get_materials_and_sections");
}

string name = "";
int ret = sapModel.FrameObj.AddByCoord(x1, y1, z1, x2, y2, z2, ref name, section ?? "Default", userName);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from FrameObj.AddByCoord");

log($"frame {name} added, length {length:0} mm, section {section ?? "Default"}");
return new { success = true, createdCount = 1, affectedNames = new[] { name }, lengthMm = Math.Round(length, 1), section = section ?? "Default", summary = $"frame {name} ({length:0} mm) added; snapshot in the result" };
