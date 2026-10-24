using HPBimToolkit.Core.Contracts;

namespace HPBimToolkit.Core.Statistics;

/// <summary>
///     Sums element changes since the statistics were last read, so the page can say its numbers are stale without the
///     add-in re-counting the model after every edit.
/// </summary>
public sealed class ModelChangeTally
{
    private int _added;
    private int _deleted;
    private int _modified;

    public bool HasChanges => _added + _deleted + _modified > 0;

    public void Record(int added, int deleted, int modified)
    {
        _added += added;
        _deleted += deleted;
        _modified += modified;
    }

    public void Reset()
    {
        _added = 0;
        _deleted = 0;
        _modified = 0;
    }

    public ModelStaleEvent ToEvent() => new(_added, _deleted, _modified);
}
