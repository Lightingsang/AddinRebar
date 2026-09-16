var filter = args.Obj("filter");
string ruleSet = args.Str("ruleSet");
var tolerance = args.Obj("tolerance");
double reachMm = args.Double("reachMm", 2500);
int limit = args.Int("limit", 100);
int offset = args.Int("offset", 0);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.StructuralDetectGrids(db, ed, tr, units, ct, log, filter, ruleSet, tolerance, reachMm, limit, offset, maxCandidates);
