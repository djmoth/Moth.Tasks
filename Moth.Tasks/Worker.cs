namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using Validation;

    public class Worker : IDisposable
    {
        private readonly TaskQueue tasks;
        private readonly bool disposeTasks;
        private readonly Thread thread;
        private readonly CancellationToken sharedCancel;
        private readonly CancellationTokenSource localCancelSource;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;

        public Worker () : this (new TaskQueue (), CancellationToken.None) { }

        public Worker (TaskQueue taskQueue, CancellationToken sharedCancelToken, EventHandler<TaskExceptionEventArgs> exceptionEventHandler = null, IProfiler profiler = null, bool disposeTaskQueue = false)
        {
            tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue));
            this.disposeTasks = disposeTaskQueue;

            localCancelSource = new CancellationTokenSource ();
            sharedCancel = sharedCancelToken != CancellationToken.None ? sharedCancel : localCancelSource.Token;

            this.exceptionEventHandler = exceptionEventHandler;
            this.profiler = profiler;

            thread = new Thread (Work);
            thread.Start ();
        }

        public bool IsRunning { get; private set; } = true;

        public bool IsCancelledRequested => sharedCancel.IsCancellationRequested || localCancelSource.IsCancellationRequested;

        private void Work ()
        {
            try
            {
                while (!IsCancelledRequested)
                {
                    bool ranTask = tasks.RunNextTask (profiler, out Exception exception);

                    if (exception != null)
                    {
                        exceptionEventHandler?.Invoke (this, new TaskExceptionEventArgs (exception));
                    }
                }
            } catch (Exception ex)
            {
                Trace.TraceError ("Internal exception in Worker: " + ex.ToString ());
                Dispose ();
            } finally
            {
                IsRunning = false;
            }
        }

        public void Dispose ()
        {
            if (disposeTasks)
            {
                tasks.Dispose ();
            }

            localCancelSource.Cancel ();
        }
    }
}
