string changeSetId = args.Str("changeSetId");
bool atomic = args.Bool("atomic", true);
return AecTools.CommitChangeSet(db, ed, tr, units, ct, log, changeSetId, atomic);
