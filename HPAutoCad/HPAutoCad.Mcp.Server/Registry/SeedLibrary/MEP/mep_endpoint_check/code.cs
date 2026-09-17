var filter = args.Obj("filter");
string ruleSet = args.Str("ruleSet");
var tolerance = args.Obj("tolerance");
var detection = args.Obj("detection");
bool includeConnected = args.Bool("includeConnected", false);
int limit = args.Int("limit", 150);
int offset = args.Int("offset", 0);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.MepEndpointCheck(db, ed, tr, units, ct, log, filter, ruleSet, tolerance, detection, includeConnected, limit, offset, maxCandidates);
