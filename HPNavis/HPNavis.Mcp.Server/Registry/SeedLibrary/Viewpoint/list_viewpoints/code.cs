bool includeComments = args.Bool("includeComments", false);
var rows = new List<object>();

void Walk(SavedItem item, string path)
{
    ct.ThrowIfCancellationRequested();
    var here = path.Length == 0 ? item.DisplayName : path + " / " + item.DisplayName;
    if (item is SavedViewpoint viewpoint)
    {
        var position = viewpoint.Viewpoint.Position;
        rows.Add(new
        {
            name = viewpoint.DisplayName,
            guid = viewpoint.Guid,
            path = here,
            positionMm = new[] { Math.Round(units.ToMm(position.X), 1), Math.Round(units.ToMm(position.Y), 1), Math.Round(units.ToMm(position.Z), 1) },
            commentCount = viewpoint.Comments.Count,
            comments = includeComments ? viewpoint.Comments.Select(c => new { author = c.Author, status = c.Status.ToString(), body = c.Body, created = c.CreationDate }).ToList() : null,
        });
    }
    else if (item is GroupItem group)
    {
        foreach (var child in group.Children) Walk(child, here);
    }
}

foreach (var item in doc.SavedViewpoints.Value) Walk(item, "");
log($"{rows.Count} viewpoint(s)");
return rows;
