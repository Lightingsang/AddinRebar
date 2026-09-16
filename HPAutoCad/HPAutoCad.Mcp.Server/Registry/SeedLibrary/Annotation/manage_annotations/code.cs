string op = args.Str("op");
var annotation = args.Obj("annotation");
var handles = args.Strings("handles");
var items = args.List("items");
var set = args.Obj("set");
string space = args.Str("space");
bool atomic = args.Bool("atomic", true);
return AecTools.ManageAnnotations(db, ed, tr, units, ct, log, op, annotation, handles, items, set, space, atomic);
