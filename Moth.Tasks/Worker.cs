namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using Validation;

    /// <summary>
    /// Runs on an <see cref="IWorkerThread"/> continuously executing tasks from a <see cref="TaskQueue"/>.
    /// </summary>
    public class Worker : IWorker
    {
        private readonly bool disposeTasks;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;
        private readonly IWorkerThread thread;
        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource ();
        private bool disposed;
        private int isRunningState;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <remarks>
        /// The <see cref="Worker"/> will start executing tasks automatically.
        /// </remarks>
        /// <param name="taskQueue">The <see cref="TaskQueue"/> of which the worker will be executing tasks from.</param>
        /// <param name="shouldDisposeTaskQueue">Determines whether the <see cref="TaskQueue"/> supplied with <paramref name="taskQueue"/> is disposed when <see cref="Dispose ()"/> is called.</param>
        /// <param name="options">Options for initializing the <see cref="Worker"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="taskQueue"/> cannot be null.</exception>
        public Worker (ITaskQueue taskQueue, bool shouldDisposeTaskQueue, WorkerOptions options)
        {
            Tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue), $"{nameof (taskQueue)} cannot be null.");
            disposeTasks = shouldDisposeTaskQueue;

            if (disposeTasks)
                GC.SuppressFinalize (Tasks);

            exceptionEventHandler = options.ExceptionEventHandler;

            if (options.ProfilerProvider != null && options.Profiler != null)
                throw new ArgumentException ("Cannot provide both a ProfilerProvider and a Profiler.");

            if (options.ProfilerProvider != null)
                profiler = options.ProfilerProvider (this);
            else
                profiler = options.Profiler;

            if (options.WorkerThreadProvider != null && options.WorkerThread != null)
                throw new ArgumentException ("Cannot provide both a WorkerThreadProvider and a WorkerThread.");

            if (options.WorkerThreadProvider != null)
                thread = options.WorkerThreadProvider (this);
            else
                thread = options.WorkerThread ?? new WorkerThread (true);

            if (!options.RequiresManualStart)
                Start ();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Worker"/> class.
        /// </summary>
        ~Worker () => Dispose (false);

        /// <summary>
        /// Gets a value indicating whether the thread is running.
        /// </summary>
        public bool IsRunning
        {
            get => Interlocked.CompareExchange (ref isRunningState, 1, 1) == 1;
            protected set => Interlocked.Exchange (ref isRunningState, value ? 1 : 0);
        }

        /// <inheritdoc />
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the <see cref="TaskQueue"/> of which the worker is executing tasks from.
        /// </summary>
        public ITaskQueue Tasks { get; }

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationToken"/> associated with this worker.
        /// </summary>
        /// <remarks>
        /// Can be passed to a task enqueued on <see cref="Tasks"/>, allowing it to exit early if <see cref="Dispose()"/> is called.
        /// </remarks>
        public CancellationToken CancellationToken => cancelSource.Token;

        /// <summary>
        /// Manually starts the worker if <see cref="WorkerOptions.RequiresManualStart"/> was set to <see langword="true"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="Worker"/> has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The worker is already started.</exception>
        public void Start ()
        {
            if (disposed)
                throw new ObjectDisposedException (nameof (Worker));

            if (IsStarted)
                throw new InvalidOperationException ("Worker is already started.");

            thread.Start (Work);

            IsStarted = true;
        }

        /// <summary>
        /// Calls <see cref="Dispose()"/> and blocks the calling thread until the <see cref="Worker"/> terminates.
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
        /// <see cref="Dispose()"/> must be called beforehand. If <see cref="Start"/> was never called, this method will return immediately.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Worker"/> is not disposed.</exception>
        public void Join ()
        {
            if (!IsStarted)
                return;

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
            if (disposed)
                return;

            disposed = true;

            if (IsStarted)
            {
                cancelSource.Cancel ();
            } else
            {
                if (disposeTasks && Tasks is IDisposable disposableTasks)
                    disposableTasks.Dispose ();
            }
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
                if (disposeTasks && Tasks is IDisposable disposableTasks)
                    disposableTasks.Dispose ();

                IsRunning = false;
            }
        }
    }
}
