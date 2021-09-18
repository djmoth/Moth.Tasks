namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public sealed unsafe class TaskQueue : IDisposable
    {
        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private readonly Queue<int> tasks = new Queue<int> ();
        private object[] taskData;
        private int firstTask;
        private int lastTaskEnd;
        private bool hasDisposableTasks;

        public TaskQueue (int startCapacity)
        {
            if (startCapacity < 0)
            {
                throw new ArgumentOutOfRangeException (nameof (startCapacity), $"{nameof (startCapacity)} must be larger than or equal to zero.");
            }

            taskData = new object[startCapacity];
        }

        public void Clear ()
        {
            lock (taskLock)
            {
                tasks.Clear ();

                firstTask = 0;
                lastTaskEnd = 0;
            }
        }

        public void Enqueue (Action action) => Enqueue (new DelegateTask (action));

        public void Enqueue<T> (Action<T> action, T arg) => Enqueue (new DelegateTask<T> (action, arg));

        public void Enqueue<T1, T2> (Action<T1, T2> action, T1 arg1, T2 arg2) => Enqueue (new DelegateTask<T1, T2> (action, arg1, arg2));

        public void Enqueue<T1, T2, T3> (Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) => Enqueue (new DelegateTask<T1, T2, T3> (action, arg1, arg2, arg3));

        public void Enqueue<T> (T task) where T : struct, ITask
        {
            lock (taskLock)
            {
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

                ref byte rawTasks = ref Unsafe.As<object, byte> (ref taskData[0]);
                ref byte newTask = ref Unsafe.Add (ref rawTasks, lastTaskEnd);

                Unsafe.WriteUnaligned (ref Unsafe.Add (ref newTask, sizeof (ushort)), task); // Write Task Data

                Unsafe.WriteUnaligned (ref newTask, (ushort)taskID); // Write Task ID



                lastTaskEnd += totalDataSize;

                if (task is IDisposable)
                {
                    hasDisposableTasks = true;
                }
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

                int id = Unsafe.ReadUnaligned<ushort> (ref this.taskData[firstTask]);

                task = taskCache.GetTask (id);

                byte* dataAlloc = stackalloc byte[task.DataIndices];
                data = dataAlloc;

                this.taskData.AsSpan (firstTask + sizeof (ushort), task.DataIndices).CopyTo (new Span<byte> (data, task.DataIndices)); // Copy Task data to stack

                firstTask += task.DataIndices + sizeof (ushort);

                if (firstTask == lastTaskEnd)
                {
                    firstTask = 0;
                    lastTaskEnd = 0;
                }
            }

            profiler?.BeginTask (task.Name);

            task.Run (ref *data);

            profiler?.EndTask ();

            return true;
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        private void Dispose (bool fromDispose)
        {
            
        }

        private void Reset ()
        {
            firstTask = 0;
            lastTaskEnd = 0;
            hasDisposableTasks = false;
        }
    }
}
