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

        public TaskGroup ()
        {
            counter = (Counter*)Marshal.AllocHGlobal (sizeof (Counter));
            *counter = new Counter (this);
        }

        ~TaskGroup ()
        {
            Dispose (false);
        }

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

                counter = null;
                isDisposed = true;
            }
        }

        private struct Counter
        {
            private GCHandle groupHandle;
            private int completedCount;
            private int taskCount;

            public Counter (TaskGroup group)
            {
                groupHandle = GCHandle.Alloc (group, GCHandleType.Normal);
                completedCount = 0;
                taskCount = 0;
            }

            public readonly GCHandle GroupHandle => groupHandle;

            public readonly int TaskCount => taskCount;

            public readonly int CompletedCount => completedCount;

            public readonly bool IsComplete => completedCount == TaskCount;

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
