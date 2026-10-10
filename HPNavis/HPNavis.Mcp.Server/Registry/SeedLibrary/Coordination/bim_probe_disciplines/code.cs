// survey only: files and their ISO role, the two properties the base sets rely on, categories per discipline
int maxItems = args.Int("maxItemsPerDiscipline", 200000);
return HPNavis.BIMCoordinator.CoordinatorTools.ProbeModel(doc, maxItems, ct);
