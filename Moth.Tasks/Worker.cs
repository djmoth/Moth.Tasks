namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using Validation;

    /// <summary>
    /// Encapsulates a <see cref="Thread"/> running in the background, executing tasks from a <see cref="TaskQueue"/>.
    /// </summary>
    public class Worker : IDisposable
    {
        private readonly TaskQueue tasks;
        private readonly bool disposeTasks;
        private readonly Thread thread;
        private readonly CancellationTokenSource cancelSource;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;
        private int isRunningState = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="taskQueue">The <see cref="TaskQueue"/> that the <see cref="Worker"/> will execute tasks from.</param>
        /// <param name="disposeTaskQueue">Determines whether the <see cref="TaskQueue"/> supplied with <paramref name="taskQueue"/> is disposed when <see cref="Dispose ()"/> is called.</param>
        /// <param name="isBackground">Defines the <see cref="Thread.IsBackground"/> property of the internal thread.</param>
        /// <param name="profilerProvider">A <see cref="ProfilerProvider"/> which may provide an <see cref="IProfiler"/> for the <see cref="Worker"/>. May be <see langword="null"/>.</param>
        /// <param name="exceptionEventHandler">Method invoked if a task throws an exception. May be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="taskQueue"/> cannot be null.</exception>
        public Worker (
            TaskQueue taskQueue,
            bool disposeTaskQueue,
            bool isBackground,
            ProfilerProvider profilerProvider,
            EventHandler<TaskExceptionEventArgs> exceptionEventHandler = null)

            : this (taskQueue, disposeTaskQueue, isBackground, (IProfiler)null, exceptionEventHandler)
        {
            profiler = profilerProvider?.Invoke (this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="taskQueue">The <see cref="TaskQueue"/> that the <see cref="Worker"/> will execute tasks from.</param>
        /// <param name="disposeTaskQueue">Determines whether the <see cref="TaskQueue"/> supplied with <paramref name="taskQueue"/> is disposed when <see cref="Dispose ()"/> is called.</param>
        /// <param name="isBackground">Defines the <see cref="Thread.IsBackground"/> property of the internal thread.</param>
        /// <param name="profiler"><see cref="IProfiler"/> used to profile tasks. May be <see langword="null"/>.</param>
        /// <param name="exceptionEventHandler">Method invoked if a task throws an exception. May be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="taskQueue"/> cannot be null.</exception>
        public Worker (
            TaskQueue taskQueue,
            bool disposeTaskQueue,
            bool isBackground,
            IProfiler profiler = null,
            EventHandler<TaskExceptionEventArgs> exceptionEventHandler = null)
        {
            tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue), $"{nameof (taskQueue)} cannot be null.");
            disposeTasks = disposeTaskQueue;

            if (disposeTasks)
                GC.SuppressFinalize (tasks);

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
        /// Finalizes an instance of the <see cref="Worker"/> class.
        /// </summary>
        ~Worker () => Dispose (false);

        /// <summary>
        /// Is the thread still running? May be <see langword="true"/> for a short while even after <see cref="Dispose ()"/> is called.
        /// </summary>
        public bool IsRunning
        {
            get => Interlocked.CompareExchange (ref isRunningState, 1, 1) == 1;
            protected set => Interlocked.Exchange (ref isRunningState, value ? 1 : 0);
        }

        /// <summary>
        /// The <see cref="TaskQueue"/> of which the worker is executing tasks from.
        /// </summary>
        public TaskQueue Tasks => tasks;

        /// <summary>
        /// Sends a signal to shutdown the thread. Also disposes of <see cref="Tasks"/> if specified in <see cref="Worker"/> constructor.
        /// </summary>
        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        /// <summary>
        /// Sends a signal to shutdown the thread. Also disposes of <see cref="Tasks"/> if specified in <see cref="Worker"/> constructor.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if called from <see cref="Dispose ()"/>, <see langword="false"/> if called from finalizer.</param>
        protected virtual void Dispose (bool disposing)
        {
            cancelSource.Cancel ();

            if (disposeTasks)
                tasks.Dispose ();
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
