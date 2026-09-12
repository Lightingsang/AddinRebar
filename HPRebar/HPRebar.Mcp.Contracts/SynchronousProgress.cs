using System;

namespace HPRebar.Mcp.Contracts;

/// <summary>
///     <see cref="IProgress{T}"/> that invokes the callback on the reporting thread, in call order.
///     <see cref="Progress{T}"/> would post each report to the thread pool, which is exactly how a
///     3-step script ends up delivering "step 2, step 3, step 1" to the client.
/// </summary>
public sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
