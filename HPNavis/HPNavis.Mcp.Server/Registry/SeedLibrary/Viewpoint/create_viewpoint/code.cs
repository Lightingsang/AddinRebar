string name = args.Require("name");
string comment = args.Str("comment", "") ?? "";
string author = args.Str("author", "MCP");

var saved = new SavedViewpoint(doc.CurrentViewpoint.ToViewpoint()) { DisplayName = name };
doc.SavedViewpoints.AddCopy(saved);
var added = doc.SavedViewpoints.Value.OfType<SavedViewpoint>().Last(v => v.DisplayName == name);
if (comment.Length > 0) doc.SavedViewpoints.AddComment(added, new Comment(comment, CommentStatus.New, author));

var position = added.Viewpoint.Position;
log($"viewpoint '{name}' saved" + (comment.Length > 0 ? " with a comment" : ""));
return new
{
    name,
    guid = added.Guid,
    positionMm = new[] { Math.Round(units.ToMm(position.X), 1), Math.Round(units.ToMm(position.Y), 1), Math.Round(units.ToMm(position.Z), 1) },
    commentCount = comment.Length > 0 ? 1 : 0,
};
