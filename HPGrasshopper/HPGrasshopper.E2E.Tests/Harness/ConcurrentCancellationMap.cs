namespace HPGrasshopper.E2E.Tests.Harness;

using System.Collections.Generic;
using System.Threading;

internal sealed class ConcurrentCancellationMap
{
    private readonly Dictionary<long, CancellationTokenSource> _map = new();
    private readonly object _lock = new();

    public void Register(long id, CancellationTokenSource cts)
    {
        lock (_lock) { _map[id] = cts; }
    }

    public void Unregister(long id)
    {
        lock (_lock) { _map.Remove(id); }
    }

    public bool Cancel(long id)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(id, out var cts))
            {
                cts.Cancel();
                return true;
            }
            return false;
        }
    }

    public void CancelAll()
    {
        lock (_lock)
        {
            foreach (var cts in _map.Values) cts.Cancel();
        }
    }
}
