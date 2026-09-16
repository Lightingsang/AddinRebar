var source = args.Obj("source");
var target = args.Obj("target");
string relation = args.Require("relation");
double? maxDistance = args.DoubleOrNull("maxDistance");
var tolerance = args.Obj("tolerance");
int limit = args.Int("limit", 100);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.QuerySpatial(db, ed, tr, units, ct, log, source, target, relation, maxDistance, tolerance, limit, maxCandidates);
