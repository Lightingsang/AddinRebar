// read-only inventory of saved sets and folders (a record of what existed before an apply)
string folder = args.Str("folder", "");
bool withConditions = args.Bool("withConditions", false);
return HPNavis.BIMCoordinator.CoordinatorTools.ListSelectionSets(doc, folder, withConditions);
