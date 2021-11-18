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
        private const int StartCapacity = 256;

        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private readonly Queue<int> tasks = new Queue<int> (16);
        private readonly Dictionary<int, ManualResetEventSlim> taskHandles = new Dictionary<int, ManualResetEventSlim> ();
        private object[] taskData = new object[StartCapacity];
        private int firstTask;
        private int lastTaskEnd;
        private bool disposed;
        private int nextTaskHandle = 1;

        /// <summary>
        /// Finalizes an instance of the <see cref="TaskQueue"/> class. Also disposes of tasks implementing <see cref="IDisposable.Dispose"/>.
        /// </summary>
        ~TaskQueue () => Clear ();

        /// <summary>
        /// Enqeue an action to be run later.
        /// </summary>
        /// <param name="action">Action to enqueue.</param>
        public void Enqueue (Action action) => Enqueue (new DelegateTask (action));

        /// <summary>
        /// Enqueue an action to be run later with an argument.
        /// </summary>
        /// <typeparam name="T">The type of the parameter that <paramref name="action"/> encapsulates.</typeparam>
        /// <param name="action">Action to enqueue.</param>
        /// <param name="arg">Argument to run <paramref name="action"/> with.</param>
        public void Enqueue<T> (Action<T> action, T arg) => Enqueue (new DelegateTask<T> (action, arg));

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        public void Enqueue<T> (in T task) where T : struct, ITask
        {
            lock (taskLock)
            {
                EnqueueImpl (task);
            }
        }

        /// <summary>
        /// Enqueue an <see cref="ITask"/> to be run later, giving out a <see cref="TaskHandle"/> for checking task status.
        /// </summary>
        /// <typeparam name="T">Type of task to run.</typeparam>
        /// <param name="task">Task data.</param>
        /// <param name="handle">Handle for checking task status.</param>
        public void Enqueue<T> (in T task, out TaskHandle handle) where T : struct, ITask
        {
            lock (taskLock)
            {
                int handleID = nextTaskHandle++;

                taskHandles.Add (handleID, default);

                handle = new TaskHandle (this, handleID);

                if (task is IDisposable)
                {
                    EnqueueImpl (new TaskWithHandle<T> (this, task, handleID));
                } else
                {
                    EnqueueImpl (new DisposableTaskWithHandle<T> (this, task, handleID));
                }
            }
        }

        /// <summary>
        /// Try to run the next task in the queue, if present.
        /// </summary>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless if an exception occured.
        /// </remarks>
        public bool TryRunNextTask () => TryRunNextTask (null, out _);

        /// <summary>
        /// Try to run the next task in the queue, if present. Also performs profiling on the task through an <see cref="IProfiler"/>.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless if an exception occured.
        /// </remarks>
        public bool TryRunNextTask (IProfiler profiler) => TryRunNextTask (profiler, out _);

        /// <summary>
        /// Try to run the next task in the queue, if present. Also provides an <see cref="Exception"/> thrown by the task in case it fails.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless if an exception occured.
        /// </remarks>
        public bool TryRunNextTask (out Exception exception) => TryRunNextTask (null, out exception);

        /// <summary>
        /// Try to run the next task in the queue, if present. Also performs profiling on the task through an <see cref="IProfiler"/>, and provides an <see cref="Exception"/> thrown by the task in case it fails.
        /// </summary>
        /// <param name="profiler"><see cref="IProfiler"/> to profile the run-time of the task.</param>
        /// <param name="exception"><see cref="Exception"/> thrown if task failed. Is <see langword="null"/> if task was run successfully.</param>
        /// <returns><see langword="true"/> if a task was run, <see langword="false"/> if the <see cref="TaskQueue"/> is empty.</returns>
        /// <remarks>
        /// Please note that the return value does not indicate if a task was successful. The method will return <see langword="true"/> if a task was ready in the queue, regardless if an exception occured.
        /// </remarks>
        public bool TryRunNextTask (IProfiler profiler, out Exception exception)
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

                task = taskCache.GetTask (id);
            } catch
            {
                access.Dispose ();

                throw; // Rethrow without setting exception, this is an internal error
            }

            bool isProfiling = false;

            try
            {
                if (profiler != null)
                {
                    profiler.BeginTask (task.Type.FullName);
                    isProfiling = true; // If profiler was started without throwing an exception
                }

                task.Run (ref access); // Run the task

                if (isProfiling)
                {
                    isProfiling = false;
                    profiler.EndTask ();
                }
            } catch (Exception ex)
            {
                exception = ex;

                if (!access.Disposed)
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
                waitEvent?.Set ();
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
                return taskHandles.ContainsKey (handleID);
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

            // If new task data will overflow the taskData array
            if (lastTaskEnd + taskInfo.DataIndices > taskData.Length)
            {
                int totalTaskDataLength = lastTaskEnd - firstTask;

                // If there is not enough total space in taskData array to hold new task, then resize taskData
                if (totalTaskDataLength + taskInfo.DataIndices > taskData.Length)
                {
                    // If taskInfo.DataIndices is abnormally large, doubling the size might not always be enough
                    int newSize = Math.Max (taskData.Length * 2, totalTaskDataLength + taskInfo.DataIndices);
                    Array.Resize (ref taskData, taskData.Length * 2);
                }

                if (firstTask != 0)
                {
                    Buffer.BlockCopy (taskData, firstTask, taskData, 0, totalTaskDataLength); // Move tasks to the beginning of taskData, to eliminate wasted space

                    lastTaskEnd = totalTaskDataLength;
                    firstTask = 0;
                }
            }

            ref T newTask = ref Unsafe.As<object, T> (ref taskData[lastTaskEnd]);
            newTask = task; // Write task data

            lastTaskEnd += taskInfo.DataIndices;
        }

        private ref T GetNextTask<T> (TaskInfo task) where T : struct, ITask
        {
            ref T data = ref Unsafe.As<object, T> (ref taskData[firstTask]);

            firstTask += task.DataIndices;

            if (firstTask == lastTaskEnd) // If firstTask is equal to lastTaskEnd, it means that this was the last task in the queue
            {
                firstTask = 0;
                lastTaskEnd = 0;
            }

            return ref data;
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
