namespace Moth.Tasks
{
    using System;
    using System.Threading;

    /// <summary>
    /// Interface for a queue of tasks taking an argument of type <typeparamref name="TArg"/> and returning a result of <typeparamref name="TResult"/>.
    /// </summary>
    /// <inheritdoc cref="ITaskQueue{TArg}"/>
    /// <typeparam name="TArg"/>
    /// <typeparam name="TResult">Type of result that tasks will return.</typeparam>
    public interface ITaskQueue<TArg, TResult>
    {
        /// <summary>
        /// Enqueues an <see cref="ITask{TArg, TResult}"/> to be run later.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue{TArg}.Enqueue{TTask}(in TTask)"/>
        void Enqueue<TTask> (in TTask task) where TTask : struct, ITask<TArg, TResult>;

        /// <summary>
        /// Enqueues an <see cref="ITask{TArg, TResult}"/> to be run later, giving out a <see cref="TaskHandle"/> for checking task status.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue{TArg}.Enqueue{TTask}(in TTask, out TaskHandle)"/>
        void Enqueue<TTask> (in TTask task, out TaskHandle handle) where TTask : struct, ITask<TArg, TResult>;

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it and returns the result. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue{TArg}.RunNextTask(TArg, IProfiler, CancellationToken)"/>
        /// <param name="arg"/>
        /// <param name="profiler"/>
        /// <param name="token"/>
        /// <returns>Result of the task.</returns>
        TResult RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default);

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it with <paramref name="arg"/> as argument and returns the result. Provides an <see cref="Exception"/> thrown by the task, in case it fails. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue{TArg}.RunNextTask(TArg, out Exception, IProfiler, CancellationToken)"/>
        /// <inheritdoc cref="RunNextTask(TArg, IProfiler, CancellationToken)"/>/>
        /// <param name="arg"/>
        /// <param name="exception"/>
        /// <param name="profiler"/>
        /// <param name="token"/>
        TResult RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default);

        /// <summary>
        /// Tries to run the next task in the queue, if present, with <paramref name="arg"/> as argument and returns the result through <paramref name="result"/>. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue{TArg}.TryRunNextTask(TArg, IProfiler)"/>
        /// <param name="arg"/>
        /// <param name="result">Result of the task.</param>
        /// <param name="profiler"/>
        bool TryRunNextTask (TArg arg, out TResult result, IProfiler profiler = null);

        /// <summary>
        /// Tries to run the next task in the queue, if present, with <paramref name="arg"/> as argument and returns the result through <paramref name="result"/>. Provides an <see cref="Exception"/> thrown by the task, in case it fails. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <inheritdoc cref="ITaskQueue{TArg}.TryRunNextTask(TArg, out Exception, IProfiler)"/>
        /// <inheritdoc cref="TryRunNextTask(TArg, out TResult, IProfiler)"/>
        /// <param name="arg"/>
        /// <param name="result"/>
        /// <param name="exception"/>
        /// <param name="profiler"/>
        bool TryRunNextTask (TArg arg, out TResult result, out Exception exception, IProfiler profiler = null);
    }
}
