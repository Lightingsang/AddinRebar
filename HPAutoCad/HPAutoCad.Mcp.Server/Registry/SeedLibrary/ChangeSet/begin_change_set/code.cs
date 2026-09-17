string label = args.Str("label");
return AecTools.BeginChangeSet(db, ed, tr, units, ct, log, label);
