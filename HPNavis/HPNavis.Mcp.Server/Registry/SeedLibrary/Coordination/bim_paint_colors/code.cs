// permanent colours of the colour sets in sheet order: preview, apply (+ read-back), verify or reset
string mode = args.Str("mode", "preview");
var codes = args.Strings("codes");
return HPNavis.BIMCoordinator.CoordinatorTools.PaintColors(doc, mode, codes, ct);