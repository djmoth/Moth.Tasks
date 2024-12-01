namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public class TaskQueue<TArg> : TaskQueue, ITaskQueue<TArg>
    {
        public new void Enqueue<TTask> (in TTask task)
            where TTask : struct, ITask<TArg>
            => EnqueueTask (task);

        public new void Enqueue<TTask> (in TTask task, out TaskHandle handle)
            where TTask : struct, ITask<TArg>
        {
            handle = CreateTaskHandle ();

            if (typeof (IDisposable).IsAssignableFrom (typeof (TTask))) // If T implements IDisposable
            {
                EnqueueTask (new DisposableTaskWithHandle<Task<TTask, TArg>, TArg, Unit> (new Task<TTask, TArg> (task), handle));
            } else
            {
                EnqueueTask (new TaskWithHandle<Task<TTask, TArg>, TArg, Unit> (new Task<TTask, TArg> (task), handle));
            }
        }

        public void RunNextTask (TArg arg, IProfiler profiler = null, CancellationToken token = default) => RunNextTask (arg, out _, profiler, token);

        public void RunNextTask (TArg arg, out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            TaskRunWrapper taskRunWrapper = new TaskRunWrapper (arg);

            RunNextTask (ref taskRunWrapper, out exception, profiler, token);
        }

        public bool TryRunNextTask (TArg arg, IProfiler profiler = null) => TryRunNextTask (arg, out _, profiler);

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

            public readonly void Run (TaskRunWrapperArgs args) => args.GetTaskInfoRunnable<IRunnableTaskInfo<TArg>> ().Run (args.Access, arg);
        }
    }
}
