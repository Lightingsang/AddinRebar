bool includeLayouts = args.Bool("includeLayouts", true);
bool includeLayers = args.Bool("includeLayers", false);
return AecTools.DrawingContext(doc, db, ed, tr, units, ct, log, includeLayouts, includeLayers);
