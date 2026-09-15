using System.IO;

namespace HPNavis.McpBridge.Service;

/// <summary>Thrown once by <see cref="BoundedOutputStream"/> when the output limit is reached; the walk over the value stops there.</summary>
public sealed class OutputLimitReachedException : Exception
{
    public OutputLimitReachedException(int limit) : base($"result exceeds {limit} bytes") { }
}

/// <summary>
///     A memory stream that keeps the first <c>limit</c> bytes and throws once when more arrives. A script may
///     return a collection of thousands of model items; serialising it fully on Navisworks' main thread
///     (bounding box and child count per item) only to cut the text afterwards is the cost this avoids —
///     the serializer aborts at the limit and reports the head. After the throw, further writes (the JSON
///     writer flushing on dispose) are ignored instead of throwing again.
/// </summary>
public sealed class BoundedOutputStream : MemoryStream
{
    private readonly int _limit;
    private bool _tripped;

    public BoundedOutputStream(int limit)
    {
        _limit = limit;
    }

    public bool LimitReached => _tripped;

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_tripped) return;

        var room = _limit - (int)Length;
        if (count <= room)
        {
            base.Write(buffer, offset, count);
            return;
        }

        if (room > 0) base.Write(buffer, offset, room);
        _tripped = true;
        throw new OutputLimitReachedException(_limit);
    }

    public override void WriteByte(byte value) => Write([value], 0, 1);
}
