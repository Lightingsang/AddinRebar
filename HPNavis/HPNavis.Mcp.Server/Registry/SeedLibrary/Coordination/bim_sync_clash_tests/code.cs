// preview (default) or upsert one Hard clash test per matrix rule eligible at the LOD
string lod = args.Str("lod", "350");
bool apply = args.Bool("apply", false);
var priorities = args.Longs("priorities").Select(p => (int)p).ToList();
var ruleIds = args.Strings("ruleIds");
bool listUnchanged = args.Bool("listUnchanged", false);
return HPNavis.BIMCoordinator.CoordinatorTools.SyncClashTests(doc, lod, apply, units.ToMm(1), priorities, ruleIds, listUnchanged, ct);
