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
    /// <remarks>
    /// This class is thread-safe.
    /// </remarks>
    public class Worker : IDisposable
    {
        private readonly bool disposeTasks;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;
        private readonly IWorkerThread thread;
        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource ();
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

            thread = options.WorkerThread ?? new WorkerThread (true);
            thread.Start (Work);
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
        public ITaskQueue Tasks { get; }

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
        /// <exception cref="InvalidOperationException">The <see cref="Worker"/> is not disposed.</exception>
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

            if (disposeTasks && Tasks is IDisposable disposableTasks)
                disposableTasks.Dispose ();
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
