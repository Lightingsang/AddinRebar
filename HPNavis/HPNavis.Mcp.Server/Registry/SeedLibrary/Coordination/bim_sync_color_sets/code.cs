// preview (default) or apply the colour search sets of sheet ColorSearchSet(DSC); a differing saved set is a conflict unless approved
bool apply = args.Bool("apply", false);
bool allowUpdate = args.Bool("allowUpdate", false);
var codes = args.Strings("codes");
return HPNavis.BIMCoordinator.CoordinatorTools.SyncColorSets(doc, apply, allowUpdate, codes, units.ToMm(1), ct);