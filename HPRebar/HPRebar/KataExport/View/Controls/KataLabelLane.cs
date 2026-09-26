namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// One horizontal row of labels: a label is drawn only if it stays clear of every label already placed, so a
/// zoomed-out run shows fewer labels instead of overlapping ones. Labels placed first win, whatever their order.
/// </summary>
internal sealed class KataLabelLane
{
    private const double Gap = 4.0;
    private readonly List<(double Left, double Right)> _placed = new();

    public bool TryPlace(double left, double right)
    {
        foreach (var (placedLeft, placedRight) in _placed)
        {
            if (left < placedRight + Gap && right > placedLeft - Gap) return false;
        }

        _placed.Add((left, right));
        return true;
    }
}
