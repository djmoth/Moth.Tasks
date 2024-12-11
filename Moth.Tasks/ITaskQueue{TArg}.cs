namespace Moth.Tasks
{
    using System;
    using System.Threading;

    /// <summary>
    /// Interface for a queue of tasks taking an argument of type <typeparamref name="TArg"/>.
    /// </summary>
    /// <typeparam name="TArg">Type of argument that tasks will take.</typeparam>
    public interface ITaskQueue<TArg> : ITaskQueue<TArg, Unit>
    {
        /// <summary>
        /// Tries to run the next task in the queue, if present, with <paramref name="arg"/> as argument. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue.TryRunNextTask(IProfiler)"/>
        /// <param name="arg">Argument to run task with.</param>
        /// <param name="profiler"/>
        /// <returns>
        /// <see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="ITaskQueue{TArg}"/> is empty.
        /// </returns>
        /// <remarks>
        /// Note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        bool TryRunNextTask (TArg arg, IProfiler profiler = null);

        /// <summary>
        /// Tries to run the next task in the queue, if present. Provides an <see cref="Exception"/> thrown by the task, in case it fails. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue.TryRunNextTask(out Exception, IProfiler)"/>
        /// <inheritdoc cref="TryRunNextTask(TArg, IProfiler)"/>
        /// <param name="arg"/>
        /// <param name="exception"/>
        /// <param name="profiler"/>
        bool TryRunNextTask (TArg arg, out Exception exception, IProfiler profiler = null);
    }
}
