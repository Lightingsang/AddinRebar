var filter = args.Obj("filter");
string ruleSet = args.Str("ruleSet");
var tolerance = args.Obj("tolerance");
var detection = args.Obj("detection");
int limit = args.Int("limit", 30);
int offset = args.Int("offset", 0);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.MepDetectNetwork(db, ed, tr, units, ct, log, filter, ruleSet, tolerance, detection, limit, offset, maxCandidates);
