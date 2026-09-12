using Microsoft.CodeAnalysis.Scripting;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Least-recently-used cache of compiled scripts keyed by source hash. Compiling costs 0.5–2 s and each
///     compilation is a dynamic assembly that never unloads, so re-running the same script must not pay
///     twice; a small bound keeps memory growth proportional to distinct scripts, not to runs.
/// </summary>
public sealed class ScriptCache
{
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<Entry>> _index = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _order = new();
    private readonly object _gate = new();

    public ScriptCache(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public int Count
    {
        get { lock (_gate) return _index.Count; }
    }

    public bool TryGet(string key, out Script<object> script)
    {
        lock (_gate)
        {
            if (_index.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _order.AddFirst(node);
                script = node.Value.Script;
                return true;
            }
        }

        script = null!;
        return false;
    }

    public void Add(string key, Script<object> script)
    {
        lock (_gate)
        {
            if (_index.ContainsKey(key)) return;

            var node = new LinkedListNode<Entry>(new Entry(key, script));
            _order.AddFirst(node);
            _index[key] = node;

            while (_index.Count > _capacity)
            {
                var oldest = _order.Last!;
                _order.RemoveLast();
                _index.Remove(oldest.Value.Key);
            }
        }
    }

    private sealed record Entry(string Key, Script<object> Script);
}
