// preview (default) or apply the search-set registry; a differing saved set is a conflict unless allowUpdate (approval)
bool apply = args.Bool("apply", false);
bool allowUpdate = args.Bool("allowUpdate", false);
bool includeExtras = args.Bool("includeExtras", false);
var codes = args.Strings("codes");
return HPNavis.BIMCoordinator.CoordinatorTools.SyncSearchSets(doc, apply, allowUpdate, codes, includeExtras, units.ToMm(1), ct);
