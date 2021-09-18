namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public sealed unsafe class TaskQueue : IDisposable
    {
        private const int StartCapacity = 256;

        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private readonly Queue<int> tasks = new Queue<int> (16);
        private readonly ExceptionHandler exceptionHandler;
        private object[] taskData = new object[StartCapacity];
        private int firstTask;
        private int lastTaskEnd;
        private bool disposed;

        public TaskQueue (ExceptionHandler exceptionHandler)
        {
            this.exceptionHandler = exceptionHandler;
        }

        ~TaskQueue () => Clear ();

        public delegate void ExceptionHandler (Type taskType, Exception exception);

        public void Enqueue (Action action) => Enqueue (new DelegateTask (action));

        public void Enqueue<T> (Action<T> action, T arg) => Enqueue (new DelegateTask<T> (action, arg));

        public void Enqueue<T1, T2> (Action<T1, T2> action, T1 arg1, T2 arg2) => Enqueue (new DelegateTask<T1, T2> (action, arg1, arg2));

        public void Enqueue<T1, T2, T3> (Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) => Enqueue (new DelegateTask<T1, T2, T3> (action, arg1, arg2, arg3));

        public void Enqueue<T> (T task) where T : struct, ITask
        {
            lock (taskLock)
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

                ref byte newTask = ref Unsafe.As<object, byte> (ref taskData[lastTaskEnd]);

                Unsafe.WriteUnaligned (ref newTask, task); // Write task data.

                lastTaskEnd += taskInfo.DataIndices;
            }
        }

        public bool TryRunNextTask () => TryRunNextTask (null);

        public bool TryRunNextTask (IProfiler profiler)
        {
            byte* data;
            TaskInfo task;

            lock (taskLock)
            {
                if (firstTask == lastTaskEnd)
                {
                    return false;
                }

                int id = tasks.Dequeue ();

                task = taskCache.GetTask (id);

                byte* dataAlloc = stackalloc byte[task.DataSize];
                data = dataAlloc;

                ref byte taskDataRef = ref Unsafe.As<object, byte> (ref taskData[firstTask]);

                // Copy from taskData to local data on stack.
                Unsafe.CopyBlock (ref *data, ref taskDataRef, (uint)task.DataSize);

                firstTask += task.DataIndices; // Increment firstTask by size of task.

                if (firstTask == lastTaskEnd) // If firstTask is equal to lastTaskEnd, it means that this was the last task in the queue.
                {
                    firstTask = 0;
                    lastTaskEnd = 0;
                }
            }

            profiler?.BeginTask (task.Name);

            try
            {
                task.Run (ref *data); // Run the task with it's data
            } catch (Exception ex)
            {
                exceptionHandler (task.Type, ex); // Notify handler in case of an exception
            } finally
            {
                if (task.Disposable)
                {
                    task.Dispose (ref *data); // Dispose the task it is required.
                }
            }

            profiler?.EndTask ();

            return true;
        }

        public void Clear ()
        {
            lock (taskLock)
            {
                int taskDataIndex = firstTask;

                foreach (int id in tasks)
                {
                    TaskInfo task = taskCache.GetTask (id);

                    if (task.Disposable)
                    {
                        TryDispose (task, taskDataIndex);
                    }

                    taskDataIndex += task.DataIndices;
                }

                tasks.Clear ();

                firstTask = 0;
                lastTaskEnd = 0;
            }

            void TryDispose (TaskInfo task, int taskDataIndex)
            {
                byte* data = stackalloc byte[task.DataSize];

                ref byte taskDataRef = ref Unsafe.As<object, byte> (ref taskData[firstTask]);

                // Copy from taskData to local data on stack.
                Unsafe.CopyBlock (ref *data, ref taskDataRef, (uint)task.DataSize);

                try
                {
                    task.Dispose (ref *data);
                } catch (Exception ex)
                {
                    exceptionHandler (task.Type, ex);
                }
            }
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
    }
}
