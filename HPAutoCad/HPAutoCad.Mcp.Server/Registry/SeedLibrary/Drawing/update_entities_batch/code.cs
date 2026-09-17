var items = args.List("items");
var handles = args.Strings("handles");
var set = args.Obj("set");
bool atomic = args.Bool("atomic", true);
string changeSetId = args.Str("changeSetId");
return AecTools.UpdateEntitiesBatch(db, ed, tr, units, ct, log, items, handles, set, atomic, changeSetId, args);
