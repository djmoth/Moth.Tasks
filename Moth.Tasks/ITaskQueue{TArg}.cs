namespace Moth.Tasks
{
    using System;
    using System.Threading;

    /// <summary>
    /// Interface for a queue of tasks taking an argument of type <typeparamref name="TArg"/>.
    /// </summary>
    /// <typeparam name="TArg">Type of argument that tasks will take.</typeparam>
    public interface ITaskQueue<TArg>
    {
        /// <summary>
        /// Enqueues an <see cref="ITask{TArg}"/> to be run later.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue.Enqueue{TTask}(in TTask)"/>
        void Enqueue<TTask> (in TTask task) where TTask : struct, ITask<TArg>;

        ///<summary>
        /// Enqueues an <see cref="ITask{TArg}"/> to be run later, giving out a <see cref="TaskHandle"/> for checking task status.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue.Enqueue{TTask}(in TTask, out TaskHandle)"/>
        void Enqueue<TTask> (in TTask task, out TaskHandle handle) where TTask : struct, ITask<TArg>;

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it with <paramref name="arg"/> as argument. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue.RunNextTask(IProfiler, CancellationToken)"/>
        /// <param name="arg">Argument to run task with.</param>
        /// <param name="profiler"/>
        /// <param name="token"/>
        void RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default);

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it with <paramref name="arg"/> as argument. Provides an <see cref="Exception"/> thrown by the task, in case it fails. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue.RunNextTask(out Exception, IProfiler, CancellationToken)"/>
        /// <inheritdoc cref="RunNextTask(TArg, IProfiler, CancellationToken)"/>
        /// <param name="arg"/>
        /// <param name="exception"/>
        /// <param name="profiler"/>
        /// <param name="token"/>
        void RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default);

        /// <summary>
        /// Tries to run the next task in the queue, if present, with <paramref name="arg"/> as argument. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue.TryRunNextTask(IProfiler)"/>
        /// <param name="arg">Argument to run task with.</param>
        /// <param name="profiler"/>
        /// <returns>
        /// <see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.
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
