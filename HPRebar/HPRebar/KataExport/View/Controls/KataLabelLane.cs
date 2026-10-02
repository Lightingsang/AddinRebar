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

    /// <summary>
    /// Places a label of <paramref name="width"/> as near <paramref name="left"/> as it fits, sliding it right or left
    /// in steps up to <paramref name="maxShift"/>; returns where it went, or null when the row is full there.
    /// </summary>
    public double? PlaceNear(double left, double width, double maxShift, double step = 6.0)
    {
        if (step <= 0.0) throw new System.ArgumentOutOfRangeException(nameof(step));
        for (double shift = 0.0; shift <= maxShift; shift += step)
        {
            if (TryPlace(left + shift, left + shift + width)) return left + shift;
            if (shift > 0.0 && TryPlace(left - shift, left - shift + width)) return left - shift;
        }

        return null;
    }
}
