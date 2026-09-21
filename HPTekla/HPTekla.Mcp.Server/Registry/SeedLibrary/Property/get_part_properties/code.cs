int id = args.Int("objectId");
if (id <= 0) throw new ArgumentException("Valid objectId (> 0) is required.");
bool includeUda = args.Bool("includeUserProperties", true);

var mo = model.SelectModelObject(new Identifier(id));
if (mo == null) throw new ArgumentException($"Object ID {id} not found in Tekla model.");
if (mo is not Part part) throw new ArgumentException($"Object ID {id} is not a Part ({mo.GetType().Name}).");

double length = 0.0, weight = 0.0, volume = 0.0;
part.GetReportProperty("LENGTH", ref length);
part.GetReportProperty("WEIGHT", ref weight);
part.GetReportProperty("VOLUME", ref volume);

var udas = new Dictionary<string, object>();
if (includeUda)
{
    string comment = "", user1 = "", user2 = "";
    if (part.GetUserProperty("COMMENT", ref comment) && !string.IsNullOrWhiteSpace(comment)) udas["COMMENT"] = comment;
    if (part.GetUserProperty("USER_FIELD_1", ref user1) && !string.IsNullOrWhiteSpace(user1)) udas["USER_FIELD_1"] = user1;
    if (part.GetUserProperty("USER_FIELD_2", ref user2) && !string.IsNullOrWhiteSpace(user2)) udas["USER_FIELD_2"] = user2;
}

log($"Part {id}: Name={part.Name}, Profile={part.Profile.ProfileString}, Material={part.Material.MaterialString}");

return new
{
    success = true,
    id = part.Identifier.ID,
    guid = part.Identifier.GUID.ToString(),
    type = part.GetType().Name,
    name = part.Name,
    profile = part.Profile.ProfileString,
    material = part.Material.MaterialString,
    partClass = part.Class,
    position = new
    {
        plane = part.Position.Plane.ToString(),
        depth = part.Position.Depth.ToString(),
        rotation = part.Position.Rotation.ToString()
    },
    measurements = new
    {
        lengthMm = length,
        weightKg = weight,
        volumeM3 = volume / 1e9
    },
    udas = udas
};
