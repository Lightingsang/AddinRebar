var items = args.List("items");
string space = args.Str("space");
bool atomic = args.Bool("atomic", true);
return AecTools.CreateEntitiesBatch(db, ed, tr, units, ct, log, items, space, atomic);
