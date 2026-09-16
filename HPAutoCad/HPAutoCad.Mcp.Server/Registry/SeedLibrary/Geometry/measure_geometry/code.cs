string measure = args.Require("measure");
var handles = args.Strings("handles");
var points = args.List("points");
var tolerance = args.Obj("tolerance");
return AecTools.Measure(db, tr, units, ct, log, measure, handles, points, tolerance);
