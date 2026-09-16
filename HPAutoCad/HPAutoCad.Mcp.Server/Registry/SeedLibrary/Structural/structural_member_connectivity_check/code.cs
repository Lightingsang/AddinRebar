var filter = args.Obj("filter");
string ruleSet = args.Str("ruleSet");
var tolerance = args.Obj("tolerance");
int limit = args.Int("limit", 100);
int offset = args.Int("offset", 0);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.StructuralMemberConnectivityCheck(db, ed, tr, units, ct, log, filter, ruleSet, tolerance, limit, offset, maxCandidates);
