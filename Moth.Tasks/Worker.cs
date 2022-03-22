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
        private readonly CancellationTokenSource cancelSource;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;

        public Worker () : this (new TaskQueue ()) { }

        public Worker (TaskQueue taskQueue, bool isBackground = true, EventHandler<TaskExceptionEventArgs> exceptionEventHandler = null, IProfiler profiler = null, bool disposeTaskQueue = false)
        {
            tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue));
            this.disposeTasks = disposeTaskQueue;

            cancelSource = new CancellationTokenSource ();

            this.exceptionEventHandler = exceptionEventHandler;
            this.profiler = profiler;

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

        /// <summary>
        /// Sends a signal to shutdown the thread. Also disposes of <see cref="TaskQueue"/> if specified in <see cref="Worker"/> constructor.
        /// </summary>
        public void Dispose ()
        {
            cancelSource.Cancel ();

            if (disposeTasks)
            {
                tasks.Dispose ();
            }
        }

        private void Work ()
        {
            try
            {
                CancellationToken cancel = cancelSource.Token;

                while (!cancel.IsCancellationRequested)
                {
                    tasks.RunNextTask (out Exception exception, profiler, cancel);

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
