namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using Validation;

    public class Worker : IDisposable
    {
        private TaskQueue tasks;
        private Thread thread;
        private CancellationTokenSource cancel;
        private IProfiler profiler;
        private EventHandler<TaskExceptionEventArgs> exceptionEvent;

        public Worker () : this (new TaskQueue (), new CancellationTokenSource ()) { }

        public Worker (TaskQueue taskQueue, CancellationTokenSource cancellationTokenSource, EventHandler<TaskExceptionEventArgs> exceptionEvent = null, IProfiler profiler = null)
        {
            tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue));
            cancel = cancellationTokenSource ?? throw new ArgumentNullException (nameof (cancellationTokenSource));
            this.profiler = profiler;

            thread = new Thread (Work);
        }

        private void Work (object workerObj)
        {
            Worker worker = (Worker)workerObj;

            while (!cancel.IsCancellationRequested)
            {
                bool ranTask = tasks.RunNextTask (profiler, out Exception exception);

                if (exception != null)
                {
                    exceptionEvent?.Invoke (workerObj, new TaskExceptionEventArgs (exception));

                }
            }
        }
    }
}
