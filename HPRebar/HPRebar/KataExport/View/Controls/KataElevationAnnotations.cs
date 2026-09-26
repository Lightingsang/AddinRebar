using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Draws the texts of the elevation, each one the text of a Kata cell: column letters, the support/span chain
/// (row 11), the grid-to-grid chain, span captions (section, z offset = row 19, soffit step = row 21), the column
/// above (row 19 "width;offset"), grid offsets (row 23) and crossing-beam offsets (row 21). A text that does not
/// fit, or would overlap its neighbour, is left out until the user zooms in.
/// </summary>
internal sealed class KataElevationAnnotations
{
    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;

    public KataElevationAnnotations(KataElevationScene scene, KataCanvasPalette palette, KataDrawPrimitives draw)
    {
        _scene = scene;
        _palette = palette;
        _draw = draw;
    }

    public void Paint()
    {
        PaintUpperColumns();
        PaintGridOffsets();
        PaintCrossingOffsets();
        PaintLetters();
        PaintColumnChain();
        PaintGridChain();
        PaintCaptions();
    }

    private void PaintUpperColumns()
    {
        var lane = new KataLabelLane();
        foreach (var support in _scene.Elevation.Supports)
        {
            if (support.Upper is not { } upper || upper.Text.Length == 0) continue;

            var (topMm, _) = _scene.BeamFaces(support.Extent);
            double x = _scene.X(upper.Extent.Mid);
            var text = _draw.Text(upper.Text, _palette.Text, KataDrawPrimitives.SmallTextSize);
            PlaceCentered(lane, text, x, _scene.Y(topMm) - KataElevationScene.UpperStubPx - text.Height - 1);
        }
    }

    private void PaintGridOffsets()
    {
        var lane = new KataLabelLane();
        double top = _scene.BubbleY + KataElevationScene.BubbleRadius + 2;
        foreach (var grid in _scene.Elevation.Grids)
        {
            if (!IsNonZero(grid.OffsetText)) continue;

            var text = _draw.Text($"lệch {grid.OffsetText}", _palette.Accent, KataDrawPrimitives.SmallTextSize);
            double left = _scene.X(grid.X) + 3;
            if (!_scene.IsVisible(left, left + text.Width) || !lane.TryPlace(left, left + text.Width)) continue;
            _draw.At(text, left, top);
        }
    }

    private void PaintCrossingOffsets()
    {
        var lane = new KataLabelLane();
        foreach (var support in _scene.Elevation.Supports)
        {
            if (support.CrossingBeamX is not { } crossing) continue;

            string row21 = _scene.Elevation.Columns[support.ColumnIndex].Row21;
            if (!IsNonZero(row21)) continue;

            var text = _draw.Text($"giao {row21}", _palette.MutedText, KataDrawPrimitives.SmallTextSize);
            PlaceCentered(lane, text, _scene.X(crossing), _scene.MarkerTextY);
        }
    }

    /// <summary>The selected column's letter is always shown; its neighbours give way to it.</summary>
    private void PaintLetters()
    {
        var columns = _scene.Elevation.Columns;
        double reservedLeft = double.NaN, reservedRight = double.NaN;
        if (_scene.SelectedColumn >= 0 && _scene.SelectedColumn < columns.Count)
        {
            var selected = columns[_scene.SelectedColumn];
            var text = _draw.Text(selected.Letter, _palette.Selected, KataDrawPrimitives.TextSize, bold: true);
            double x = _scene.X(selected.Extent.Mid);
            reservedLeft = x - text.Width / 2.0 - 4.0;
            reservedRight = x + text.Width / 2.0 + 4.0;
            _draw.Centered(text, x, _scene.LetterY - 1);
        }

        var lane = new KataLabelLane();
        foreach (var column in columns)
        {
            if (column.Index == _scene.SelectedColumn) continue;

            var text = _draw.Text(column.Letter, _palette.MutedText, KataDrawPrimitives.SmallTextSize);
            double x = _scene.X(column.Extent.Mid);
            if (x + text.Width / 2.0 > reservedLeft && x - text.Width / 2.0 < reservedRight) continue;
            PlaceCentered(lane, text, x, _scene.LetterY);
        }
    }

    /// <summary>Row 11: every support width (or "b x h" of a crossing beam) and span length, tick to tick.</summary>
    private void PaintColumnChain()
    {
        var columns = _scene.Elevation.Columns;
        double y = _scene.ChainY;
        _draw.Line(_palette.Dimension, _scene.X(columns[0].Extent.Start), y, _scene.X(columns[columns.Count - 1].Extent.End), y);

        foreach (var column in columns)
        {
            double left = _scene.X(column.Extent.Start), right = _scene.X(column.Extent.End);
            if (!_scene.IsVisible(left - 5, right + 5)) continue;

            _draw.Tick(_palette.Dimension, left, y);
            _draw.Tick(_palette.Dimension, right, y);
            if (column.IsZeroWidth || column.Row11.Length == 0) continue;

            var text = _draw.Text(column.Row11, _palette.Text);
            if (text.Width + 4 <= right - left) _draw.Centered(text, (left + right) / 2.0, y - text.Height - 1);
        }
    }

    private void PaintGridChain()
    {
        var dimensions = _scene.Elevation.GridDimensions;
        if (dimensions.Count == 0) return;

        double y = _scene.GridChainY;
        _draw.Line(_palette.Dimension, _scene.X(dimensions[0].Extent.Start), y, _scene.X(dimensions[dimensions.Count - 1].Extent.End), y);
        foreach (var dimension in dimensions)
        {
            double left = _scene.X(dimension.Extent.Start), right = _scene.X(dimension.Extent.End);
            if (!_scene.IsVisible(left - 5, right + 5)) continue;

            _draw.Tick(_palette.Dimension, left, y);
            _draw.Tick(_palette.Dimension, right, y);
            var text = _draw.Text(dimension.Text, _palette.MutedText);
            if (text.Width + 4 <= right - left) _draw.Centered(text, (left + right) / 2.0, y - text.Height - 1);
        }
    }

    /// <summary>"Nhịp n – b x h" under each span (just "n" when narrow), then its z offset and soffit step when not zero.</summary>
    private void PaintCaptions()
    {
        var titles = new KataLabelLane();
        var details = new KataLabelLane();
        foreach (var column in _scene.Elevation.Columns)
        {
            if (column.Kind != KataColumnKind.Span) continue;

            double left = _scene.X(column.Extent.Start), right = _scene.X(column.Extent.End), mid = (left + right) / 2.0;
            if (!_scene.IsVisible(left, right)) continue;

            var title = _draw.Text($"Nhịp {column.SpanNumber} – {column.SpanSection}", _palette.Text, KataDrawPrimitives.SmallTextSize, bold: true);
            if (title.Width + 4 > right - left) title = _draw.Text($"{column.SpanNumber}", _palette.Text, KataDrawPrimitives.SmallTextSize, bold: true);
            PlaceCentered(titles, title, mid, _scene.CaptionY);

            var parts = new List<string>(2);
            if (IsNonZero(column.Row19)) parts.Add($"z {column.Row19}");
            if (IsNonZero(column.Row21)) parts.Add($"đáy {column.Row21}");
            if (parts.Count == 0) continue;

            var detail = _draw.Text(string.Join(" · ", parts), _palette.MutedText, KataDrawPrimitives.SmallTextSize);
            if (detail.Width + 4 <= right - left) PlaceCentered(details, detail, mid, _scene.CaptionY + title.Height + 1);
        }
    }

    private void PlaceCentered(KataLabelLane lane, System.Windows.Media.FormattedText text, double x, double top)
    {
        double left = x - text.Width / 2.0, right = x + text.Width / 2.0;
        if (!_scene.IsVisible(left, right) || !lane.TryPlace(left, right)) return;
        _draw.Centered(text, x, top);
    }

    private static bool IsNonZero(string? cell) => !string.IsNullOrEmpty(cell) && cell != "0";
}
