namespace Moth.Tasks
{
    using System;
    using System.Threading;

    /// <summary>
    /// Interface for a queue of tasks taking no arguments.
    /// </summary>
    public interface ITaskQueue : ITaskQueue<Unit>
    {
        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        void RunNextTask (IProfiler profiler = null, CancellationToken token = default);

        /// <inheritdoc cref="RunNextTask(IProfiler, CancellationToken)"/>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"/>
        /// <param name="token"/>
        void RunNextTask (out Exception exception, IProfiler profiler = null, CancellationToken token = default);

        /// <summary>
        /// Tries to run the next task in the queue, if present. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="ITaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        bool TryRunNextTask (IProfiler profiler = null);

        /// <summary>
        /// Tries to run the next task in the queue, if present. Provides an <see cref="Exception"/> thrown by the task, in case it fails.
        /// May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="TryRunNextTask(IProfiler)"/>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"/>
        bool TryRunNextTask (out Exception exception, IProfiler profiler = null);
    }
}