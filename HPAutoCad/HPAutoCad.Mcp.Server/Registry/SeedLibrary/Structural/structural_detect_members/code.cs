var filter = args.Obj("filter");
var kinds = args.Strings("kinds");
string ruleSet = args.Str("ruleSet");
var prefixes = args.Obj("prefixes");
double minConfidence = args.Double("minConfidence", 0);
int limit = args.Int("limit", 100);
int offset = args.Int("offset", 0);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.StructuralDetectMembers(db, ed, tr, units, ct, log, filter, kinds, ruleSet, prefixes, minConfidence, limit, offset, maxCandidates);
