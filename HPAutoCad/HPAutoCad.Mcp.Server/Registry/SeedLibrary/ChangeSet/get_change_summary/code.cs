string changeSetId = args.Str("changeSetId");
return AecTools.GetChangeSummary(db, ed, tr, units, ct, log, changeSetId);
