string op = args.Str("op");
var names = args.Strings("names");
string namePattern = args.Str("namePattern");
var attach = args.Obj("attach");
bool insertBind = args.Bool("insertBind", false);
string space = args.Str("space");
string changeSetId = args.Str("changeSetId");
return AecTools.ManageXrefs(db, ed, tr, units, ct, log, op, names, namePattern, attach, insertBind, space, changeSetId, args);
