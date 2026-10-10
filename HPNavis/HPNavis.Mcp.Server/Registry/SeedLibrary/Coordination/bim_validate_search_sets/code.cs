// read-only acceptance check of the registry against the open model
string scope = args.Str("scope", "base");
return HPNavis.BIMCoordinator.CoordinatorTools.ValidateSearchSets(doc, scope, units.ToMm(1), ct);
