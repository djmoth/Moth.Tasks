namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using Validation;

    /// <summary>
    /// Runs on an <see cref="IWorkerThread"/> continuously executing tasks from an <see cref="ITaskQueue{TArg, TResult}"/>.
    /// </summary>
    /// <typeparam name="TArg">Type of task argument.</typeparam>
    /// <typeparam name="TResult">Type of task result.</typeparam>
    public class Worker<TArg, TResult> : IWorker<TArg, TResult>, IDisposable
    {
        private readonly bool disposeTasks;
        private readonly IProfiler profiler;
        private readonly EventHandler<TaskExceptionEventArgs> exceptionEventHandler;
        private readonly IWorkerThread thread;
        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource ();
        private readonly TaskArgumentSource<TArg> argSource;
        private readonly TaskResultCallback<TResult> resultCallback;
        private bool disposed;
        private int isRunningState;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker{TArg, TResult}"/> class.
        /// </summary>
        /// <remarks>
        /// The <see cref="Worker{TArg, TResult}"/> will start executing tasks automatically.
        /// </remarks>
        /// <param name="taskQueue">The <see cref="ITaskQueue{TArg, TResult}"/> of which the worker will be executing tasks from.</param>
        /// <param name="shouldDisposeTaskQueue">If <see langword="true"/>, the <see cref="ITaskQueue{TArg, TResult}"/> supplied with <paramref name="taskQueue"/> will be disposed when <see cref="Dispose ()"/> is called.</param>
        /// <param name="options">Options for initializing the <see cref="Worker{TArg, TResult}"/>.</param>
        /// <param name="argSource">Source for arguments to supply to tasks. Will use <see langword="default"/> of <typeparamref name="TArg"/> if <see langword="null"/>.</param>
        /// <param name="resultCallback">Callback for handling results returned by tasks. Results will be discarded if <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="taskQueue"/> cannot be null.</exception>
        public Worker (ITaskQueue<TArg, TResult> taskQueue, bool shouldDisposeTaskQueue, WorkerOptions options, TaskArgumentSource<TArg> argSource = null, TaskResultCallback<TResult> resultCallback = null)
        {
            Tasks = taskQueue ?? throw new ArgumentNullException (nameof (taskQueue), $"{nameof (taskQueue)} cannot be null.");
            disposeTasks = shouldDisposeTaskQueue;

            if (shouldDisposeTaskQueue)
                GC.SuppressFinalize (Tasks);

            exceptionEventHandler = options.ExceptionEventHandler;
            profiler = options.Profiler;
            thread = options.WorkerThread ?? new WorkerThread (true);

            this.argSource = argSource;
            this.resultCallback = resultCallback;

            if (!options.RequiresManualStart)
                Start ();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Worker{TArg, TResult}"/> class.
        /// </summary>
        ~Worker () => Dispose (false);

        /// <summary>
        /// Gets a value indicating whether the thread is running.
        /// </summary>
        public bool IsRunning
        {
            get => Interlocked.CompareExchange (ref isRunningState, 1, 1) == 1;
            private set => Interlocked.Exchange (ref isRunningState, value ? 1 : 0);
        }

        /// <inheritdoc />
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the <see cref="ITaskQueue{TArg, TResult}"/> of which the worker is executing tasks from.
        /// </summary>
        public ITaskQueue<TArg, TResult> Tasks { get; }

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
        /// <exception cref="ObjectDisposedException">The <see cref="Worker{TArg, TResult}"/> has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The worker is already started.</exception>
        public void Start ()
        {
            if (disposed)
                throw new ObjectDisposedException (nameof (Worker<TArg, TResult>));

            if (IsStarted)
                throw new InvalidOperationException ("Worker is already started.");

            thread.Start (Work);

            IsStarted = true;
        }

        /// <summary>
        /// Calls <see cref="Dispose()"/> and blocks the calling thread until the <see cref="Worker{TArg, TResult}"/> terminates.
        /// </summary>
        public void DisposeAndJoin ()
        {
            Dispose ();
            thread.Join ();
        }

        /// <summary>
        /// Blocks the calling thread until the <see cref="Worker{TArg, TResult}"/> terminates.
        /// </summary>
        /// <remarks>
        /// <see cref="Dispose()"/> must be called beforehand. If <see cref="Start"/> was never called, this method will return immediately.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Worker{TArg, TResult}"/> is not disposed.</exception>
        public void Join ()
        {
            if (!IsStarted)
                return;

            if (!cancelSource.IsCancellationRequested)
                throw new InvalidOperationException ("Join may only be called after the Worker is disposed.");

            thread.Join ();
        }

        /// <summary>
        /// Sends a signal to shutdown the thread. Also disposes of <see cref="Tasks"/> if specified in <see cref="Worker{TArg, TResult}"/> constructor.
        /// </summary>
        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        /// <summary>
        /// Sends a signal to shutdown the thread. Also disposes of <see cref="Tasks"/> if specified in <see cref="Worker{TArg, TResult}"/> constructor.
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
                    TArg arg = argSource != null ? argSource () : default;
                    TResult result = Tasks.RunNextTask (arg, out Exception exception, profiler, cancel);

                    if (exception != null)
                    {
                        exceptionEventHandler?.Invoke (this, new TaskExceptionEventArgs (exception));
                    } else
                    {
                        resultCallback?.Invoke (result);
                    }
                }
            } catch (Exception ex)
            {
                Trace.TraceError ("Internal exception in Worker: " + ex.ToString ());
                Dispose ();

                throw;
            } finally
            {
                if (disposeTasks && Tasks is IDisposable disposableTasks)
                    disposableTasks.Dispose ();

                IsRunning = false;
            }
        }
    }
}
