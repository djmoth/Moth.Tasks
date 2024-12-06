namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a queue of tasks to be run with an argument.
    /// </summary>
    /// <typeparam name="TArg">Type of argument to pass to the tasks.</typeparam>
    public class TaskQueue<TArg> : TaskQueue, ITaskQueue<TArg>
    {
        /// <inheritdoc />
        public new void Enqueue<TTask> (in TTask task)
            where TTask : struct, ITask<TArg>
            => EnqueueTask (task);

        /// <inheritdoc />
        public new void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask<TArg>
        {
            handle = CreateTaskHandle ();

            if (typeof (IDisposable).IsAssignableFrom (typeof (TTask))) // If T implements IDisposable
            {
                EnqueueTask (new DisposableTaskWithHandle<TaskWrapper<TTask, TArg>, TArg, Unit> (new TaskWrapper<TTask, TArg> (task), handle));
            } else
            {
                EnqueueTask (new TaskWithHandle<TaskWrapper<TTask, TArg>, TArg, Unit> (new TaskWrapper<TTask, TArg> (task), handle));
            }
        }

        /// <inheritdoc />
        public void RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default) => RunNextTask (arg, out _, profiler, token);

        /// <inheritdoc />
        public void RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            TaskRunWrapper taskRunWrapper = new TaskRunWrapper (arg);

            RunNextTask (ref taskRunWrapper, out exception, profiler, token);
        }

        /// <inheritdoc />
        public bool TryRunNextTask (TArg arg, IProfiler profiler = null) => TryRunNextTask (arg, out _, profiler);

        /// <inheritdoc />
        public bool TryRunNextTask (TArg arg, out Exception exception, IProfiler profiler = null)
        {
            TaskRunWrapper taskRunWrapper = new TaskRunWrapper (arg);

            return TryRunNextTask (ref taskRunWrapper, out exception, profiler);
        }

        private struct TaskRunWrapper : ITask<TaskRunWrapperArgs>
        {
            private readonly TArg arg;

            public TaskRunWrapper (TArg arg)
            {
                this.arg = arg;
            }

            public readonly void Run (TaskRunWrapperArgs args) => args.GetTaskMetadataRunnable<IRunnableTaskMetadata<TArg>> ().Run (args.Access, arg);
        }
    }
}
