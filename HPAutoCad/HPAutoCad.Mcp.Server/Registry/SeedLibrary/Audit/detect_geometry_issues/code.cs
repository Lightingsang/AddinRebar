var filter = args.Obj("filter");
var handles = args.Strings("handles");
var issueTypes = args.Strings("issueTypes");
var tolerance = args.Obj("tolerance");
int limit = args.Int("limit", 100);
int maxCandidates = args.Int("maxCandidates", 5000);
return AecTools.DetectGeometryIssues(db, ed, tr, units, ct, log, filter, handles, issueTypes, tolerance, limit, maxCandidates);
