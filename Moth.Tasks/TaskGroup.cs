namespace Moth.Tasks
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    public unsafe class TaskGroup : IDisposable
    {
        private readonly object whenCompleteLock = new object ();
        private Counter* counter;
        private Action whenComplete;
        private bool isDisposed;
        private GCHandle thisHandle;

        public TaskGroup ()
        {
            counter = (Counter*)Marshal.AllocHGlobal (sizeof (Counter));

            thisHandle = GCHandle.Alloc (this, GCHandleType.Normal);
            *counter = new Counter (thisHandle);
        }

        ~TaskGroup ()
        {
            Dispose (false);
        }

        /// <summary>
        /// Gets the current progress of the task group, as a value between 0 and 1.
        /// </summary>
        public float Progress => !isDisposed ? (float)counter->CompletedCount / counter->TaskCount : throw new ObjectDisposedException (nameof (TaskGroup));

        /// <summary>
        /// Gets the number of tasks in the group.
        /// </summary>
        public int TaskCount => !isDisposed ? counter->TaskCount : throw new ObjectDisposedException (nameof (TaskGroup));

        /// <summary>
        /// Gets the number of tasks that have completed.
        /// </summary>
        public int CompletedCount => !isDisposed ? counter->CompletedCount : throw new ObjectDisposedException (nameof (TaskGroup));

        /// <summary>
        /// Gets a value indicating whether all tasks in the group have completed.
        /// </summary>
        public bool IsComplete => !isDisposed ? counter->IsComplete : throw new ObjectDisposedException (nameof (TaskGroup));

        /// <summary>
        /// Adds an action to be invoked when all tasks in the group have completed.
        /// If the tasks have already completed, the action is invoked immediately.
        /// </summary>
        /// <remarks>
        /// The action is invoked on the thread that completes the last task in the group.
        /// </remarks>
        /// <param name="action">The action to invoke.</param>
        public void WhenComplete (Action action)
        {
            if (isDisposed)
                throw new ObjectDisposedException (nameof (TaskGroup));

            lock (whenCompleteLock)
            {
                if (counter->IsComplete)
                {
                    action ();
                } else
                {
                    if (whenComplete == null)
                        whenComplete = action;
                    else
                        whenComplete += action;
                }
            }
        }

        /// <summary>
        /// Enqueues a task in the specified queue.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <param name="queue">The <see cref="TaskQueue"/> to enqueue the task in.</param>
        /// <param name="task">The task to enqueue.</param>
        public void Enqueue<T> (TaskQueue queue, T task)
            where T : struct, ITask
        {
            if (isDisposed)
                throw new ObjectDisposedException (nameof (TaskGroup));

            counter->AddTask ();
            queue.Enqueue (new TaskGroupItem<T> (counter, task));
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (!isDisposed)
            {
                lock (whenCompleteLock)
                {
                    if (counter->IsComplete)
                    {
                        Marshal.FreeHGlobal ((IntPtr)counter);
                    } else
                    {
                        throw new InvalidOperationException ("Cannot dispose TaskGroup while tasks are still running.");
                    }
                }

                thisHandle.Free ();
                counter = null;
                isDisposed = true;
            }
        }

        private struct Counter
        {
            private GCHandle groupHandle;
            private int completedCount;
            private int taskCount;

            public Counter (GCHandle groupHandle)
            {
                this.groupHandle = groupHandle;
                completedCount = 0;
                taskCount = 0;
            }

            public readonly GCHandle GroupHandle => groupHandle;

            public int TaskCount => Volatile.Read (ref taskCount);

            public int CompletedCount => Volatile.Read (ref completedCount);

            public bool IsComplete => CompletedCount == TaskCount;

            public void AddTask ()
            {
                Interlocked.Increment (ref taskCount);
            }

            public void MarkComplete ()
            {
                int newCompletedCount = Interlocked.Increment (ref completedCount);

                if (newCompletedCount == Volatile.Read (ref taskCount))
                {
                    TaskGroup group = (TaskGroup)groupHandle.Target;

                    lock (group.whenCompleteLock)
                    {
                        group.whenComplete?.Invoke ();
                    }
                }
            }
        }

        private unsafe readonly struct TaskGroupItem<T> : ITask, IDisposable
        where T : struct, ITask
        {
            private readonly Counter* groupCounter;
            private readonly T task;

            public TaskGroupItem (Counter* groupCounter, T task)
            {
                this.groupCounter = groupCounter;
                this.task = task;
            }

            public void Run () => task.Run ();

            public void Dispose ()
            {
                groupCounter->MarkComplete ();
            }
        }
    }
}
