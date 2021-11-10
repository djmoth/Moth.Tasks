﻿namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// 
    /// </summary>
    public sealed unsafe class TaskQueue : IDisposable
    {
        private const int StartCapacity = 256;

        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private readonly Queue<int> tasks = new Queue<int> (16);
        private readonly Dictionary<int, TaskHandleEvent> taskHandles = new Dictionary<int, TaskHandleEvent> ();
        private readonly ExceptionHandler exceptionHandler;
        private object[] taskData = new object[StartCapacity];
        private int firstTask;
        private int lastTaskEnd;
        private bool disposed;
        private int nextTaskHandle = 1;

        public TaskQueue (ExceptionHandler exceptionHandler)
        {
            this.exceptionHandler = exceptionHandler;
        }

        ~TaskQueue () => Clear ();

        public delegate void ExceptionHandler (Exception exception);

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
        /// Enqueue an <see cref="ITask"/> to be run later, giving back a <see cref="TaskHandle"/> for checking task status.
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

                EnqueueImpl (task);
            }
        }

        /// <summary>
        /// Used by <see cref="TaskHandle.WaitForCompletion"/> to wait until task is complete.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        /// <param name="timeout">Maximum time in milliseconds to wait. A value of -1 waits infinitely.</param>
        /// <returns><see langword="true"/> if task was completed, <see langword="false"/> if timeout was reached.</returns>
        internal bool WaitForCompletion (int handleID, int timeout)
        {
            TaskHandleEvent waitEvent;
            bool complete;

            lock (taskLock)
            {
                complete = !taskHandles.TryGetValue (handleID, out waitEvent);

                if (!complete)
                {
                    waitEvent.AddWaiter ();
                }
            }

            if (!complete)
            {
                complete = waitEvent.Wait (timeout);

                lock (taskLock)
                {
                    if (waitEvent.RemoveWaiter (complete))
                    {
                        taskHandles.Remove (handleID);
                    }
                }
            }

            return complete;
        }

        /// <summary>
        /// Used by <see cref="TaskWithHandle{T}"/>
        /// </summary>
        /// <param name="handleID"></param>
        internal void NotifyTaskComplete (int handleID)
        {
            lock (taskLock)
            {
                TaskHandleEvent waitEvent = taskHandles[handleID];

                if (waitEvent.SignalComplete ())
                {
                    taskHandles.Remove (handleID);
                }
            }
        }

        internal bool IsTaskComplete (int handleID)
        {
            lock (taskLock)
            {
                return taskHandles.ContainsKey (handleID);
            }
        }

        public bool TryRunNextTask () => TryRunNextTask (null);

        public bool TryRunNextTask (IProfiler profiler)
        {
            TaskInfo task;

            TaskDataAccess access = new TaskDataAccess (this);

            if (tasks.Count == 0)
            {
                access.Dispose ();
                return false;
            }

            try
            {
                int id = tasks.Dequeue ();

                task = taskCache.GetTask (id);
            } catch
            {
                access.Dispose ();

                throw; // Rethrow without notifying exceptionHandler.
            }

            bool isProfiling = false;

            try
            {
                if (profiler != null)
                {
                    profiler.BeginTask (task.Name);
                    isProfiling = true; // If profiler was started without throwing and exception
                }

                task.Run (ref access); // Run the task with it's data.

                if (isProfiling)
                {
                    isProfiling = false;
                    profiler.EndTask ();
                }
            } catch (Exception ex)
            {
                if (!access.Disposed)
                {
                    access.Dispose ();
                }

                if (isProfiling)
                {
                    try
                    {
                        profiler.EndTask ();
                    } catch (Exception profilerEx)
                    {
                        exceptionHandler (profilerEx);
                    }
                }

                exceptionHandler (ex); // Notify handler in case of an exception.
            }

            return true;
        }

        public void Clear ()
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
                        exceptionHandler (ex);
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

        public void Dispose ()
        {
            lock (taskLock)
            {
                Clear ();
                GC.SuppressFinalize (this);

                disposed = true;
            }
        }

        private void EnqueueImpl<T> (in T task) where T : struct, ITask
        {
            if (disposed)
            {
                throw new ObjectDisposedException (nameof (TaskQueue), "New tasks may not be enqueued after TaskQueue has been disposed.");
            }

            TaskInfo taskInfo = taskCache.GetTask<T> ();

            tasks.Enqueue (taskInfo.ID); // Add task ID to the queue.

            // If new task data will overflow the taskData array.
            if (lastTaskEnd + taskInfo.DataIndices > taskData.Length)
            {
                int totalTaskDataLength = lastTaskEnd - firstTask;

                // If there is not enough total space in taskData array to hold new task, then resize taskData
                if (totalTaskDataLength + taskInfo.DataIndices > taskData.Length)
                {
                    // If taskInfo.DataIndices is abnormally large, doubling the size might not always be enough.
                    int newSize = Math.Max (taskData.Length * 2, totalTaskDataLength + taskInfo.DataIndices);
                    Array.Resize (ref taskData, taskData.Length * 2);
                }

                if (firstTask != 0)
                {
                    Buffer.BlockCopy (taskData, firstTask, taskData, 0, totalTaskDataLength); // Move tasks to the beginning of taskData, to eliminate wasted space.

                    lastTaskEnd = totalTaskDataLength;
                    firstTask = 0;
                }
            }

            ref T newTask = ref Unsafe.As<object, T> (ref taskData[lastTaskEnd]);
            newTask = task; // Write task data.

            lastTaskEnd += taskInfo.DataIndices;
        }

        private ref T GetNextTask<T> (TaskInfo task) where T : struct, ITask
        {
            ref T data = ref Unsafe.As<object, T> (ref taskData[firstTask]);

            firstTask += task.DataIndices;

            if (firstTask == lastTaskEnd) // If firstTask is equal to lastTaskEnd, it means that this was the last task in the queue.
            {
                firstTask = 0;
                lastTaskEnd = 0;
            }

            return ref data;
        }

        internal ref struct TaskDataAccess
        {
            private TaskQueue queue;

            public bool Disposed => queue == null;

            public TaskDataAccess (TaskQueue queue)
            {
                this.queue = queue;
                Monitor.Enter (queue.taskLock);
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public readonly T GetTaskData<T> (TaskInfo task) where T : struct, ITask => queue.GetNextTask<T> (task);

            public void Dispose ()
            {
                Monitor.Exit (queue.taskLock);
                queue = null;
            }
        }
    }
}
