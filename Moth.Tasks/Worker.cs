namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using Validation;

    /// <summary>
    /// Encapsulates a thread running in the background, executing tasks from a <see cref="TaskQueue"/>.
    /// </summary>
    public class Worker : IDisposable
    {
        private readonly TaskQueue tasks;
        private readonly bool disposeTasks;
        private readonly Thread thread;
        private readonly CancellationToken sharedCancel;
        private readonly CancellationTokenSource localCancelSource;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;
        private readonly AutoResetEvent runEvent = new AutoResetEvent (true);

        public Worker () : this (new TaskQueue (), CancellationToken.None) { }

        public Worker (TaskQueue taskQueue, CancellationToken sharedCancelToken, bool isBackground = true, EventHandler<TaskExceptionEventArgs> exceptionEventHandler = null, IProfiler profiler = null, bool disposeTaskQueue = false)
        {
            tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue));
            this.disposeTasks = disposeTaskQueue;

            localCancelSource = new CancellationTokenSource ();
            sharedCancel = sharedCancelToken != CancellationToken.None ? sharedCancel : localCancelSource.Token;

            this.exceptionEventHandler = exceptionEventHandler;
            this.profiler = profiler;

            taskQueue.TaskEnqueued += RunEvent;

            thread = new Thread (Work)
            {
                IsBackground = isBackground,
            };
            thread.Start ();
        }

        /// <summary>
        /// Is the thread still running? May be <see langword="true"/> for a short while even after <see cref="Dispose"/> is called.
        /// </summary>
        public bool IsRunning { get; private set; } = true;

        public bool IsCancelledRequested => sharedCancel.IsCancellationRequested || localCancelSource.IsCancellationRequested;

        public void Dispose ()
        {
            tasks.TaskEnqueued -= RunEvent;

            if (disposeTasks)
            {
                tasks.Dispose ();
            }

            localCancelSource.Cancel ();

            runEvent.Set (); // Signal event incase thread is waiting, to alert thread of cancellation
        }

        private void RunEvent (object sender, EventArgs e) => runEvent.Set ();

        private void Work ()
        {
            try
            {
                while (!IsCancelledRequested)
                {
                    runEvent.WaitOne ();

                    if (IsCancelledRequested)
                    {
                        break;
                    }

                    bool ranTask = tasks.TryRunNextTask (profiler, out Exception exception);

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
    }
}
