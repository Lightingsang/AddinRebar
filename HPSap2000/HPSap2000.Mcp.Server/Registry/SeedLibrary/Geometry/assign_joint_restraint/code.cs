var pointNames = args.Strings("pointNames").Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
if (pointNames.Count == 0) throw new ArgumentException("pointNames must name at least one point");
string type = (args.Str("type", "fixed") ?? "fixed").Trim().ToLowerInvariant();

bool[] values = type switch
{
    "fixed" => new[] { true, true, true, true, true, true },
    "pinned" => new[] { true, true, true, false, false, false },
    "roller" => new[] { false, false, true, false, false, false },
    "free" => new[] { false, false, false, false, false, false },
    "custom" => args.List("restraints").Select(a => a.AsBool()).ToArray() switch
    {
        { Length: 6 } r => r,
        _ => throw new ArgumentException("custom restraint requires a 'restraints' array of exactly 6 booleans [u1, u2, u3, r1, r2, r3]")
    },
    _ => throw new ArgumentException("type must be 'fixed', 'pinned', 'roller', 'free', or 'custom'")
};

var affected = new List<string>();
var errors = new List<object>();
foreach (var name in pointNames)
{
    ct.ThrowIfCancellationRequested();
    bool[] v = (bool[])values.Clone();
    int ret = sapModel.PointObj.SetRestraint(name, ref v, eItemType.Objects);
    if (ret == 0) affected.Add(name);
    else errors.Add(new { code = "SAP2000_RET", message = $"SAP2000 returned {ret} from PointObj.SetRestraint({name})", name });
}

log($"restraint {type} assigned to {affected.Count} of {pointNames.Count} points");
return new { success = errors.Count == 0, modifiedCount = affected.Count, affectedNames = affected, type, restraints = values, errors, summary = $"{type} restraint assigned to {affected.Count} point(s); snapshot in the result" };
