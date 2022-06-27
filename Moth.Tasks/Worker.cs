﻿namespace Moth.Tasks
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
    /// <remarks>
    /// This class is thread-safe.
    /// </remarks>
    public class Worker : IDisposable
    {
        private readonly bool disposeTasks;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;
        private readonly Thread thread;
        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource ();
        private int isRunningState;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <remarks>
        /// The <see cref="Worker"/> will start executing tasks automatically.
        /// </remarks>
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
        /// <remarks>
        /// The <see cref="Worker"/> will start executing tasks automatically.
        /// </remarks>
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
            Tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue), $"{nameof (taskQueue)} cannot be null.");
            disposeTasks = disposeTaskQueue;

            if (disposeTasks)
                GC.SuppressFinalize (Tasks);

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
        /// Gets a value indicating whether the thread is running.
        /// </summary>
        /// <remarks>
        /// May be <see langword="true"/> for a short while even after <see cref="Dispose ()"/> is called.
        /// </remarks>
        public bool IsRunning
        {
            get => Interlocked.CompareExchange (ref isRunningState, 1, 1) == 1;
            protected set => Interlocked.Exchange (ref isRunningState, value ? 1 : 0);
        }

        /// <summary>
        /// Gets the <see cref="TaskQueue"/> of which the worker is executing tasks from.
        /// </summary>
        public TaskQueue Tasks { get; }

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationToken"/> associated with this worker.
        /// </summary>
        /// <remarks>
        /// Can be passed to a task enqueued on <see cref="Tasks"/>, allowing it to exit early if <see cref="Dispose"/> is called.
        /// </remarks>
        public CancellationToken CancellationToken => cancelSource.Token;

        /// <summary>
        /// Calls <see cref="Dispose"/> and blocks the calling thread until the <see cref="Worker"/> terminates.
        /// </summary>
        public void DisposeAndJoin ()
        {
            Dispose ();
            thread.Join ();
        }

        /// <summary>
        /// Blocks the calling thread until the <see cref="Worker"/> terminates.
        /// </summary>
        /// <remarks>
        /// <see cref="Dispose"/> must be called beforehand.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Worker>"/> is not disposed.</exception>
        public void Join ()
        {
            if (!cancelSource.IsCancellationRequested)
                throw new InvalidOperationException ("Join may only be called after the Worker is disposed.");

            thread.Join ();
        }

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
                Tasks.Dispose ();
        }

        private void Work ()
        {
            IsRunning = true;

            try
            {
                CancellationToken cancel = cancelSource.Token;

                while (!cancel.IsCancellationRequested)
                {
                    Tasks.RunNextTask (out Exception exception, profiler, cancel);

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
