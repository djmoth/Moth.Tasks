namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    public unsafe class TaskQueue<TArg, TResult> : TaskQueue<TArg>, ITaskQueue<TArg, TResult>
    {
        public new void Enqueue<TTask> (in TTask task)
            where TTask : struct, ITask<TArg, TResult>
            => EnqueueTask (task);

        public new void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask<TArg, TResult>
        {
            handle = CreateTaskHandle ();

            if (typeof (IDisposable).IsAssignableFrom (typeof (TTask))) // If T implements IDisposable
            {
                EnqueueTask (new DisposableTaskWithHandle<TTask, TArg, TResult> (task, handle));
            } else
            {
                EnqueueTask (new TaskWithHandle<TTask, TArg, TResult> (task, handle));
            }
        }

        public TResult RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default) => RunNextTask (arg, out _, profiler, token);

        public TResult RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            TaskRunWrapper taskRunWrapper = new TaskRunWrapper (arg);

            RunNextTask (ref taskRunWrapper, out exception, profiler, token);

            return taskRunWrapper.Result;
        }

        public bool TryRunNextTask (TArg arg, out TResult result, IProfiler profiler = null) => TryRunNextTask (arg, out result, out _, profiler);

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

            public void Run (TaskRunWrapperArgs args) => Result = args.GetTaskInfoRunnable<IRunnableTaskInfo<TArg, TResult>> ().Run (args.Access, arg);
        }
    }
}
