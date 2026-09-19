---
name: stj-utf8jsonwriter-never-flushes-midwalk
description: System.Text.Json fact for reviewing the hosts' result serializers — Serialize(Utf8JsonWriter over a Stream) buffers the whole value and writes the stream once at the final Flush; only the Stream overloads flush mid-walk (verified 2026-09-15, STJ 10.0.12, net48 + net10)
metadata:
  type: project
---

`JsonSerializer.Serialize(Utf8JsonWriter writer, …)` with a writer built over a `Stream` does **not** bound the walk: `WriteStack.FlushThreshold` is only set by the `Serialize(Stream, …)` overloads, and `Utf8JsonWriter.Grow` over a stream just grows its internal `ArrayBufferWriter`. The stream receives exactly one `Write(byte[],int,int)` at the final `Flush()`. Probe with a counting `IEnumerable<T>` and a throwing bounded `MemoryStream` (limit 64 KB, ~300-byte items): writer overload pulled 100 000/100 000 items; `Serialize(stream, …)` pulled 212 (flushes at ~90 % of `DefaultBufferSize`, 16 KB; set it to 4096 to stop nearer the limit). Same on net48 and net10. A `finally { writer.Dispose(); }` after a mid-walk converter exception can also *replace* that exception with the stream's limit exception (dispose flushes).

**Why:** HPNavis phase 2 (`NavisResultSerializer` + `BoundedOutputStream`) claimed "bounded while writing" with the writer overload; the unit test asserted output size only and passed, the live check on a small model ran in 0.3 s, so nothing caught it.

**How to apply:** whenever a bridge serializer (Revit `ResultSerializer`, AutoCAD, Navis) claims to cut cost — not just bytes — insist on the `Stream` overload and a test with a `yield`-based enumerable that counts pulls. On net48 the stream only needs `Write(byte[],int,int)` overridden (no Span overload exists there); on net8+ `MemoryStream.Write(ReadOnlySpan)` falls back to the array override for derived types.
