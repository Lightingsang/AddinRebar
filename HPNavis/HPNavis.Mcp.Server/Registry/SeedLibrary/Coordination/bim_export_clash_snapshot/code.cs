// heavy: writes the clash snapshot (and the Navisworks HTML report with its pictures) beside the model; the bridge's heavy gate sees ExportClashSnapshot here
string lod = args.Str("lod", "auto");
bool images = args.Bool("images", true);
int maxPictures = args.Int("maxPictures", 500);
if (!app.HasClashModule) throw new InvalidOperationException("Clash Detective is not available in this Navisworks (Simulate); the export needs Navisworks Manage.");
return HPNavis.BIMCoordinator.CoordinatorTools.ExportClashSnapshot(doc, lod, units.ToMm(1), images, maxPictures, ct);
