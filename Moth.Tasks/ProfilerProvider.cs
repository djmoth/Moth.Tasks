﻿namespace Moth.Tasks
{
    /// <summary>
    /// Represents a method that provides an <see cref="IProfiler"/> for a <see cref="Worker"/>, or <see langword="null"/> if profiling is not desired.
    /// </summary>
    /// <param name="worker">The <see cref="Worker"/>.</param>
    /// <returns>An <see cref="IProfiler"/> for the <paramref name="worker"/>, or <see langword="null"/> if profiling is not desired.</returns>
    /// <remarks>
    /// The method does not have to provide a unique <see cref="IProfiler"/> for each <see cref="Worker"/>, yet in that case it must be able to differentiate between <see cref="Worker"/>s on its own, so as their calls to <see cref="IProfiler.BeginTask"/> and <see cref="IProfiler.EndTask"/> don't interfere.
    /// </remarks>
    public delegate IProfiler ProfilerProvider (Worker worker);
}
