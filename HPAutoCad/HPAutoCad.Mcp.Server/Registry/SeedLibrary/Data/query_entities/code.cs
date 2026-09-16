var filter = args.Obj("filter");
int limit = args.Int("limit", 100);
int offset = args.Int("offset", 0);
string mode = args.Str("mode", "summary");
var properties = args.Strings("properties");
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.QueryEntities(db, ed, tr, units, ct, log, filter, limit, offset, mode, properties, maxCandidates);
