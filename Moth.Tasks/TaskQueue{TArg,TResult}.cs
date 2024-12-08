namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a queue of tasks to be run with an argument and return a result.
    /// </summary>
    /// <typeparam name="TArg">Type of argument to pass to the tasks.</typeparam>
    /// <typeparam name="TResult">Type of the task results.</typeparam>
    public unsafe class TaskQueue<TArg, TResult> : TaskQueue<TArg>, ITaskQueue<TArg, TResult>
    {
        /// <inheritdoc />
        public new void Enqueue<TTask> (in TTask task)
            where TTask : struct, ITask<TArg, TResult>
            => EnqueueTask (task);

        /// <inheritdoc />
        public new void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask<TArg, TResult>
        {
            handle = CreateTaskHandle ();

            EnqueueTask (new TaskWithHandle<TTask, TArg, TResult> (task, handle));
        }

        /// <inheritdoc />
        public new TResult RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default) => RunNextTask (arg, out _, profiler, token);

        /// <inheritdoc />
        public new TResult RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            TaskRunWrapper taskRunWrapper = new TaskRunWrapper (arg);

            RunNextTask (ref taskRunWrapper, out exception, profiler, token);

            return taskRunWrapper.Result;
        }

        /// <inheritdoc />
        public bool TryRunNextTask (TArg arg, out TResult result, IProfiler profiler = null) => TryRunNextTask (arg, out result, out _, profiler);

        /// <inheritdoc />
        public bool TryRunNextTask (TArg arg, out TResult result, out Exception exception, IProfiler profiler = null)
        {
            TaskRunWrapper taskRunWrapper = new TaskRunWrapper (arg);

            bool taskWasRun = TryRunNextTask (ref taskRunWrapper, out exception, profiler);

            result = taskRunWrapper.Result;
            return taskWasRun;
        }

        [StructLayout (LayoutKind.Auto)]
        private struct TaskRunWrapper : ITask<TaskRunWrapperArgs>
        {
            public TResult Result;
            private readonly TArg arg;

            public TaskRunWrapper (TArg arg)
            {
                this.arg = arg;
                Result = default;
            }

            public void Run (TaskRunWrapperArgs args) => args.GetTaskMetadataRunnable<IRunnableTaskMetadata<TArg, TResult>> ().Run (args.Access, arg, out Result);
        }
    }
}
