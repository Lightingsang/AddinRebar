var filter = args.Obj("filter");
var sections = args.Strings("sections");
var tolerance = args.Obj("tolerance");
string ruleSet = args.Str("ruleSet");
string minSeverity = args.Str("minSeverity");
int limit = args.Int("limit", 100);
int offset = args.Int("offset", 0);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.AuditAecDrawing(db, ed, tr, units, ct, log, filter, sections, tolerance, ruleSet, minSeverity, limit, offset, maxCandidates);
