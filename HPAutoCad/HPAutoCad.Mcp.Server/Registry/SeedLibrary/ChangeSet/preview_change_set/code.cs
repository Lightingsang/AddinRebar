string changeSetId = args.Str("changeSetId");
int limit = args.Int("limit", 30);
int offset = args.Int("offset", 0);
return AecTools.PreviewChangeSet(db, ed, tr, units, ct, log, changeSetId, limit, offset);
