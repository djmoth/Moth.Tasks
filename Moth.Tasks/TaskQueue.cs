namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// A queue of tasks, which can be run in FIFO order.
    /// </summary>
    public sealed unsafe class TaskQueue : IDisposable
    {
        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private readonly Dictionary<int, ManualResetEventSlim> taskHandles = new Dictionary<int, ManualResetEventSlim> ();
        private readonly Queue<int> tasks;
        private readonly ManualResetEventSlim tasksEnqueuedEvent = new ManualResetEventSlim (); // Must be explicitly set by callers of EnqueueImpl
        private object[] taskData;
        private int firstTask;
        private int lastTaskEnd;
        private bool disposed;
        private int nextTaskHandle = 1;
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueue"/> class.
        /// </summary>
        public TaskQueue ()
            : this (16, 256) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueue"/> class.
        /// </summary>
        /// <param name="taskCapacity">Starting capacity for the internal task queue.</param>
        /// <param name="dataCapacity">Starting capacity for the internal task data array.</param>
        internal TaskQueue (int taskCapacity, int dataCapacity)
        {
            tasks = new Queue<int> (taskCapacity);
            taskData = new object[dataCapacity];
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TaskQueue"/> class. Also disposes of tasks implementing <see cref="IDisposable.Dispose"/>.
        /// </summary>
        ~TaskQueue () => Clear ();

        /// <summary>
        /// The number of tasks currently enqueued.
        /// </summary>
        public int Count
        {
            get
            {
                lock (taskLock)
                {
                    return tasks.Count;
                }
            }
        }

        /// <summary>
        /// Enqueue an action to be run later.
        /// </summary>
        /// <param name="action">Action to enqueue.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue (Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException (nameof (action));
            }

            Enqueue (new DelegateTask (action));
        }

        /// <summary>
        /// Enqueue an action to be run later with an argument.
        /// </summary>
        /// <typeparam name="T">The type of the parameter that <paramref name="action"/> encapsulates.</typeparam>
        /// <param name="action">Action to enqueue.</param>
        /// <param name="arg">Argument to run <paramref name="action"/> with.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<T> (Action<T> action, T arg)
        {
            if (action == null)
            {
                throw new ArgumentNullException (nameof (action));
            }

            Enqueue (new DelegateTask<T> (action, arg));
        }

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<T> (in T task) where T : struct, ITask
        {
            lock (taskLock)
            {
                EnqueueImpl (task);
            }

            tasksEnqueuedEvent.Set (); // Signal potentially waiting threads that tasks are ready to be executed
        }

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later, giving out a <see cref="TaskHandle"/> for checking task status.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Handle for checking task status.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="TaskQueue"/> has been disposed.</exception>
        public void Enqueue<T> (in T task, out TaskHandle handle) where T : struct, ITask
        {
            lock (taskLock)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException (nameof (TaskQueue), "New tasks may not be enqueued after TaskQueue has been disposed.");
                }

                int handleID = nextTaskHandle++;

                taskHandles.Add (handleID, null);

                handle = new TaskHandle (this, handleID);

                if (typeof (IDisposable).IsAssignableFrom (typeof (T))) // If T implements IDisposable
                {
                    EnqueueImpl (new DisposableTaskWithHandle<T> (this, task, handleID));
                } else
                {
                    EnqueueImpl (new TaskWithHandle<T> (this, task, handleID));
                }
            }

            tasksEnqueuedEvent.Set (); // Signal potentially waiting threads that tasks are ready to be executed
        }

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (IProfiler profiler = null, CancellationToken token = default) => RunNextTask (out _, profiler, token);

        /// <summary>
        /// Blocks until a task is ready in the queue, then runs it.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe when waiting for a task. Does not cancel actual task execution.</param>
        public void RunNextTask (out Exception exception, IProfiler profiler = null, CancellationToken token = default)
        {
            tasksEnqueuedEvent.Wait (token);

            if (token.IsCancellationRequested)
            {
                exception = null;
                return;
            }

            TryRunNextTask (out exception, profiler);
        }

        /// <summary>
        /// Tries to run the next task in the queue, if present. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (IProfiler profiler = null) => TryRunNextTask (out _, profiler);

        /// <summary>
        /// Tries to run the next task in the queue, if present. Provides an <see cref="Exception"/> thrown by the task, in case it fails. May also perform profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless of whether an exception occured.
        /// </remarks>
        public bool TryRunNextTask (out Exception exception, IProfiler profiler = null)
        {
            exception = null;

            TaskDataAccess access = new TaskDataAccess (this);

            if (tasks.Count == 0)
            {
                access.Dispose ();
                return false;
            }

            TaskInfo task;

            try
            {
                int id = tasks.Dequeue ();

                if (tasks.Count == 0)
                {
                    tasksEnqueuedEvent.Reset (); // All tasks have fetched, and as so the event can be reset.
                }

                task = taskCache.GetTask (id);
            } catch // Internal error
            {
                access.Dispose ();

                throw;
            }

            bool isProfiling = false;

            try
            {
                if (profiler != null)
                {
                    profiler.BeginTask (task.Type.FullName);
                    isProfiling = true; // If profiler was started without throwing an exception
                }

                task.RunAndDispose (ref access); // Run the task

                if (isProfiling)
                {
                    isProfiling = false;
                    profiler.EndTask ();
                }
            } catch (Exception ex)
            {
                exception = ex;

                if (!access.Disposed) // Internal error, TaskInfo should always call TaskDataAccess.Dispose after getting task data in TaskInfo.RunAndDispose
                {
                    access.Dispose ();
                }

                if (isProfiling)
                {
                    profiler.EndTask ();
                }
            }

            return true;
        }

        /// <summary>
        /// Removes all pending tasks from the queue. Also calls <see cref="IDisposable.Dispose"/> on tasks which implement the method.
        /// </summary>
        /// <param name="exceptionHandler">Method for handling an exception thrown by a task's <see cref="IDisposable.Dispose"/>.</param>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call, the method continues on with disposing the remaining tasks.
        /// </remarks>
        public void Clear (Action<Exception> exceptionHandler = null)
        {
            using TaskDataAccess access = new TaskDataAccess (this);

            foreach (int id in tasks)
            {
                TaskInfo task = taskCache.GetTask (id);

                if (task.Disposable)
                {
                    try
                    {
                        task.Dispose (access);
                    } catch (Exception ex)
                    {
                        exceptionHandler?.Invoke (ex);
                    }
                } else
                {
                    firstTask += task.DataIndices; // Skip task data
                }
            }

            tasks.Clear ();
            taskHandles.Clear ();

            firstTask = 0;
            lastTaskEnd = 0;
        }

        /// <summary>
        /// Disposes all tasks which implements <see cref="IDisposable"/>.
        /// </summary>
        /// <remarks>
        /// As the method iterates through all tasks in the queue and calls <see cref="IDisposable.Dispose"/> on tasks, it can hang for an unknown amount of time. If an exception is thrown in an <see cref="IDisposable.Dispose"/> call, the method continues on with disposing the remaining tasks.
        /// </remarks>
        public void Dispose ()
        {
            lock (taskLock)
            {
                Clear ();
                GC.SuppressFinalize (this);

                disposed = true;
            }
        }

        /// <summary>
        /// Used by <see cref="TaskHandle.WaitForCompletion ()"/> to wait until task is complete.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns><see langword="true"/> if task was completed, <see langword="false"/> if timeout was reached.</returns>
        internal bool WaitForCompletion (int handleID, int millisecondsTimeout)
        {
            ManualResetEventSlim waitEvent;
            bool complete;

            lock (taskLock)
            {
                complete = !taskHandles.TryGetValue (handleID, out waitEvent);

                if (!complete && waitEvent == null)
                {
                    waitEvent = new ManualResetEventSlim ();
                    taskHandles[handleID] = waitEvent;
                }
            }

            if (!complete)
            {
                complete = waitEvent.Wait (millisecondsTimeout);
            }

            return complete;
        }

        /// <summary>
        /// Used by <see cref="TaskWithHandle{T}"/> to notify callers of <see cref="WaitForCompletion(int, int)"/> that the task is done.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        internal void NotifyTaskComplete (int handleID)
        {
            lock (taskLock)
            {
                ManualResetEventSlim waitEvent = taskHandles[handleID];

                if (waitEvent != null)
                {
                    waitEvent.Set ();
                    waitEvent.Dispose ();
                }

                taskHandles.Remove (handleID);
            }
        }

        /// <summary>
        /// Check if a task has completed.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        /// <returns><see langword="true"/> if task has completed, otherwise <see langword="false"/>.</returns>
        internal bool IsTaskComplete (int handleID)
        {
            lock (taskLock)
            {
                return !taskHandles.ContainsKey (handleID);
            }
        }

        private void EnqueueImpl<T> (in T task) where T : struct, ITask
        {
            if (disposed)
            {
                throw new ObjectDisposedException (nameof (TaskQueue), "New tasks may not be enqueued after TaskQueue has been disposed.");
            }

            TaskInfo taskInfo = taskCache.GetTask<T> ();

            tasks.Enqueue (taskInfo.ID); // Add task ID to the queue

            // Only write task data if present
            if (taskInfo.DataIndices > 0)
            {
                // If new task data will overflow the taskData array
                if (lastTaskEnd + taskInfo.DataIndices > taskData.Length)
                {
                    int totalTaskDataLength = lastTaskEnd - firstTask;

                    // If there is not enough total space in taskData array to hold new task, then resize taskData
                    if (totalTaskDataLength + taskInfo.DataIndices > taskData.Length)
                    {
                        // If taskInfo.DataIndices is abnormally large, doubling the size might not always be enough
                        int newSize = Math.Max (taskData.Length * 2, totalTaskDataLength + taskInfo.DataIndices);
                        Array.Resize (ref taskData, newSize);
                    }

                    if (firstTask != 0)
                    {
                        Array.Copy (taskData, firstTask, taskData, 0, totalTaskDataLength); // Move tasks to the beginning of taskData, to eliminate wasted space

                        lastTaskEnd = totalTaskDataLength;
                        firstTask = 0;
                    }
                }

                ref T newTask = ref Unsafe.As<object, T> (ref taskData[lastTaskEnd]);
                newTask = task; // Write task data

                lastTaskEnd += taskInfo.DataIndices;
            }

            /* tasksEnqueuedEvent.Set () could be called here as this is the shared Enqueue implementation, but taskLock is still locked at this point:
             * As so, it is better to wait and let it be called explicitly by callers of EnqueueImpl
             */
        }

        private T GetNextTask<T> (TaskInfo task) where T : struct, ITask
        {
            ref T dataRef = ref Unsafe.As<object, T> (ref taskData[firstTask]);
            T data = dataRef;

            dataRef = default; // Clear stored data, as to not leave references hanging

            firstTask += task.DataIndices;

            if (firstTask == lastTaskEnd) // If firstTask is now equal to lastTaskEnd, then this was the last task in the queue
            {
                firstTask = 0;
                lastTaskEnd = 0;
            }

            return data;
        }

        /// <summary>
        /// Provides a way for a task to access its data while locking the <see cref="TaskQueue"/>.
        /// </summary>
        internal ref struct TaskDataAccess
        {
            private TaskQueue queue;

            /// <summary>
            /// Initializes a new instance of the <see cref="TaskDataAccess"/> struct. Locks the <see cref="TaskQueue"/>.
            /// </summary>
            /// <param name="queue">Reference to the queue.</param>
            public TaskDataAccess (TaskQueue queue)
            {
                this.queue = queue;
                Monitor.Enter (queue.taskLock);
            }

            /// <summary>
            /// Gets a value indicating whether the lock is still held.
            /// </summary>
            public bool Disposed => queue == null;

            /// <summary>
            /// Fetches next data of a task.
            /// </summary>
            /// <typeparam name="T">Type of task.</typeparam>
            /// <param name="task">TaskInfo of task.</param>
            /// <returns>Task data.</returns>
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public readonly T GetTaskData<T> (TaskInfo task) where T : struct, ITask => queue.GetNextTask<T> (task);

            /// <summary>
            /// Exits the lock.
            /// </summary>
            public void Dispose ()
            {
                Monitor.Exit (queue.taskLock);
                queue = null;
            }
        }
    }
}
